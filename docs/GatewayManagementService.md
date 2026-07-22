# GatewayManagementService

Central management service for API gateway routes, circuit breakers, rate limiting, and operational metrics. Provides CRUD operations for gateway routes with integrated observability, and administrative controls for resilience and throttling policies across the gateway cluster.

## API

### `public GatewayManagementService(...)`

Initializes a new instance of the gateway management service. Dependencies typically include route repository, metrics collector, circuit breaker registry, and rate limiter store.

**Parameters**  
- `routeRepository` — Persistence abstraction for `GatewayRoute` entities.  
- `metricsCollector` — Aggregates per-route and global traffic metrics.  
- `circuitBreakerRegistry` — Manages circuit breaker state per downstream service.  
- `rateLimiterStore` — Distributed store for rate limit counters and policies.

**Throws**  
- `ArgumentNullException` — Any required dependency is null.

---

### `public async Task<GatewayRoute> CreateRouteAsync(GatewayRoute route)`

Creates a new gateway route configuration and persists it.

**Parameters**  
- `route` — Route definition including path template, upstream target, policies, and metadata. Must have a unique identifier or one will be generated.

**Returns**  
The persisted `GatewayRoute` with assigned identifier and timestamps.

**Throws**  
- `ArgumentNullException` — `route` is null.  
- `InvalidOperationException` — Route conflicts with existing path/template.  
- `DbUpdateException` — Persistence failure.

---

### `public async Task<IEnumerable<object>> GetAllRoutesWithMetricsAsync()`

Retrieves all configured routes enriched with their current runtime metrics.

**Returns**  
Collection of anonymous objects containing route configuration and metric snapshots (request count, latency percentiles, error rate, last access).

**Throws**  
- `InvalidOperationException` — Metrics collector unavailable.

---

### `public async Task<(GatewayRoute? route, object? metrics)> GetRouteByIdWithMetricsAsync(string routeId)`

Fetches a single route by its identifier along with its associated metrics.

**Parameters**  
- `routeId` — Unique route identifier.

**Returns**  
Tuple of route (null if not found) and metrics snapshot (null if route not found or metrics unavailable).

**Throws**  
- `ArgumentException` — `routeId` is null or empty.

---

### `public async Task<GatewayRoute> UpdateRouteAsync(string routeId, GatewayRoute updatedRoute)`

Replaces an existing route configuration.

**Parameters**  
- `routeId` — Identifier of the route to update.  
- `updatedRoute` — New route definition; identifier in this object is ignored.

**Returns**  
The updated `GatewayRoute` with revised configuration and updated timestamp.

**Throws**  
- `ArgumentException` — `routeId` is null or empty.  
- `ArgumentNullException` — `updatedRoute` is null.  
- `KeyNotFoundException` — No route exists with `routeId`.  
- `InvalidOperationException` — Updated route conflicts with another existing route.

---

### `public async Task<bool> DeleteRouteAsync(string routeId)`

Removes a route configuration from the gateway.

**Parameters**  
- `routeId` — Identifier of the route to delete.

**Returns**  
`true` if the route existed and was removed; `false` if no route matched.

**Throws**  
- `ArgumentException` — `routeId` is null or empty.

---

### `public async Task<object> GetRouteMetricsAsync(string routeId)`

Returns detailed metrics for a specific route.

**Parameters**  
- `routeId` — Route identifier.

**Returns**  
Object containing request throughput, latency histogram, error breakdown, active connections, and time-series buckets.

**Throws**  
- `ArgumentException` — `routeId` is null or empty.  
- `KeyNotFoundException` — Route not found.

---

### `public async Task<IEnumerable<object>> GetCircuitBreakerStatusesAsync()`

Retrieves current state of all circuit breakers registered in the gateway.

**Returns**  
Collection of objects each describing a circuit breaker: name, state (Closed/Open/HalfOpen), failure count, last failure timestamp, next attempt timestamp.

**Throws**  
- `InvalidOperationException` — Circuit breaker registry unavailable.

---

### `public async Task<object> ResetCircuitBreakerAsync(string breakerName)`

Manually forces a circuit breaker into the Closed state, clearing failure counters.

**Parameters**  
- `breakerName` — Name of the circuit breaker to reset.

**Returns**  
Object confirming the reset with previous state and new state.

**Throws**  
- `ArgumentException` — `breakerName` is null or empty.  
- `KeyNotFoundException` — No circuit breaker registered under that name.

---

### `public async Task<object> GetRateLimitStatusAsync(string policyName, string key)`

Queries current consumption for a rate limit policy and key.

**Parameters**  
- `policyName` — Rate limit policy identifier.  
- `key` — Partition key (e.g., client IP, API key, tenant ID).

