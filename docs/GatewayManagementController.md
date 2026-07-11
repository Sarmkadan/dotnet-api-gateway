# GatewayManagementController

The `GatewayManagementController` serves as the administrative API surface for managing the runtime configuration of the API gateway. It exposes endpoints to create, read, update, and delete downstream route definitions, as well as to inspect and reset operational state such as circuit breakers, rate limit counters, and aggregated metrics. All methods return `Task<IActionResult>` and are designed to be consumed by management dashboards or automation tooling.

## API

### `GatewayManagementController`
Constructor. Initializes a new instance of the controller with the required gateway management dependencies injected via the framework.

### `async Task<IActionResult> CreateRoute`
Creates a new route definition in the gateway configuration store and activates it for traffic routing.

- **Parameters** (from request body): A route model containing upstream path pattern, downstream target URL, HTTP methods, timeout, and optional policies.
- **Returns**: `201 Created` with the persisted route object on success; `400 BadRequest` when validation fails; `409 Conflict` if a route with the same key already exists.
- **Throws**: `ArgumentException` when required fields are missing or malformed; `InvalidOperationException` if the underlying configuration store is unreachable.

### `async Task<IActionResult> GetAllRoutes`
Retrieves all currently registered routes, including inactive ones.

- **Parameters**: None (optionally accepts query flags for filtering, e.g. `?activeOnly=true`).
- **Returns**: `200 OK` with a collection of route objects; `204 NoContent` when no routes exist.
- **Throws**: `InvalidOperationException` if the configuration store cannot be queried.

### `async Task<IActionResult> GetRouteById`
Fetches a single route by its unique identifier.

- **Parameters**: `string id` from the route segment.
- **Returns**: `200 OK` with the route object; `404 NotFound` when the identifier does not match any route.
- **Throws**: `ArgumentException` when `id` is null or whitespace.

### `async Task<IActionResult> UpdateRoute`
Replaces an existing route definition entirely. This is a full update; partial merges are not performed.

- **Parameters**: `string id` from the route segment, and a complete route model from the request body.
- **Returns**: `200 OK` with the updated route; `400 BadRequest` on validation failure; `404 NotFound` when the route does not exist.
- **Throws**: `ArgumentException` for invalid input; `InvalidOperationException` if the store write fails.

### `async Task<IActionResult> DeleteRoute`
Removes a route definition and immediately stops traffic from being forwarded to its downstream target.

- **Parameters**: `string id` from the route segment.
- **Returns**: `204 NoContent` on successful deletion; `404 NotFound` when the route does not exist.
- **Throws**: `ArgumentException` when `id` is null or whitespace; `InvalidOperationException` if the store operation fails.

### `async Task<IActionResult> GetRouteMetrics`
Returns performance counters for a specific route, such as request count, average latency, error rate, and throughput over a configurable window.

- **Parameters**: `string id` from the route segment; optional query parameters for time window (`?window=5m`).
- **Returns**: `200 OK` with a metrics payload; `404 NotFound` when the route does not exist or has no recorded metrics.
- **Throws**: `ArgumentException` for invalid time window formats.

### `async Task<IActionResult> GetCircuitBreakerStatuses`
Lists the current state of all circuit breakers associated with routes, including open/closed/half-open status, failure count, and cooldown remaining.

- **Parameters**: None.
- **Returns**: `200 OK` with a dictionary keyed by route identifier containing breaker status objects; `204 NoContent` when no circuit breakers are configured.
- **Throws**: `InvalidOperationException` if the circuit breaker subsystem is unavailable.

### `async Task<IActionResult> ResetCircuitBreaker`
Forcibly transitions a circuit breaker back to the closed state, clearing its failure counter and cooldown timer.

- **Parameters**: `string routeId` from query or route segment.
- **Returns**: `200 OK` with the updated breaker status; `404 NotFound` when no breaker exists for the given route.
- **Throws**: `ArgumentException` when `routeId` is missing; `InvalidOperationException` if the reset command cannot be delivered.

### `async Task<IActionResult> GetRateLimitStatus`
Retrieves the current consumption state for a rate-limited resource, including remaining tokens, reset time, and whether the limit is currently enforced.

- **Parameters**: `string key` identifying the rate limit bucket (e.g. client IP or API key).
- **Returns**: `200 OK` with the rate limit status object; `404 NotFound` when the key has no active limit bucket.
- **Throws**: `ArgumentException` when `key` is null or empty.

