#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

/// <summary>
/// Registry for managing webhook subscriptions and event routing.
/// Maintains thread-safe collection of active webhooks and routes events to subscribers.
/// </summary>
public sealed class WebhookRegistry
{
    private readonly List<WebhookSubscription> _subscriptions = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<WebhookRegistry> _logger;

    public WebhookRegistry(ILogger<WebhookRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a new webhook subscription.
    /// </summary>
    public void Register(WebhookSubscription subscription)
    {
        if (subscription is null)
            throw new ArgumentNullException(nameof(subscription));

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
    public void Unregister(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

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
    public List<WebhookSubscription> GetSubscriptionsForEvent(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return new List<WebhookSubscription>();

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
    public async Task PublishEventAsync(WebhookEvent webhookEvent)
    {
        if (webhookEvent is null)
            throw new ArgumentNullException(nameof(webhookEvent));

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
    private async Task DeliverWebhookAsync(WebhookSubscription subscription, WebhookEvent webhookEvent)
    {
        for (int attempt = 0; attempt <= subscription.RetryPolicy.MaxRetries; attempt++)
        {
            try
            {
                using var client = new HttpClient();
                var json = System.Text.Json.JsonSerializer.Serialize(webhookEvent);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

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
}

/// <summary>
/// Represents a webhook subscription with event type filters and retry policy.
/// </summary>
public sealed class WebhookSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CallbackUrl { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = []string>();
    public string Secret { get; set; } = string.Empty;
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
}
