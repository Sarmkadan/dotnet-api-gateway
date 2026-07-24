# EventBus

Centralized event dispatching mechanism for the API gateway, enabling decoupled communication between route handlers, middleware, and external services via strongly-typed events.

## Dispatch Semantics

The EventBus implements the following dispatch semantics:

- **All handlers are invoked**: Regardless of individual handler failures, all subscribed handlers are invoked
- **Exception isolation**: Individual handler exceptions do not prevent other handlers from executing
- **Aggregate exceptions**: When multiple handlers fail, exceptions are aggregated and thrown as `EventDispatchException` after all handlers complete
- **Structured failure information**: Detailed information about which handlers failed and with what exceptions is available
- **Preserved execution order**: Handlers are invoked in the order they were subscribed
- **Proper async/await**: Async handlers are awaited properly without deadlocks

## API

### `Subscribe<TEvent>`
Registers a handler for events of type `TEvent`. Handlers are invoked in the order they are subscribed when the event is published.

**Parameters:**
- `handler`: The async handler callback that will process the event

**Exceptions:**
- `ArgumentNullException`: Thrown when handler is null

### `Unsubscribe<TEvent>`
Removes a handler previously registered for events of type `TEvent`.

**Parameters:**
- `handler`: The handler callback to remove

### `PublishAsync<TEvent>`
Asynchronously invokes all subscribed handlers for the given event.

**Type Parameters:**
- `TEvent`: The event type to publish

**Parameters:**
- `evt`: The event to publish

**Returns:**
- `Task`: Task representing the asynchronous operation

**Exceptions:**
- `ArgumentNullException`: Thrown when evt is null
- `EventDispatchException`: Thrown when one or more handlers fail. Contains aggregated exceptions from all failed handlers

### `GetSubscriberCount<TEvent>`
Returns the number of handlers currently subscribed to events of type `TEvent`.

**Type Parameters:**
- `TEvent`: The event type to check

**Returns:**
- `int`: The number of subscribers for the specified event type

### `Clear`
Removes all event subscriptions across all types.

### `EventDispatchException`
Exception thrown when event dispatch fails for one or more handlers.

**Properties:**
- `FailedHandlers`: Collection of handler failures with detailed error information
- `EventType`: The event type that failed to dispatch

**Example:**
```csharp
try
{
    await eventBus.PublishAsync(routeCreatedEvent);
}
catch (EventDispatchException ex)
{
    Console.WriteLine($"Event dispatch failed for {ex.EventType}: {ex.Message}");
    foreach (var failure in ex.FailedHandlers)
    {
        Console.WriteLine($"Handler #{failure.HandlerIndex} failed: {failure.Exception?.Message}");
    }
}
```

## Usage