### `async Task<IActionResult> ResetRateLimitForKey`
Resets the token bucket for a single rate limit key, immediately restoring its full capacity.

- **Parameters**: `string key` from the request.
- **Returns**: `200 OK` with the new status; `404 NotFound` when the key does not exist.
- **Throws**: `ArgumentException` for missing key; `InvalidOperationException` if the rate limit store is unreachable.

### `async Task<IActionResult> ResetAllRateLimits`
Resets every active rate limit bucket across all keys. This is a bulk operation that clears all counters.

- **Parameters**: None.
- **Returns**: `200 OK` with a count of reset buckets; `204 NoContent` when no buckets were active.
- **Throws**: `InvalidOperationException` if the rate limit store is unreachable.

### `async Task<IActionResult> GetGlobalMetrics`
Returns aggregate gateway-wide metrics, including total requests, global error rate, active connection count, and overall throughput.

- **Parameters**: Optional query parameter for time window (`?window=10m`).
- **Returns**: `200 OK` with a global metrics payload; `204 NoContent` when no data has been recorded yet.
- **Throws**: `ArgumentException` for invalid window formats.

## Usage

### Example 1: Register a new route and inspect its metrics
```csharp
// Assume controller is resolved via dependency injection
var newRoute = new RouteDefinition
{
    UpstreamPath = "/api/orders/**",
    DownstreamUrl = "https://orders-service.internal:8080",
    Methods = new[] { "GET", "POST" },
    TimeoutSeconds = 30
};

var createResult = await controller.CreateRoute(newRoute) as CreatedResult;
var createdRoute = createResult.Value as RouteDefinition;

// Later, retrieve metrics for the newly created route
var metricsResult = await controller.GetRouteMetrics(createdRoute.Id, window: "5m") as OkObjectResult;
var metrics = metricsResult.Value as RouteMetrics;
Console.WriteLine($"Avg latency: {metrics.AverageLatencyMs} ms");
```

### Example 2: Monitor and reset a tripped circuit breaker
```csharp
// Check all circuit breaker statuses
var statusResult = await controller.GetCircuitBreakerStatuses() as OkObjectResult;
var breakers = statusResult.Value as Dictionary<string, CircuitBreakerStatus>;

foreach (var kvp in breakers)
{
    if (kvp.Value.State == CircuitState.Open)
    {
        Console.WriteLine($"Breaker for route {kvp.Key} is open. Resetting...");
        await controller.ResetCircuitBreaker(kvp.Key);
    }
}

// Verify reset
var updatedResult = await controller.GetCircuitBreakerStatuses() as OkObjectResult;
var updatedBreakers = updatedResult.Value as Dictionary<string, CircuitBreakerStatus>;
Console.WriteLine($"All breakers closed: {updatedBreakers.All(b => b.Value.State == CircuitState.Closed)}");
```

## Notes

- **Idempotency**: `DeleteRoute` returns `204 NoContent` even if the route was already removed; callers should treat this as success. `ResetCircuitBreaker` and `ResetRateLimitForKey` are similarly idempotent for non-existent targets, returning `404` to distinguish between “already reset” and “never existed.”
- **Concurrency**: Route mutations (`CreateRoute`, `UpdateRoute`, `DeleteRoute`) are serialized through the configuration store to prevent race conditions. However, simultaneous reads (`GetAllRoutes`, `GetRouteById`) may return stale data for the duration of a write transaction. Metrics and status endpoints read from eventually-consistent counters and may lag behind the most recent request by a few seconds.
- **Thread Safety**: The controller itself is stateless and safe for concurrent invocation across requests. Underlying stores and subsystems (circuit breaker state, rate limit buckets) use atomic operations internally; no external locking is required by callers.
- **Bulk Operations**: `ResetAllRateLimits` affects every key in the store. On large deployments with many distinct keys, this may cause a brief CPU spike as counters are zeroed. Consider scheduling such resets during maintenance windows.
- **Metrics Windows**: The `window` parameter on `GetRouteMetrics` and `GetGlobalMetrics` accepts values like `1m`, `5m`, `15m`, `1h`. Values outside the supported range cause an `ArgumentException`. If no data exists for the requested window, a `204 NoContent` is returned rather than an empty payload.
- **Circuit Breaker Reset Semantics**: Resetting a breaker clears the failure counter and immediately closes the circuit, allowing traffic to flow. This does not guarantee the downstream service has recovered; callers should verify health independently before resetting.
