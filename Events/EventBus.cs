// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Events;

/// <summary>
/// In-memory event bus for pub-sub messaging within the gateway.
/// Allows components to publish and subscribe to domain events.
/// </summary>
public class EventBus
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
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IGatewayEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

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
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IGatewayEvent
    {
        if (handler == null)
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
    public async Task PublishAsync<TEvent>(TEvent evt) where TEvent : class, IGatewayEvent
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

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

        var tasks = handlers.Cast<Func<TEvent, Task>>().Select(h => InvokeHandlerAsync(h, evt));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invoke handler with error handling.
    /// </summary>
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
    }
}

/// <summary>
/// Interface for all gateway events.
/// </summary>
public interface IGatewayEvent
{
    DateTime Timestamp { get; }
    string EventType { get; }
}

/// <summary>
/// Base class for gateway events.
/// </summary>
public abstract class GatewayEvent : IGatewayEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
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
