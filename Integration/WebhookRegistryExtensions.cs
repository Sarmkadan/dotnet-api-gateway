#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

/// <summary>
/// Extension methods for <see cref="WebhookRegistry"/> providing additional utility functionality.
/// </summary>
public static class WebhookRegistryExtensions
{
    /// <summary>
    /// Bulk register multiple webhook subscriptions at once.
    /// </summary>
    /// <param name="registry">The webhook registry instance.</param>
    /// <param name="subscriptions">Collection of subscriptions to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptions"/> is <see langword="null"/>.</exception>
    /// <returns>Count of successfully registered subscriptions.</returns>
    public static int RegisterRange(this WebhookRegistry registry, IEnumerable<WebhookSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(subscriptions);

        int count = 0;
        foreach (var subscription in subscriptions)
        {
            registry.Register(subscription);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Bulk unregister multiple webhook subscriptions by their IDs.
    /// </summary>
    /// <param name="registry">The webhook registry instance.</param>
    /// <param name="subscriptionIds">Collection of subscription IDs to unregister.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptionIds"/> is <see langword="null"/>.</exception>
    /// <returns>Count of successfully unregistered subscriptions.</returns>
    public static int UnregisterRange(this WebhookRegistry registry, IEnumerable<string> subscriptionIds)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(subscriptionIds);

        int count = 0;
        foreach (var subscriptionId in subscriptionIds)
        {
            registry.Unregister(subscriptionId);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Check if any active subscriptions exist for the specified event type.
    /// </summary>
    /// <param name="registry">The webhook registry instance.</param>
    /// <param name="eventType">The event type to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <returns>True if active subscriptions exist, false otherwise.</returns>
    public static bool HasSubscriptionsForEvent(this WebhookRegistry registry, string eventType)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return false;
        }

        var subscriptions = registry.GetSubscriptionsForEvent(eventType);
        return subscriptions.Count > 0;
    }

    /// <summary>
    /// Get the count of active subscriptions for a specific event type.
    /// </summary>
    /// <param name="registry">The webhook registry instance.</param>
    /// <param name="eventType">The event type to count subscriptions for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <returns>Count of active subscriptions for the event type.</returns>
    public static int CountSubscriptionsForEvent(this WebhookRegistry registry, string eventType)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return string.IsNullOrWhiteSpace(eventType)
            ? 0
            : registry.GetSubscriptionsForEvent(eventType).Count;
    }
}