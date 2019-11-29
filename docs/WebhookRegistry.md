# WebhookRegistry

Central registry for managing webhook subscriptions and event publishing. It tracks subscriptions by event type, handles retry policies for failed deliveries, and provides methods to register, unregister, and query subscriptions.

## API

### `public WebhookRegistry()`

Initializes a new, empty webhook registry with default retry policy settings.

---

### `public void Register(WebhookSubscription subscription)`

Registers a new subscription in the registry.

- **subscription**: The subscription to register. Must not be `null`.
- Throws: `ArgumentNullException` if `subscription` is `null`.

---

### `public void Unregister(string id)`

Removes a subscription from the registry by its unique identifier.

- **id**: The unique identifier of the subscription to remove. Must not be `null` or empty.
- Throws: `ArgumentException` if `id` is `null` or empty.

---

### `public List<WebhookSubscription> GetSubscriptionsForEvent(string eventType)`

Retrieves all active subscriptions that are interested in the specified event type.

- **eventType**: The event type to filter subscriptions by. Must not be `null` or empty.
- Returns: A list of matching subscriptions. Never `null`; may be empty.
- Throws: `ArgumentException` if `eventType` is `null` or empty.

---

### `public List<WebhookSubscription> GetAllSubscriptions()`

Retrieves all registered subscriptions, regardless of event type or status.

- Returns: A list of all subscriptions. Never `null`; may be empty.

---

### `public async Task PublishEventAsync(string eventType, object? data)`

Publishes an event to all registered subscriptions matching the specified event type. Uses the registry’s retry policy to handle delivery failures.

- **eventType**: The type of event being published. Must not be `null` or empty.
- **data**: Optional payload to send with the event. May be `null`.
- Throws: `ArgumentException` if `eventType` is `null` or empty.

---

### `public string Id` (instance property)

Gets the unique identifier of the registry instance.

- Returns: A non-null, non-empty string.

---

### `public string CallbackUrl` (instance property of `WebhookSubscription`)

Gets the callback URL where the webhook should be delivered.

- Returns: A non-null, non-empty string.

---

### `public string[] EventTypes` (instance property of `WebhookSubscription`)

Gets the array of event types this subscription is interested in.

- Returns: A non-null array; may be empty.

---

### `public string Secret` (instance property of `WebhookSubscription`)

Gets the secret used to sign webhook payloads for verification.

- Returns: A non-null string; may be empty.

---

### `public bool Active` (instance property of `WebhookSubscription`)

Gets or sets whether the subscription is currently active and should receive events.

- Returns: `true` if active; otherwise, `false`.

---

### `public DateTime CreatedAt` (instance property of `WebhookSubscription`)

Gets the timestamp when the subscription was created.

- Returns: A `DateTime` representing the creation time.

---

### `public WebhookRetryPolicy RetryPolicy` (instance property of `WebhookSubscription`)

Gets the retry policy associated with this subscription.

- Returns: The retry policy. Never `null`.

---

### `public int MaxRetries` (instance property of `WebhookRetryPolicy`)

Gets the maximum number of retry attempts allowed for failed deliveries.

- Returns: A non-negative integer.

---

### `public int InitialDelayMs` (instance property of `WebhookRetryPolicy`)

Gets the initial delay in milliseconds before the first retry attempt.

- Returns: A non-negative integer.

---

### `public int MaxDelayMs` (instance property of `WebhookRetryPolicy`)

Gets the maximum delay in milliseconds for any retry attempt.

- Returns: A non-negative integer.

---
### `public string EventType` (instance property of `WebhookEvent`)

Gets the type of the event being published.

- Returns: A non-null, non-empty string.

---
### `public DateTime Timestamp` (instance property of `WebhookEvent`)

Gets the timestamp when the event was published.

- Returns: A `DateTime` representing the event time.

---
### `public object? Data` (instance property of `WebhookEvent`)

Gets the optional payload data associated with the event.

- Returns: The payload data, or `null` if none was provided.

## Usage
