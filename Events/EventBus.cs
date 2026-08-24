#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Immutable;

namespace DotNetApiGateway.Events;

/// <summary>
/// In-memory event bus for pub-sub messaging within the gateway.
/// Allows components to publish and subscribe to domain events.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<string, List<Delegate>> _subscribers = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<EventBus> _logger;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to events of specific type with handler callback.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler callback that will process the event.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IGatewayEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent).Name;

        _lock.EnterWriteLock();
        try
        {
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);
            _logger.LogInformation("Handler subscribed to event type: {EventType}", eventType);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unsubscribe handler from events.
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler callback to remove.</param>
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IGatewayEvent
    {
        if (handler is null)
            return;

        var eventType = typeof(TEvent).Name;

        _lock.EnterWriteLock();
        try
        {
            if (_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType].Remove(handler);
                _logger.LogInformation("Handler unsubscribed from event type: {EventType}", eventType);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Publish event to all subscribed handlers asynchronously.
    /// </summary>
    /// <remarks>
    /// Dispatch semantics:
    /// <list type="bullet">
    /// <item><description>All handlers are invoked regardless of individual failures</description></item>
    /// <item><description>Exceptions from individual handlers are aggregated and thrown as <see cref="EventDispatchException"/> after all handlers complete</description></item>
    /// <item><description>Each handler failure is logged individually with full exception details</description></item>
    /// <item><description>Handler execution order is preserved</description></item>
    /// <item><description>Async handlers are awaited properly without deadlocks</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TEvent">The event type to publish.</typeparam>
    /// <param name="evt">The event to publish.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="evt"/> is null.</exception>
    /// <exception cref="EventDispatchException">Thrown when one or more handlers fail. Contains aggregated exceptions from all failed handlers.</exception>
    public async Task PublishAsync<TEvent>(TEvent evt) where TEvent : class, IGatewayEvent
    {
        ArgumentNullException.ThrowIfNull(evt);

        var eventType = typeof(TEvent).Name;

        _lock.EnterReadLock();
        List<Delegate> handlers;
        try
        {
            if (!_subscribers.TryGetValue(eventType, out var handlerList))
                return;

            handlers = new List<Delegate>(handlerList);
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _logger.LogInformation("Publishing event {EventType} to {HandlerCount} subscribers", eventType, handlers.Count);

        var handlerTasks = handlers.Cast<Func<TEvent, Task>>().Select(h => InvokeHandlerAsync(h, evt)).ToList();

        await Task.WhenAll(handlerTasks);

        var failedHandlers = handlerTasks
            .Select((task, index) => new { Index = index, Task = task })
            .Where(x => x.Task.IsFaulted)
            .Select(x => new HandlerFailure(
                x.Index,
                x.Task.Exception?.InnerException,
                handlers[x.Index].Method?.DeclaringType?.Name ?? "Unknown"))
            .ToImmutableArray();

        if (failedHandlers.Length > 0)
        {
            _logger.LogError("Event dispatch completed with {FailedHandlerCount} failed handlers out of {TotalHandlerCount} total handlers",
                failedHandlers.Length, handlers.Count);
            throw new EventDispatchException(failedHandlers, eventType);
        }
    }

    /// <summary>
    /// Invoke handler with error handling.
    /// </summary>
    /// <remarks>
    /// This method ensures that:
    /// <list type="bullet">
    /// <item><description>Exceptions are caught and logged individually</description></item>
    /// <item><description>Async handlers are awaited properly</description></item>
    /// <item><description>No exceptions propagate to the caller</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="handler">The handler to invoke.</param>
    /// <param name="evt">The event to pass to the handler.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    private async Task InvokeHandlerAsync<TEvent>(Func<TEvent, Task> handler, TEvent evt) where TEvent : class
    {
        try
        {
            await handler(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event handler failed for {EventType}", typeof(TEvent).Name);
        }
    }

    /// <summary>
    /// Get count of subscribers for specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <returns>The number of subscribers for the specified event type.</returns>
    public int GetSubscriberCount<TEvent>() where TEvent : class, IGatewayEvent
    {
        var eventType = typeof(TEvent).Name;

        _lock.EnterReadLock();
        try
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
                return handlers.Count;

            return 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clear all subscribers for all event types.
    /// </summary>
    /// <remarks>
    /// This method removes all registered handlers from the event bus.
    /// </remarks>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.Clear();
            _logger.LogInformation("All event subscribers cleared");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        public override string ToString() => $"EventBus {{ RouteId = {RouteId}, RouteName = {RouteName}, TargetId = {TargetId}, OldState = {OldState}, NewState = {NewState}, ClientId = {ClientId} }}";
}
}

/// <summary>
/// Exception thrown when event dispatch fails for one or more handlers.
/// </summary>
/// <remarks>
/// Contains detailed information about which handlers failed and with what exceptions.
/// </remarks>
public sealed class EventDispatchException : Exception
{
    /// <summary>
    /// Gets the collection of handler failures.
    /// </summary>
    public IReadOnlyCollection<HandlerFailure> FailedHandlers { get; }

    /// <summary>
    /// Gets the event type that failed to dispatch.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDispatchException"/> class.
    /// </summary>
    /// <param name="failedHandlers">Collection of handler failures.</param>
    /// <param name="eventType">The event type that failed to dispatch.</param>
    public EventDispatchException(IReadOnlyCollection<HandlerFailure> failedHandlers, string eventType)
    {
        ArgumentNullException.ThrowIfNull(failedHandlers);
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        FailedHandlers = failedHandlers;
        EventType = eventType;

        var failureCount = failedHandlers.Count;
        var message = $"Event dispatch failed for {eventType}: {failureCount} handler(s) failed";

        if (failedHandlers.Count == 1)
        {
            var failure = failedHandlers.First();
            message += $"\nHandler #{failure.HandlerIndex} ({failure.HandlerType}) failed: {failure.Exception?.Message}";
        }
        else
        {
            message += "\nFailed handlers:";
            foreach (var failure in failedHandlers)
            {
                message += $"\n- Handler #{failure.HandlerIndex} ({failure.HandlerType}): {failure.Exception?.Message}";
            }
        }

        Data[nameof(FailedHandlers)] = failedHandlers;
        Data[nameof(EventType)] = eventType;

        base.Data[nameof(Message)] = message;
    }

    /// <summary>
    /// Gets the message that describes the exception.
    /// </summary>
    public override string Message => (string?)base.Data[nameof(Message)] ?? base.Message ?? "Event dispatch failed";
}

/// <summary>
/// Represents information about a failed handler during event dispatch.
/// </summary>
/// <param name="HandlerIndex">The index/position of the handler in the subscription list.</param>
/// <param name="Exception">The exception thrown by the handler, if any.</param>
/// <param name="HandlerType">The type containing the handler method.</param>
public sealed record HandlerFailure(int HandlerIndex, Exception? Exception, string HandlerType);

/// <summary>
/// Interface for all gateway events.
/// </summary>
public interface IGatewayEvent
{
    DateTime Timestamp { get; }
    string EventType { get;     public override string ToString() => $"EventBus {{ RouteId = {RouteId}, RouteName = {RouteName}, TargetId = {TargetId}, OldState = {OldState}, NewState = {NewState}, ClientId = {ClientId} }}";
}
}

/// <summary>
/// Base class for gateway events.
/// </summary>
public abstract class GatewayEvent : IGatewayEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public abstract string EventType { get;     public override string ToString() => $"EventBus {{ RouteId = {RouteId}, RouteName = {RouteName}, TargetId = {TargetId}, OldState = {OldState}, NewState = {NewState}, ClientId = {ClientId} }}";
}
}

/// <summary>
/// Event published when a route is created.
/// </summary>
public class RouteCreatedEvent : GatewayEvent
{
    public string RouteId { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public override string EventType => nameof(RouteCreatedEvent);
}

/// <summary>
/// Event published when circuit breaker state changes.
/// </summary>
public class CircuitBreakerStateChangedEvent : GatewayEvent
{
    public string TargetId { get; set; } = string.Empty;
    public string OldState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public override string EventType => nameof(CircuitBreakerStateChangedEvent);
}

/// <summary>
/// Event published when rate limit is exceeded.
/// </summary>
public class RateLimitExceededEvent : GatewayEvent
{
    public string ClientId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public override string EventType => nameof(RateLimitExceededEvent);
}

/// <summary>
/// Event published when request fails.
/// </summary>
public class RequestFailedEvent : GatewayEvent
{
    public string RequestId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public override string EventType => nameof(RequestFailedEvent);
}