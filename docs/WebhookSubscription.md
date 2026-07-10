# WebhookSubscription

Represents a registered subscription for receiving webhook events from the API gateway. It encapsulates the configuration, state, delivery statistics, and retry policy for outbound HTTP callbacks triggered by specified event types.

## API

### `public string Id`
Gets the unique identifier assigned to this subscription upon creation. This value is immutable and used to reference the subscription in management operations.

### `public string CallbackUrl`
Gets the absolute URL to which webhook payloads are delivered via HTTP POST. Must be a reachable endpoint capable of accepting JSON payloads.

### `public string[] EventTypes`
Gets the array of event type strings this subscription is configured to receive. Only events whose type matches an entry in this array will trigger a delivery attempt.

### `public string Secret`
Gets the shared secret used to compute HMAC signatures on outgoing webhook payloads. The receiving endpoint can use this value to verify the authenticity and integrity of the delivery.

### `public bool Active`
Gets or sets whether the subscription is currently enabled for delivery. When `false`, matching events will not trigger delivery attempts. Disabling a subscription does not reset its delivery statistics.

### `public DateTime CreatedAt`
Gets the UTC timestamp at which the subscription was originally created. This value is set once and never modified.

### `public WebhookRetryPolicy RetryPolicy`
Gets the retry strategy configuration that governs how failed deliveries are retried. This object defines the backoff algorithm, maximum retry count, and delay boundaries.

### `public WebhookDeliveryStats DeliveryStats`
Gets an object containing aggregate delivery metrics for this subscription, including total attempts, successes, failures, and the timestamp of the most recent delivery.

### `public int MaxRetries`
Gets the maximum number of retry attempts permitted for a single delivery before it is marked as permanently failed. This value is derived from the active retry policy.

### `public int InitialDelayMs`
Gets the initial backoff delay in milliseconds before the first retry attempt. Subsequent retries apply progressively larger delays according to the retry policy's backoff multiplier.

### `public int MaxDelayMs`
Gets the upper bound in milliseconds for any retry delay. The computed backoff delay will never exceed this value, regardless of the retry count.

### `public int TotalDeliveries`
Gets the cumulative count of all delivery attempts (both initial and retry) made for this subscription since its creation.

### `public int SuccessfulDeliveries`
Gets the cumulative count of delivery attempts that received a successful HTTP response (typically 2xx status codes) from the callback endpoint.

### `public int FailedDeliveries`
Gets the cumulative count of delivery attempts that resulted in a non-successful HTTP response, a network error, or a timeout.

### `public DateTime? LastDeliveryTime`
Gets the UTC timestamp of the most recent delivery attempt, or `null` if no delivery has ever been attempted for this subscription.

### `public string? LastError`
Gets the error message or HTTP status description from the most recent failed delivery attempt, or `null` if the last delivery succeeded or no deliveries have occurred.

### `public string EventType`
Gets the event type string associated with a specific webhook payload being delivered. This member is populated on the delivery payload object, not on the subscription configuration itself.

### `public DateTime Timestamp`
Gets the UTC timestamp at which the event being delivered was generated. This member is populated on the delivery payload object.

### `public object? Data`
Gets the event-specific payload data being delivered. The concrete type depends on the event type. This member is populated on the delivery payload object and may be `null` for events that carry no data.

### `public int RetryCount`
Gets the zero-based retry attempt number for the current delivery. A value of `0` indicates the initial delivery attempt; higher values indicate successive retries. This member is populated on the delivery payload object.

## Usage

### Creating and Configuring a Subscription

```csharp
var subscription = new WebhookSubscription
{
    CallbackUrl = "https://api.example.com/webhooks/receive",
    EventTypes = new[] { "order.created", "order.updated", "payment.completed" },
    Secret = "whsec_a1b2c3d4e5f6g7h8i9j0",
    Active = true,
    RetryPolicy = new WebhookRetryPolicy
    {
        MaxRetries = 5,
        InitialDelayMs = 1000,
        MaxDelayMs = 60000,
        BackoffMultiplier = 2.0
    }
};

await gatewayClient.RegisterSubscriptionAsync(subscription);
Console.WriteLine($"Subscription registered with ID: {subscription.Id}");
```

### Inspecting Delivery Statistics and Handling Failures

```csharp
var subscription = await gatewayClient.GetSubscriptionAsync("sub_9a8b7c6d5e4f");

if (subscription.FailedDeliveries > 0 && subscription.LastError != null)
{
    Console.WriteLine($"Last failure at {subscription.LastDeliveryTime}: {subscription.LastError}");

    if (subscription.FailedDeliveries > subscription.SuccessfulDeliveries * 0.5)
    {
        // Majority of deliveries are failing — disable and investigate
        subscription.Active = false;
        await gatewayClient.UpdateSubscriptionAsync(subscription);
        Console.WriteLine("Subscription disabled due to excessive failures.");
    }
}

Console.WriteLine($"Delivery stats: {subscription.SuccessfulDeliveries}/{subscription.TotalDeliveries} succeeded");
```

## Notes

- The `EventType`, `Timestamp`, `Data`, and `RetryCount` members are meaningful only within the context of a delivered webhook payload. They are not persisted as part of the subscription configuration and should not be accessed on a subscription object retrieved from the management API.
- `DeliveryStats` provides a snapshot of aggregate counters. These counters are updated asynchronously after delivery attempts complete; there may be a short delay before the most recent attempt is reflected in `TotalDeliveries`, `SuccessfulDeliveries`, or `FailedDeliveries`.
- Setting `Active` to `false` prevents new delivery attempts but does not cancel in-flight retries for events already dispatched. To guarantee no further calls, disable the subscription and wait for any pending retry cycles to exhaust or expire.
- The `Secret` value is write-only in many implementations. When retrieving a subscription, the `Secret` property may return a masked or empty value depending on the gateway's security configuration. Store the secret at creation time if you need it for inbound signature verification.
- `MaxRetries`, `InitialDelayMs`, and `MaxDelayMs` are convenience members that mirror the active `RetryPolicy`. Modifying the `RetryPolicy` object and persisting the subscription updates these values accordingly.
- Thread safety: Subscription objects retrieved from the API gateway are not guaranteed to be thread-safe for concurrent mutation. Client code should synchronize access when modifying properties such as `Active` and `RetryPolicy` across multiple threads, or treat retrieved instances as snapshots and replace them atomically.
