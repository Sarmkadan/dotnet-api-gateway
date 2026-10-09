# Load Balancing

This document describes how the gateway distributes requests across the `Targets` of a route. The selection logic lives in `Services/RoutingService.cs` (`SelectTarget`), the strategy enum is `LoadBalancingStrategy` in `Constants/GatewayEnums.cs`, and the per-target model is `RouteTarget` in `Models/RouteTarget.cs`.

## Overview

Each `GatewayRoute` holds an array of `RouteTarget` entries. When a request matches a route, `RoutingService.SelectTarget(route, clientIp)` picks one target in three steps:

1. **Health filter.** Targets with `IsHealthy == false` are removed. If no healthy target remains, the gateway throws `GatewayException` with code `NO_HEALTHY_TARGETS` (HTTP 503).
2. **Circuit breaker filter.** Targets whose circuit is `Open` are removed. If every healthy target has an open circuit, the gateway throws `ALL_CIRCUITS_OPEN` (HTTP 503). If no target has a circuit breaker configured, this step is a no-op.
3. **Strategy.** The remaining targets are passed to the active `LoadBalancingStrategy`.

Health and circuit state are therefore applied before the strategy runs. A strategy never sees an unhealthy or open-circuit target.

## Target Model

| Property | Type | Default | Role in selection |
|---|---|---|---|
| `BaseUrl` | `string` | (required) | Destination used by `GetForwardUrl`. |
| `Weight` | `int` | `1` | Validated to the range 1-100. Used only by the `LeastConnections` fallback described below. Not used by round-robin or IP hash. |
| `IsHealthy` | `bool` | `true` | Set through `UpdateHealthStatus`. Unhealthy targets are excluded. |
| `CircuitState` | `CircuitBreakerState?` | `null` | `null` means no circuit breaker. `Open` excludes the target. |

## Strategies

The enum has three values. The default in `RoutingService` is `RoundRobin`, and the `switch` falls back to round-robin for any unrecognized value.

### RoundRobin (default)

```csharp
var ticket = unchecked((uint)Interlocked.Increment(ref _roundRobinIndex));
return targets[(int)(ticket % (uint)targets.Count)];
```

- A single shared counter (`_roundRobinIndex`, starts at -1 so the first ticket is 0) is incremented atomically with `Interlocked`.
- The `uint` cast and modulo keep the index valid after the counter overflows `int.MaxValue`.
- Requests are distributed evenly across the **filtered** list. When a target becomes unhealthy or its circuit opens, the rotation simply skips it.
- Weights are ignored. Two targets with weights 1 and 9 each receive 50% of traffic.
- The counter is shared by all routes on the same `RoutingService` instance. Distribution is even in aggregate, but per-route sequences are not independent.

### IpHash

- Uses FNV-1a over the client IP string (`ComputeStableHash`), then takes the result modulo the number of available targets.
- A deterministic hash is used on purpose. `string.GetHashCode()` is randomized per process and would move clients between backends after every restart.
- Same client IP gives the same target while the target list is unchanged. The modulo is over the filtered list, so when a target is removed, most clients are remapped. This is not consistent hashing.
- If `clientIp` is null or whitespace, the strategy falls back to round-robin.
- Behind a reverse proxy or CDN, the value passed as `clientIp` must be the real client address, otherwise all traffic lands on one target.

### LeastConnections

This strategy is **not** true least-connections. The gateway does not track in-flight requests per target. The implementation is:

```csharp
return targets.OrderBy(t => t.Weight).First();
```

It always picks the target with the lowest `Weight`. Consequences:

- The same target is selected for every request until it becomes unhealthy or its circuit opens. There is no rotation among equal weights.
- `Weight` is interpreted as "lower means less loaded", which is the opposite of how weight is usually understood in round-robin configurations.
- Do not choose this strategy expecting load-aware distribution. Treat it as a placeholder until active connection counting is added.

## Choosing a Strategy

| Need | Strategy | Notes |
|---|---|---|
| Even spread across stateless backends | RoundRobin | Default. Weights are not honored. |
| Session affinity without a shared session store | IpHash | Needs correct client IP resolution. Remaps clients when targets are added or removed. |
| Load-aware distribution | None available | `LeastConnections` does not implement it. |

## Current Configuration Status

The strategy is **not read from configuration**. `ServiceCollectionExtensions` registers `RoutingService` as a singleton with its constructor defaults, so production instances run `RoundRobin`. The only places that pass a non-default strategy are the unit tests and benchmarks. To use another strategy today, the registration in `Configuration/ServiceCollectionExtensions.cs` has to be changed, or the option has to be added to `DotnetApiGatewayOptions` and bound there.

## Health and Circuit Breaker Interaction

- Health (`IsHealthy`) is a binary flag. `ShouldReceiveTraffic` in `Models/RouteTargetExtensions.cs` allows an unhealthy target to receive traffic again after the health-check interval has passed since its last check, which lets it be probed.
- The circuit breaker is independent. An `Open` circuit removes the target until the breaker moves to `HalfOpen`. In `HalfOpen`, the target stays eligible, so probe requests can reach it.
- If all healthy targets are circuit-open, the request fails fast with `ALL_CIRCUITS_OPEN`. The gateway does not send traffic to a known-failing target as a last resort.

## Example Route

```json
{
  "Id": "orders",
  "Name": "Orders API",
  "PathPattern": "/api/orders/{**rest}",
  "AllowedMethods": ["GET", "POST"],
  "Targets": [
    { "BaseUrl": "https://orders-1.internal:8443", "Weight": 1 },
    { "BaseUrl": "https://orders-2.internal:8443", "Weight": 1 },
    { "BaseUrl": "https://orders-3.internal:8443", "Weight": 2 }
  ]
}
```

With the current `RoundRobin` default, the three targets receive roughly one third of traffic each. The `Weight` of 2 on `orders-3` has no effect. Under `LeastConnections`, `orders-1` would receive all traffic while it is healthy.

## Known Limitations

- Weights are validated but not used by the default strategy.
- No active connection tracking, so no true least-connections.
- IP hash is modulo-based; adding or removing a target remaps most clients.
- Strategy is not configurable from `appsettings.json`.
- Round-robin state is per process. Multiple gateway replicas each run their own rotation, which is even in aggregate but not coordinated.

## Related Documents

- [architecture.md](architecture.md) - where routing sits in the middleware pipeline
- [RoutingService.md](RoutingService.md) - API reference for the service
- [RouteTarget.md](RouteTarget.md) - target model details
- [CircuitBreakerPolicy.md](CircuitBreakerPolicy.md) - circuit states that affect selection
- [RouteTargetExtensions.md](RouteTargetExtensions.md) - health and traffic eligibility helpers