**Returns**  
Object with limit, remaining, reset window timestamp, and whether the key is currently blocked.

**Throws**  
- `ArgumentException` — `policyName` or `key` is null or empty.  
- `KeyNotFoundException` — Policy not found.

---

### `public async Task ResetRateLimitForKeyAsync(string policyName, string key)`

Resets the rate limit counter for a specific key under a policy, effectively unblocking it.

**Parameters**  
- `policyName` — Rate limit policy identifier.  
- `key` — Partition key to reset.

**Throws**  
- `ArgumentException` — `policyName` or `key` is null or empty.  
- `KeyNotFoundException` — Policy not found.

---

### `public async Task ResetAllRateLimitsAsync(string policyName)`

Clears all rate limit counters for a given policy across all keys.

**Parameters**  
- `policyName` — Rate limit policy identifier.

**Throws**  
- `ArgumentException` — `policyName` is null or empty.  
- `KeyNotFoundException` — Policy not found.

---

### `public async Task<object> GetGlobalMetricsAsync()`

Returns cluster-wide aggregated metrics across all routes.

**Returns**  
Object containing total request volume, global error rate, latency percentiles, active circuit breakers, rate limit saturation, and top routes by traffic.

**Throws**  
- `InvalidOperationException` — Metrics collector unavailable.

## Usage

### Example 1: Provision a new route with policies and verify metrics

```csharp
var route = new GatewayRoute
{
    PathTemplate = "/api/v1/orders/{**catchall}",
    UpstreamTemplate = "https://orders-service.internal/{**catchall}",
    Methods = new[] { "GET", "POST" },
    RateLimitPolicy = "standard",
    CircuitBreakerPolicy = "orders-cb",
    Timeout = TimeSpan.FromSeconds(30),
    Metadata = new Dictionary<string, string> { { "team", "commerce" } }
};

GatewayRoute created = await _gatewayManagement.CreateRouteAsync(route);

// After traffic flows, inspect combined view
var allWithMetrics = await _gatewayManagement.GetAllRoutesWithMetricsAsync();
var myRouteMetrics = allWithMetrics.FirstOrDefault(r => 
    ((GatewayRoute)r.GetType().GetProperty("Route")!.GetValue(r)!).Id == created.Id);
```

### Example 2: Operational runbook — reset tripped circuit breaker and clear rate limit for a tenant

```csharp
// Detect open breaker via status endpoint
var statuses = await _gatewayManagement.GetCircuitBreakerStatusesAsync();
var ordersBreaker = statuses.FirstOrDefault(s => 
    (string)s.GetType().GetProperty("Name")!.GetValue(s)! == "orders-cb");

if (ordersBreaker != null && 
    (string)ordersBreaker.GetType().GetProperty("State")!.GetValue(ordersBreaker)! == "Open")
{
    await _gatewayManagement.ResetCircuitBreakerAsync("orders-cb");
}

// Tenant "acme-corp" hit rate limit; unblock them
await _gatewayManagement.ResetRateLimitForKeyAsync("standard", "tenant:acme-corp");

// Verify global health after remediation
var global = await _gatewayManagement.GetGlobalMetricsAsync();
```

## Notes

- **Thread safety**: All public methods are safe for concurrent calls. Internal synchronization relies on the underlying repositories and stores (typically distributed locks or optimistic concurrency). Callers should not assume atomicity across multiple method calls.
- **Idempotency**: `CreateRouteAsync` is not idempotent; repeated calls with the same natural key will throw `InvalidOperationException`. `DeleteRouteAsync` and `Reset*` methods are idempotent — repeating them returns success without side effects.
- **Metrics freshness**: Metrics objects are point-in-time snapshots. High-traffic routes may show stale counters if polled faster than the collector's aggregation interval (typically 10–30 seconds).
- **Circuit breaker reset**: `ResetCircuitBreakerAsync` bypasses the normal half-open probe sequence. Use sparingly; prefer letting the breaker self-heal unless manual intervention is required by runbook.
- **Rate limit keys**: Keys are arbitrary strings. The convention `tenant:{id}`, `ip:{address}`, or `apikey:{hash}` is recommended but not enforced. `ResetAllRateLimitsAsync` is a heavy operation — it scans the entire key space for the policy and may incur latency on large clusters.
- **Route updates**: `UpdateRouteAsync` replaces the entire route definition. Partial updates are not supported; callers must fetch, modify, and resubmit the full object.
- **Error handling**: Methods throwing `KeyNotFoundException` indicate missing configuration (route, policy, breaker). Treat as 404-equivalent in API layers. `InvalidOperationException` typically signals cross-entity conflicts or unavailable subsystems.
