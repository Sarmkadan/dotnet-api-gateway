#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetApiGateway.Integration;

using DotNetApiGateway.Models;
using DotNetApiGateway.Utilities;

/// <summary>
/// Registry for managing webhook subscriptions and event routing.
/// Maintains thread-safe collection of active webhooks and routes events to subscribers.
/// </summary>
public sealed class WebhookRegistry
{
    private readonly List<WebhookSubscription> _subscriptions = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<WebhookRegistry> _logger;
    private readonly WebhookCallbackUrlValidator _urlValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record registration and delivery activity.</param>
    /// <param name="urlValidator">Validator used to re-check callback URLs against SSRF rules immediately before delivery.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="urlValidator"/> is null.</exception>
    public WebhookRegistry(ILogger<WebhookRegistry> logger, WebhookCallbackUrlValidator urlValidator)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(urlValidator);

        _logger = logger;
        _urlValidator = urlValidator;
    }

    /// <summary>
    /// Register a new webhook subscription.
    /// </summary>
    /// <param name="subscription">The webhook subscription to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if subscription is null.</exception>
    public void Register(WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        _lock.EnterWriteLock();
        try
        {
            _subscriptions.Add(subscription);
            _logger.LogInformation("Webhook registered: {SubscriptionId}", subscription.Id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unregister a webhook subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to unregister.</param>
    /// <exception cref="ArgumentNullException">Thrown if subscriptionId is null.</exception>
    public void Unregister(string subscriptionId)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        _lock.EnterWriteLock();
        try
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription is not null)
            {
                _subscriptions.Remove(subscription);
                _logger.LogInformation("Webhook unregistered: {SubscriptionId}", subscriptionId);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get all active webhook subscriptions for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type to filter subscriptions by.</param>
    /// <returns>List of matching webhook subscriptions.</returns>
    /// <exception cref="ArgumentNullException">Thrown if eventType is null.</exception>
    public List<WebhookSubscription> GetSubscriptionsForEvent(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        _lock.EnterReadLock();
        try
        {
            return _subscriptions
                .Where(s => s.Active && (s.EventTypes.Contains("*") || s.EventTypes.Contains(eventType)))
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get all registered subscriptions.
    /// </summary>
    /// <returns>List of all registered webhook subscriptions.</returns>
    public List<WebhookSubscription> GetAllSubscriptions()
    {
        _lock.EnterReadLock();
        try
        {
            return new List<WebhookSubscription>(_subscriptions);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Publish event to all subscribed webhooks.
    /// Executes asynchronously without blocking the caller.
    /// </summary>
    /// <param name="webhookEvent">The webhook event to publish.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if webhookEvent is null.</exception>
    public async Task PublishEventAsync(WebhookEvent webhookEvent)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        var subscriptions = GetSubscriptionsForEvent(webhookEvent.EventType);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No subscriptions found for event type: {EventType}", webhookEvent.EventType);
            return;
        }

        _logger.LogInformation("Publishing event {EventType} to {SubscriptionCount} subscribers", webhookEvent.EventType, subscriptions.Count);

        var tasks = subscriptions.Select(s => DeliverWebhookAsync(s, webhookEvent));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Deliver webhook event to a specific subscription with retry logic.
    /// </summary>
    /// <param name="subscription">The webhook subscription to deliver to.</param>
    /// <param name="webhookEvent">The webhook event to deliver.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task DeliverWebhookAsync(WebhookSubscription subscription, WebhookEvent webhookEvent)
    {
        for (int attempt = 0; attempt <= subscription.RetryPolicy.MaxRetries; attempt++)
        {
            try
            {
                // Re-validate the callback URL immediately before every delivery attempt.
                // The URL was already validated at registration time, but DNS records can
                // change between registration and delivery (or between retries), so a
                // stale check here would leave the gateway open to DNS rebinding SSRF.
                var validation = await _urlValidator.ValidateAsync(subscription.CallbackUrl).ConfigureAwait(false);
                if (!validation.IsAllowed)
                {
                    _logger.LogError(
                        "Webhook delivery blocked for {SubscriptionId}: {Reason}", subscription.Id, validation.Error);
                    return;
                }

                using var client = new HttpClient();

                var json = System.Text.Json.JsonSerializer.Serialize(webhookEvent);

                // Sign the exact JSON body being sent so tampering with any field, including
                // the event data, invalidates the signature.
                var signature = GenerateWebhookSignature(subscription, webhookEvent.SignedAt, json);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(signature))
                {
                    content.Headers.Add("X-Signature", signature);
                }

                var response = await client.PostAsync(subscription.CallbackUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook delivered successfully: {SubscriptionId}", subscription.Id);
                    return;
                }

                _logger.LogWarning("Webhook delivery failed with status {StatusCode}: {SubscriptionId}", response.StatusCode, subscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook delivery exception (attempt {Attempt}): {SubscriptionId}", attempt + 1, subscription.Id);
            }

            // Delay before retry (exponential backoff)
            if (attempt < subscription.RetryPolicy.MaxRetries)
            {
                var delay = Math.Min(
                    subscription.RetryPolicy.InitialDelayMs * (int)Math.Pow(2, attempt),
                    subscription.RetryPolicy.MaxDelayMs
                );
                await Task.Delay(delay);
            }
        }

        _logger.LogError("Webhook delivery failed after {MaxRetries} retries: {SubscriptionId}", subscription.RetryPolicy.MaxRetries, subscription.Id);
    }

    /// <summary>
    /// Generate the HMAC-SHA256 signature header value for a webhook delivery, per the scheme
    /// documented in docs/WebhookSignatureVerification.md: HMAC-SHA256("{timestamp}.{jsonPayload}", secret).
    /// Always signs with the subscription's current secret, since receivers are expected to
    /// accept signatures from either the current or previous secret while a rotation is rolling
    /// out (see <see cref="WebhookSubscription.PreviousSecret"/>).
    /// </summary>
    /// <param name="subscription">The webhook subscription whose secret signs the payload.</param>
    /// <param name="signedAt">Unix timestamp in seconds included in the signature to prevent replay.</param>
    /// <param name="jsonPayload">The exact serialized JSON body being delivered.</param>
    /// <returns>The <c>X-Signature</c> header value, or an empty string if no secret is configured.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="jsonPayload"/> is null or empty.</exception>
    private string GenerateWebhookSignature(WebhookSubscription subscription, long signedAt, string jsonPayload)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentException.ThrowIfNullOrEmpty(jsonPayload);

        var secret = subscription.CurrentSecret;

        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("No secret available for subscription {SubscriptionId}", subscription.Id);
            return string.Empty;
        }

        var message = $"{signedAt}.{jsonPayload}";
        var signature = CryptoUtility.GenerateHmacSha256(message, secret);

        return $"t={signedAt},v1={signature}";
    }
}

/// <summary>
/// Represents a webhook subscription with event type filters and retry policy.
/// </summary>
public sealed class WebhookSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CallbackUrl { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = [];
    public string CurrentSecret { get; set; } = string.Empty;
    public string? PreviousSecret { get; set; }
    public DateTime? SecretRotationAt { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WebhookRetryPolicy RetryPolicy { get; set; } = new();
}

/// <summary>
/// Retry policy configuration for webhook delivery.
/// </summary>
public sealed class WebhookRetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 60000;
}

/// <summary>
/// Webhook event payload sent to subscribers.
/// </summary>
public sealed class WebhookEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }
    public long SignedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}