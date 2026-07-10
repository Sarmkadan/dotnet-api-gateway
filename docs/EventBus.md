# EventBus

Centralized event dispatching mechanism for the API gateway, enabling decoupled communication between route handlers, middleware, and external services via strongly-typed events.

## API

### `Subscribe<TEvent>`
Registers a handler for events of type `TEvent`. Handlers are invoked in the order they are subscribed when the event is published.

### `Unsubscribe<TEvent>`
Removes all handlers previously registered for events of type `TEvent`.

### `PublishAsync<TEvent>`
Asynchronously invokes all subscribed handlers for the given event. Throws `InvalidOperationException` if the event type has no subscribers.

### `GetSubscriberCount<TEvent>`
Returns the number of handlers currently subscribed to events of type `TEvent`.

### `Clear`
Removes all event subscriptions across all types.

### `Timestamp`
Gets the creation timestamp of the bus instance.

### `EventType`
Gets the fully qualified type name of the current event being processed, or `null` if not in an event context.

### `RouteId`
Gets the identifier of the route associated with the current event or operation.

### `RouteName`
Gets the name of the route associated with the current event or operation.

### `TargetId`
Gets the identifier of the target entity involved in the current event or operation.

### `OldState`
Gets the previous state value during state transition events.

### `NewState`
Gets the new state value during state transition events.

### `ClientId`
Gets the identifier of the client associated with the current event or operation.

### `RequestCount`
Gets the total number of requests processed by the gateway.

### `Limit`
Gets the configured maximum number of concurrent operations allowed.

### `RequestId`
Gets the identifier of the current request being processed.

### `Path`
Gets the request path associated with the current event or operation.

### `Reason`
Gets the reason or cause associated with the current event or operation.

## Usage
