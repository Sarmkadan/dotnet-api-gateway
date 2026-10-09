# Rate Limiting

This document describes how rate limiting works in the gateway: the policy model, the request pipeline, the algorithms, response headers, and the known limitations of the current implementation.

## Overview

Rate limiting is configured per route through `GatewayRoute.RateLimitPolicy` (`Models/RateLimitPolicy.cs`). A route without a policy, or with `Enabled = false`, is not limited.

Requests are limited by `RateLimitingMiddleware` (`Middleware/RateLimitingMiddleware.cs`), which runs right after `RoutingMiddleware` in `Program.cs`:

```
RoutingMiddleware -> RateLimitingMiddleware -> ExceptionMappingMiddleware -> ...
```

The middleware needs the resolved route, so it must stay after routing.

## Policy Properties

| Property | Default | Meaning |
|---|---|---|
| `RequestsPerMinute` | `1000` | Permit limit per minute (used by Fixed and Sliding Window and as the Token Bucket refill rate). |
| `RequestsPerHour` | `50000` | Hourly limit. Used only when `RequestsPerMinute` is `0` (see Limitations). |
| `Strategy` | `TokenBucket` | `TokenBucket`, `SlidingWindow` or `FixedWindow` (`RateLimitStrategy`). |
| `KeyGenerator` | `ClientIp` | Partition key source: `ClientIp`, `AuthenticatedUser`, `RouteId`, `CustomHeader:<name>`. Any other value falls back to `global`. |
| `BypassForAuthenticatedUsers` | `false` | Skip limiting for requests where `HttpContext.User` is authenticated. |
| `BurstSize` | `10` | Token bucket capacity. Must be between 1 and `RequestsPerMinute`. |
| `Enabled` | `true` | Turns the policy on or off. |
| `StorageType` | `InMemory` | `InMemory` or `Redis` (see Limitations). |
| `RedisConnectionString` | `null` | Required when `StorageType` is `Redis`. Never logged. |

### Validation

`RateLimitPolicy.Validate()` enforces:

- `RequestsPerMinute >= 1`
- `RequestsPerHour >= 1`
- `RequestsPerHour >= RequestsPerMinute`
- `1 <= BurstSize <= RequestsPerMinute`

`ConfigurationValidator.ValidateRateLimitPolicy` runs the same kind of checks for every route that has a policy at startup.

## Request Flow

1. The middleware looks up the route in `HttpContext.Items`. No route means no limiting.
2. If the policy is missing or disabled, the request passes through.
3. The key is built from `KeyGenerator`. An empty key bypasses limiting with a warning.
4. If `BypassForAuthenticatedUsers` is set and the user is authenticated, the request passes through.
5. `RateLimitingService.IsAllowedAsync` acquires one permit from the partitioned limiter for `{key}_{policy.Id}`.
6. If the permit is denied, the gateway responds with `429 Too Many Requests` and body `Too Many Requests`. Otherwise the request continues.

Limiters are created lazily per key and policy. Limiters older than one hour are disposed on the next limiter lookup.

## Algorithms

Limiters are built in `Services/BuiltInRateLimiter.cs` on top of `System.Threading.RateLimiting`:

- **TokenBucket** (default): capacity `BurstSize`, one token added every `60 / RequestsPerMinute` seconds, oldest-first queue order.
- **FixedWindow**: `RequestsPerMinute` permits per 1-minute window. Falls back to `RequestsPerHour` per 1-hour window when `RequestsPerMinute` is `0`.
- **SlidingWindow**: `RequestsPerMinute` permits over a sliding 1-minute window with one segment.

Any unknown strategy value uses FixedWindow.

## Response Headers

Headers are set on every response that passes through the middleware, including allowed requests:

| Header | Meaning |
|---|---|
| `X-RateLimit-Limit` | Limit reported by the limiter. |
| `X-RateLimit-Remaining` | Remaining permits reported by the limiter. |
| `X-RateLimit-Reset` | Seconds until the next reset, as reported by the limiter. |
| `RateLimit-Limit` | Same value as `X-RateLimit-Limit` (RFC draft style). |
| `RateLimit-Remaining` | Same value as `X-RateLimit-Remaining`. |
| `Retry-After` | Set on both 429 and allowed responses (see Limitations). |

The 429 response carries the same headers. The `Retry-After` value is the reset value in seconds.

## Error Handling

`RateLimitExceededException` (`Exceptions/`) maps to `429` in `ExceptionMappingMiddleware`. The middleware itself writes the 429 directly and does not throw this exception.

## Limitations

These are behaviors of the current code that operators should know about:

- **Redis storage is not used by the request path.** `RateLimitingService` always uses the in-memory `System.Threading.RateLimiting` limiters. `StorageType = Redis` and `RateLimitStoreFactory` (which returns `RedisRateLimitStore`) are not consulted when a request is limited, so counters are per process and are not shared across instances.
- **Fail-open on errors.** If acquiring a permit throws, `IsAllowedAsync` logs the error and allows the request.
- **`RequestsPerHour` is mostly ignored.** The window is set from `RequestsPerMinute` whenever it is greater than `0`. The hourly limit applies only when `RequestsPerMinute` is `0`, which `Validate()` rejects.
- **`Retry-After` on allowed responses.** The middleware sets `Retry-After` on successful responses too. RFC 9110 defines it as a hint for rejected or unavailable responses (429, 503), so clients should read it only from 429 responses.
- **Limiter cleanup is lazy.** Idle limiters are disposed only when another limiter is requested. Memory for many distinct keys (for example, one per client IP) is released slowly.
- **Limiter reset per key is not supported.** `RateLimitingService` can not reset a single key with the current limiter model.

## Configuration Example

A route-level policy (the `RateLimitPolicy` object inside a `GatewayRoute`):

```json
{
  "RateLimitPolicy": {
    "RequestsPerMinute": 120,
    "RequestsPerHour": 5000,
    "Strategy": "TokenBucket",
    "KeyGenerator": "ClientIp",
    "BurstSize": 20,
    "BypassForAuthenticatedUsers": false,
    "Enabled": true,
    "StorageType": "InMemory"
  }
}
```

Property names follow `Models/RateLimitPolicy.cs`. See `docs/configuration-reference.md` for the full route schema.

## Related Files

- `Models/RateLimitPolicy.cs`, `Models/RateLimitEntry.cs`
- `Middleware/RateLimitingMiddleware.cs`
- `Services/RateLimitingService.cs`, `Services/BuiltInRateLimiter.cs`
- `Repositories/IRateLimitStore.cs`, `InMemoryRateLimitStore.cs`, `RedisRateLimitStore.cs`, `RateLimitStoreFactory.cs`
- `Utilities/RateLimitMetrics.cs`
- `Constants/GatewayConstants.cs` (header names)
