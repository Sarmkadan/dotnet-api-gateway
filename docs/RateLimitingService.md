# RateLimitingService

`RateLimitingService` provides a configurable, in-memory rate-limiting mechanism for controlling the frequency of operations on a per-key basis. It tracks allowed requests, remaining capacity, and the time window until the next reset, making it suitable for enforcing throttling policies in API gateways or similar middleware.

## API

### public RateLimitingService

Constructs a new instance of the rate-limiting service. The constructor accepts configuration parameters that define the maximum number of allowed requests and the time window in which they are counted.

### public async Task\<bool> IsAllowedAsync

Determines whether a request identified by a given key is permitted under the current rate limit.

- **Parameters:** A string key identifying the caller or operation subject to the limit.
- **Returns:** `true` if the request is allowed; `false` if the limit has been exhausted for the current window.
- **Exceptions:** Throws `ArgumentNullException` when the key is `null`. Throws `ObjectDisposedException` if the instance has been disposed.

### public async Task\<RateLimitInfo> GetRateLimitInfoAsync

Retrieves the current rate-limit state for a specified key without consuming a request.

- **Parameters:** A string key identifying the caller or operation.
- **Returns:** A `RateLimitInfo` object containing the configured limit, remaining allowed requests, and the time (in seconds) until the window resets.
- **Exceptions:** Throws `ArgumentNullException` when the key is `null`. Throws `ObjectDisposedException` if the instance has been disposed.

### public async Task ResetKeyLimitsAsync

Resets all counters and the time window for a specific key, effectively granting a fresh set of allowed requests.

- **Parameters:** A string key whose limits should be cleared.
- **Returns:** A task representing the asynchronous operation.
- **Exceptions:** Throws `ArgumentNullException` when the key is `null`. Throws `ObjectDisposedException` if the instance has been disposed.

### public async Task ResetAllLimitsAsync

Resets all counters and time windows for every tracked key in the service.

- **Returns:** A task representing the asynchronous operation.
- **Exceptions:** Throws `ObjectDisposedException` if the instance has been disposed.

### public void Dispose

Releases all resources held by the service and marks the instance as unusable. Any subsequent calls to other members will throw `ObjectDisposedException`.

### public int Limit

Gets the maximum number of requests allowed per window for each key. This value is set during construction and remains constant for the lifetime of the instance.

### public int Remaining

Gets the number of requests still available in the current window for the most recently evaluated key. This value reflects the state after the last call to `IsAllowedAsync` or `GetRateLimitInfoAsync`.

### public int Reset

Gets the number of seconds remaining until the current window resets for the most recently evaluated key. This value reflects the state after the last call to `IsAllowedAsync` or `GetRateLimitInfoAsync`.

## Usage

```csharp
// Example 1: Enforcing a per-client rate limit in a gateway pipeline
var rateLimiter = new RateLimitingService(maxRequests: 100, windowSeconds: 60);

string clientKey = "client-abc-123";

if (await rateLimiter.IsAllowedAsync(clientKey))
{
    // Forward the request downstream
    Console.WriteLine("Request allowed.");
}
else
{
    var info = await rateLimiter.GetRateLimitInfoAsync(clientKey);
    Console.WriteLine($"Rate limit exceeded. Retry after {info.Reset} seconds.");
}
```

```csharp
// Example 2: Admin endpoint that resets limits for a misbehaving client
var rateLimiter = new RateLimitingService(maxRequests: 50, windowSeconds: 30);

// After detecting a client is being unfairly throttled due to a bug
string clientKey = "buggy-client-456";
await rateLimiter.ResetKeyLimitsAsync(clientKey);

// Verify the reset
var info = await rateLimiter.GetRateLimitInfoAsync(clientKey);
Console.WriteLine($"Remaining: {info.Remaining}, Reset in: {info.Reset}s");
// Output: Remaining: 50, Reset in: 30s
```

## Notes

- The `Remaining` and `Reset` properties reflect the state of the *last key* accessed via `IsAllowedAsync` or `GetRateLimitInfoAsync`. They are not globally meaningful and should be read immediately after the relevant async call to avoid race conditions in concurrent environments.
- `IsAllowedAsync` both evaluates and consumes a request atomically for the given key. Calling `GetRateLimitInfoAsync` does not consume a request and can be used for inspection-only scenarios.
- All public async methods are thread-safe with respect to internal state; concurrent calls for the same or different keys are handled correctly without external synchronization.
- Once `Dispose` is called, the instance enters a terminal state. Any further method or property access will throw `ObjectDisposedException`. There is no mechanism to re-enable a disposed instance.
- The service operates entirely in memory. State is lost on process restart. For persistent or distributed rate limiting, an external store must be integrated separately.
- `ResetKeyLimitsAsync` and `ResetAllLimitsAsync` immediately discard all tracking data for the affected keys, including the current window start time. The next request will begin a fresh window.
