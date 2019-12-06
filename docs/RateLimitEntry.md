# RateLimitEntry

`RateLimitEntry` is a data structure used by the dotnet-api-gateway to track and enforce rate limiting for API requests. It records the current state of a rate-limited key, including the number of tokens consumed, remaining capacity, and the time window for the next available request.

## API

### `Key`
The unique identifier for the rate-limited resource (e.g., client IP, API key, or endpoint path). This value is used to group and track requests under the same rate-limiting policy.

### `Count`
The number of tokens consumed within the current rate-limiting window. This value increments with each request and is reset when the window expires.

### `RemainingTimeSeconds`
The number of seconds remaining until the current rate-limiting window resets. This value is calculated based on the `LastRequest` timestamp and the rate-limiting policy duration.

### `Tokens`
The number of tokens currently available for consumption in the rate-limiting window. This value decreases with each request and is replenished when the window resets.

### `LastRequest`
The timestamp of the most recent request associated with this `RateLimitEntry`. Used to calculate the `RemainingTimeSeconds` and enforce window-based rate limiting.

## Usage

```csharp
// Example 1: Tracking a rate-limited request
var entry = new RateLimitEntry
{
    Key = "client-123",
    Count = 5,
    RemainingTimeSeconds = 30,
    Tokens = 5.0,
    LastRequest = DateTime.UtcNow.AddSeconds(-10)
};

// Example 2: Checking if a request should be allowed
if (entry.Tokens >= 1.0)
{
    entry.Count++;
    entry.Tokens--;
    entry.LastRequest = DateTime.UtcNow;
}
```

## Notes
- Thread safety: All public members are mutable and intended for single-threaded access. Concurrent modifications require external synchronization.
- Edge cases: If `LastRequest` is in the future (e.g., due to clock skew), `RemainingTimeSeconds` may report negative values until corrected.
- State consistency: The relationship between `Count`, `Tokens`, and `RemainingTimeSeconds` depends on the gateway's rate-limiting algorithm. Invalid states (e.g., `Tokens` < 0) may occur during rapid concurrent updates without synchronization.
