#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Models for webhook configuration and delivery.
/// </summary>

/// <summary>
/// Webhook subscription configuration.
/// </summary>
public sealed class WebhookSubscription
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("eventTypes")]
    public string[] EventTypes { get; set; } = []string>();

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("retryPolicy")]
    public WebhookRetryPolicy RetryPolicy { get; set; } = new();

    [JsonPropertyName("deliveryStats")]
    public WebhookDeliveryStats DeliveryStats { get; set; } = new();
}

/// <summary>
/// Retry policy for webhook delivery.
/// </summary>
public sealed class WebhookRetryPolicy
{
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("initialDelayMs")]
    public int InitialDelayMs { get; set; } = 1000;

    [JsonPropertyName("maxDelayMs")]
    public int MaxDelayMs { get; set; } = 60000;
}

/// <summary>
/// Webhook delivery statistics.
/// </summary>
public sealed class WebhookDeliveryStats
{
    [JsonPropertyName("totalDeliveries")]
    public int TotalDeliveries { get; set; }

    [JsonPropertyName("successfulDeliveries")]
    public int SuccessfulDeliveries { get; set; }

    [JsonPropertyName("failedDeliveries")]
    public int FailedDeliveries { get; set; }

    [JsonPropertyName("lastDeliveryTime")]
    public DateTime? LastDeliveryTime { get; set; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }
}

/// <summary>
/// Webhook event payload.
/// </summary>
public sealed class WebhookEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }
}

/// <summary>
/// Webhook event delivery attempt.
/// </summary>
public sealed class WebhookDeliveryAttempt
{
    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTimeMs { get; set; }
}
