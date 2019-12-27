# WebhookSubscriptionExtensions

Provides static utility methods for evaluating the operational state and delivery performance of webhook subscriptions. These members expose key metrics and lifecycle checks—such as whether a subscription is currently active, its success rate over recent deliveries, the delay before the next retry attempt, and whether the maximum retry threshold has been breached—without requiring direct access to the underlying subscription object.

## API

### `IsActive`

```csharp
public static bool IsActive(WebhookSubscription subscription)
```

Determines whether the given subscription is in an active state, meaning it is eligible to receive event deliveries. Returns `true` if the subscription is active; otherwise `false`. Throws an `ArgumentNullException` if `subscription` is `null`.

### `GetSuccessRate`

```csharp
public static int GetSuccessRate(WebhookSubscription subscription)
```

Calculates the recent delivery success rate for the subscription as an integer percentage (0–100). The rate is derived from a sliding window of the most recent delivery attempts. Returns the computed success percentage. Throws an `ArgumentNullException` if `subscription` is `null`.

### `GetNextRetryDelay`

```csharp
public static int GetNextRetryDelay(WebhookSubscription subscription)
```

Returns the delay, in seconds, before the next automatic retry attempt for a subscription that has experienced a delivery failure. If no retry is scheduled, the return value is zero. Throws an `ArgumentNullException` if `subscription` is `null`.

### `HasExceededMaxRetries`

```csharp
public static bool HasExceededMaxRetries(WebhookSubscription subscription)
```

Indicates whether the subscription has exhausted its configured maximum number of consecutive retry attempts. Returns `true` if the retry limit has been reached or exceeded; otherwise `false`. Throws an `ArgumentNullException` if `subscription` is `null`.

## Usage

### Example 1: Filtering Active Subscriptions with High Success Rates

```csharp
IEnumerable<WebhookSubscription> subscriptions = GetSubscriptionsForTenant(tenantId);

var reliableSubscriptions = subscriptions
    .Where(s => WebhookSubscriptionExtensions.IsActive(s))
    .Where(s => WebhookSubscriptionExtensions.GetSuccessRate(s) >= 95)
    .ToList();

Console.WriteLine($"Found {reliableSubscriptions.Count} reliable, active subscriptions.");
```

### Example 2: Handling a Subscription That Has Exceeded Retries

```csharp
var subscription = GetSubscriptionById(subscriptionId);

if (WebhookSubscriptionExtensions.HasExceededMaxRetries(subscription))
{
    Console.WriteLine("Subscription has exceeded maximum retries. Manual intervention required.");
    DisableSubscription(subscription);
}
else if (!WebhookSubscriptionExtensions.IsActive(subscription))
{
    int delay = WebhookSubscriptionExtensions.GetNextRetryDelay(subscription);
    Console.WriteLine($"Subscription is inactive. Next retry in {delay} seconds.");
}
```

## Notes

- All methods throw `ArgumentNullException` when a `null` subscription is passed; callers must guard against null references before invocation.
- `GetSuccessRate` returns a cached value computed from a fixed-size delivery history window. The result may not reflect deliveries that occurred after the last internal aggregation cycle.
- `GetNextRetryDelay` returns zero both when no retry is pending and when the subscription is active. Callers should combine it with `IsActive` or `HasExceededMaxRetries` to distinguish between these cases.
- `HasExceededMaxRetries` returns `true` only after the final retry attempt has been made and failed. A subscription that is still within its retry window will return `false` even if it is currently failing.
- These methods are stateless static extensions and are safe to call concurrently from multiple threads, provided the underlying `WebhookSubscription` instance is not being mutated during the call. No internal locking is performed.
