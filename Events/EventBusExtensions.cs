#nullable enable

namespace DotNetApiGateway.Events;

/// <summary>
/// Extension methods for <see cref="EventBus"/> providing additional functionality
/// for event bus management and monitoring.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Get all subscriber counts for all registered event types.
    /// </summary>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>Dictionary mapping event type names to subscriber counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bus is null.</exception>
    public static IReadOnlyDictionary<string, int> GetAllSubscriberCounts(this EventBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        var eventTypes = typeof(IGatewayEvent).Assembly.GetTypes()
            .Where(t => typeof(IGatewayEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var eventType in eventTypes)
        {
            var countMethod = typeof(EventBus).GetMethod("GetSubscriberCount")?.MakeGenericMethod(eventType);
            if (countMethod != null)
            {
                var count = (int)countMethod.Invoke(bus, null)!;
                result[eventType.Name] = count;
            }
        }

        return result;
    }

    /// <summary>
    /// Get the total number of subscribers across all event types.
    /// </summary>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>Total subscriber count.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bus is null.</exception>
    public static int GetTotalSubscriberCount(this EventBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var allCounts = bus.GetAllSubscriberCounts();
        return allCounts.Sum(kvp => kvp.Value);
    }

    /// <summary>
    /// Check if there are any subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>True if there are subscribers; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bus is null.</exception>
    public static bool HasSubscribers<TEvent>(this EventBus bus) where TEvent : class, IGatewayEvent
    {
        ArgumentNullException.ThrowIfNull(bus);

        return bus.GetSubscriberCount<TEvent>() > 0;
    }

    /// <summary>
    /// Get all event types that currently have subscribers.
    /// </summary>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>Collection of event type names with subscribers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bus is null.</exception>
    public static IEnumerable<string> GetEventTypesWithSubscribers(this EventBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return bus.GetAllSubscriberCounts()
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Publish multiple events as a batch.
    /// </summary>
    /// <param name="bus">The event bus instance.</param>
    /// <param name="events">Collection of events to publish.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bus or events is null.</exception>
    public static async Task PublishBatchAsync(this EventBus bus, IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(events);

        var tasks = new List<Task>();

        foreach (var evt in events)
        {
            if (evt is null)
                continue;

            var eventType = evt.GetType();
            if (typeof(IGatewayEvent).IsAssignableFrom(eventType))
            {
                var publishMethod = typeof(EventBus).GetMethod("PublishAsync")?.MakeGenericMethod(eventType);
                if (publishMethod != null)
                {
                    tasks.Add((Task)publishMethod.Invoke(bus, new[] { evt })!);
                }
            }
        }

        await Task.WhenAll(tasks);
    }
}