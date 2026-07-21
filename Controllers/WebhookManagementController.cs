#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using DotNetApiGateway.Integration;
using WebhookSubscription = DotNetApiGateway.Integration.WebhookSubscription;
using WebhookRetryPolicy = DotNetApiGateway.Integration.WebhookRetryPolicy;
using WebhookEvent = DotNetApiGateway.Integration.WebhookEvent;
using DotNetApiGateway.Formatters;

/// <summary>
/// Manages webhook subscriptions and delivery configuration.
/// Handles webhook event routing and retry policies for external systems.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookManagementController : ControllerBase
{
    private readonly WebhookRegistry _webhookRegistry;
    private readonly ILogger<WebhookManagementController> _logger;
    private static readonly Dictionary<string, WebhookSubscription> _subscriptions = new();

    public WebhookManagementController(WebhookRegistry webhookRegistry, ILogger<WebhookManagementController> logger)
    {
        _webhookRegistry = webhookRegistry;
        _logger = logger;
    }

    private IActionResult FormatError(object errorObj)
    {
        var accept = HttpContext.Request.Headers["Accept"].ToString();
        if (!string.IsNullOrEmpty(accept) && accept.Contains("application/xml", StringComparison.OrdinalIgnoreCase))
        {
            var xml = XmlFormatter.Serialize(errorObj);
            return Content(xml, "application/xml");
        }

        return BadRequest(errorObj);
    }

    /// <summary>
    /// Subscribe to gateway events with webhook callback URL.
    /// Webhook will be called when specified events occur with event payload.
    /// </summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateWebhookSubscription([FromBody] CreateWebhookSubscriptionRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CallbackUrl))
            return FormatError(new { error = "Callback URL required" });

        if (!Uri.TryCreate(request.CallbackUrl, UriKind.Absolute, out _))
            return FormatError(new { error = "Invalid callback URL format" });

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid().ToString(),
            CallbackUrl = request.CallbackUrl,
            EventTypes = request.EventTypes ?? new[] { "*" },
            Secret = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            Active = true,
            RetryPolicy = new WebhookRetryPolicy
            {
                MaxRetries = request.MaxRetries ?? 3,
                InitialDelayMs = request.InitialDelayMs ?? 1000,
                MaxDelayMs = request.MaxDelayMs ?? 60000
            }
        };

        _subscriptions[subscription.Id] = subscription;
        _webhookRegistry.Register(subscription);
        _logger.LogInformation("Webhook subscription created: {SubscriptionId}", subscription.Id);

        return CreatedAtAction(nameof(GetWebhookSubscription), new { id = subscription.Id }, subscription);
    }

    /// <summary>
    /// List all active webhook subscriptions with event types and status.
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllWebhookSubscriptions()
    {
        return Ok(_subscriptions.Values.ToList());
    }

    /// <summary>
    /// Get a specific webhook subscription by ID with delivery statistics.
    /// </summary>
    [HttpGet("subscriptions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetWebhookSubscription(string id)
    {
        if (!_subscriptions.TryGetValue(id, out var subscription))
            return NotFound(new { error = "Subscription not found", id });

        return Ok(subscription);
    }

    /// <summary>
    /// Update webhook subscription configuration including events and retry policy.
    /// </summary>
    [HttpPut("subscriptions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateWebhookSubscription(string id, [FromBody] UpdateWebhookSubscriptionRequest request)
    {
        if (!_subscriptions.TryGetValue(id, out var subscription))
            return NotFound(new { error = "Subscription not found", id });

        if (!string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            if (!Uri.TryCreate(request.CallbackUrl, UriKind.Absolute, out _))
                return FormatError(new { error = "Invalid callback URL format" });

            subscription.CallbackUrl = request.CallbackUrl;
        }

        if (request.EventTypes is not null && request.EventTypes.Length > 0)
            subscription.EventTypes = request.EventTypes;

        if (request.MaxRetries.HasValue)
            subscription.RetryPolicy.MaxRetries = request.MaxRetries.Value;

        _logger.LogInformation("Webhook subscription updated: {SubscriptionId}", id);
        return Ok(subscription);
    }

    /// <summary>
    /// Delete a webhook subscription and stop sending events.
    /// </summary>
    [HttpDelete("subscriptions/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteWebhookSubscription(string id)
    {
        if (!_subscriptions.TryGetValue(id, out var subscription))
            return NotFound(new { error = "Subscription not found", id });

        subscription.Active = false;
        _subscriptions.Remove(id);
        _logger.LogInformation("Webhook subscription deleted: {SubscriptionId}", id);
        return NoContent();
    }

    /// <summary>
    /// Test webhook delivery by sending a sample event payload.
    /// Validates that the webhook endpoint is reachable and responding.
    /// </summary>
    [HttpPost("subscriptions/{id}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> TestWebhookDelivery(string id)
    {
        if (!_subscriptions.TryGetValue(id, out var subscription))
            return NotFound(new { error = "Subscription not found", id });

        try
        {
            var testEvent = new WebhookEvent
            {
                EventType = "test",
                Timestamp = DateTime.UtcNow,
                Data = new { message = "Test event from API Gateway" }
            };

            using var client = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(testEvent);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(subscription.CallbackUrl, content);

            _logger.LogInformation("Webhook test delivery: {SubscriptionId} - {StatusCode}", id, response.StatusCode);
            return Ok(new { statusCode = response.StatusCode, success = response.IsSuccessStatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook test delivery failed: {SubscriptionId}", id);
            return StatusCode(502, new { error = "Webhook delivery failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Fire a test event to all registered webhooks for a specific event type.
    /// Uses the WebhookRegistry to deliver events with retry logic.
    /// </summary>
    [HttpPost("test-fire")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestFireWebhook([FromBody] TestFireRequest request)
    {
        if (request is null)
            return BadRequest(new { error = "Request body required" });

        if (string.IsNullOrWhiteSpace(request.EventType))
            return BadRequest(new { error = "Event type required" });

        try
        {
            var testEvent = new WebhookEvent
            {
                EventType = request.EventType,
                Timestamp = DateTime.UtcNow,
                Data = request.Data ?? new { message = "Test fire event" }
            };

            await _webhookRegistry.PublishEventAsync(testEvent);

            _logger.LogInformation("Test fire webhook event published: {EventType}", request.EventType);
            return Ok(new { message = "Test event fired successfully", eventType = request.EventType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test fire webhook failed: {EventType}", request.EventType);
            return StatusCode(500, new { error = "Test fire failed", details = ex.Message });
        }
    }
}

public sealed class CreateWebhookSubscriptionRequest
{
    public string? CallbackUrl { get; set; }
    public string[]? EventTypes { get; set; }
    public int? MaxRetries { get; set; }
    public int? InitialDelayMs { get; set; }
    public int? MaxDelayMs { get; set; }
}

public sealed class UpdateWebhookSubscriptionRequest
{
    public string? CallbackUrl { get; set; }
    public string[]? EventTypes { get; set; }
    public int? MaxRetries { get; set; }
}

public sealed class TestFireRequest
{
    public string EventType { get; set; } = string.Empty;
    public object? Data { get; set; }
}
