namespace DotNetApiGateway.Models;

/// <summary>
/// Extension methods for <see cref="WebhookSubscription"/> providing common operations and utilities.
/// </summary>
public static class WebhookSubscriptionExtensions
{
    /// <summary>
    /// Determines whether the subscription is currently active and ready to receive events.
    /// </summary>
    /// <param name="subscription">The webhook subscription.</param>
    /// <returns>True if the subscription is active; otherwise, false.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="subscription"/> is null.</exception>
    public static bool IsActive(this WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        return subscription.Active && !string.IsNullOrEmpty(subscription.CallbackUrl);
    }

    /// <summary>
    /// Gets the current delivery success rate as a percentage (0-100).
    /// </summary>
    /// <param name="subscription">The webhook subscription.</param>
    /// <returns>The success rate percentage, or 0 if no deliveries have been attempted or <paramref name="subscription"/> is null.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="subscription"/> is null.</exception>
    public static int GetSuccessRate(this WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        if (subscription.DeliveryStats.TotalDeliveries == 0)
        {
            return 0;
        }

        return (int)Math.Round((double)subscription.DeliveryStats.SuccessfulDeliveries / subscription.DeliveryStats.TotalDeliveries * 100);
    }

    /// <summary>
    /// Gets the next retry delay in milliseconds based on the current retry count and policy.
    /// </summary>
    /// <param name="subscription">The webhook subscription.</param>
    /// <param name="currentRetryCount">The number of previous retry attempts.</param>
    /// <returns>The delay in milliseconds before the next retry attempt.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="subscription"/> is null.</exception>
 /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="currentRetryCount"/> is negative.</exception>
    public static int GetNextRetryDelay(this WebhookSubscription subscription, int currentRetryCount)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        ArgumentOutOfRangeException.ThrowIfNegative(currentRetryCount);

    if (subscription.RetryPolicy is null)
        {
            return 1000;
        }

        // Exponential backoff with jitter
        var delay = Math.Min(
            subscription.RetryPolicy.InitialDelayMs * (int)Math.Pow(2, currentRetryCount),
            subscription.RetryPolicy.MaxDelayMs
        );

        // Add jitter: ±20% random variation
        // Use thread-safe Random.Shared to avoid issues with rapid successive calls
    var random = Random.Shared;
        var jitter = (int)(delay * 0.2 * random.NextDouble());
        return Math.Max(delay - jitter, 100); // Minimum 100ms
    }

    /// <summary>
    /// Determines if the subscription has exceeded its maximum retry attempts.
    /// </summary>
    /// <param name="subscription">The webhook subscription.</param>
    /// <param name="currentRetryCount">The number of previous retry attempts.</param>
    /// <returns>True if the subscription has exceeded its maximum retry attempts; otherwise, false.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="subscription"/> is null.</exception>
    public static bool HasExceededMaxRetries(this WebhookSubscription subscription, int currentRetryCount)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        return currentRetryCount >= (subscription.RetryPolicy?.MaxRetries ?? 3);
    }
}