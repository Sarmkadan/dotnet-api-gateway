# Route Matching

This document describes how the gateway maps an incoming request (path + HTTP method) to a `GatewayRoute`. It covers the code paths in `Models/CompiledRouteTable.cs`, `Models/GatewayRoute.cs`, and `Services/RoutingService.cs`.

## Overview

1. The routing middleware reads `context.Request.Path.Value` (falling back to `/` when null) and `context.Request.Method`. The query string is not part of the path used for matching.
2. `RoutingService.FindRouteAsync(path, method)` asks the current `CompiledRouteTable` for a match.
3. If no route matches, a `RouteNotFoundException` is thrown (code `INVALID_ROUTE`, HTTP 404).
4. If a route matches, the caller proceeds to target selection (see [Target selection](#target-selection-after-matching)).

## Compiled route table

Routes are not scanned from the repository on each request. `CompiledRouteTable` is built from the routes in the repository and stored as a snapshot:

- Only routes with `IsActive == true` are compiled. Inactive routes are invisible to matching and to `GetAllActiveRoutes()`.
- Routes are indexed in a case-insensitive dictionary keyed by `PathPattern`.
- Routes are also indexed by `Id` (used by `GetRouteById`).

### Duplicate path patterns

The path index is keyed by `PathPattern` alone, not by pattern plus method. If two active routes share the same `PathPattern`, **the later one in compile order silently replaces the earlier one**. For example, two routes both using `/api/orders` (one for `GET`, one for `POST`) do not coexist: only one survives in the path index, and requests for the other method do not match.

To serve several methods on the same path, put them in one route with multiple `AllowedMethods`.

### Hot reload

`RoutingService` subscribes to the repository's change token. When configuration changes, it replaces its reference to the current `CompiledRouteTable` with a newly compiled instance. Lookups in progress keep using the snapshot they started with, so a reload never changes a match mid-lookup.

## Matching algorithm

`CompiledRouteTable.FindRoute(path, method)` runs two phases.

### Phase 1: exact key lookup

The request path is looked up directly in the path index (case-insensitive). The candidate is accepted only if it is active and `SupportsMethod(method)` is true. This means the lookup succeeds only when the request path is byte-for-byte equal to a stored `PathPattern` (ignoring case). Patterns containing `*` or `{...}` can never be found in this phase, because a request path does not contain those literal characters.

### Phase 2: pattern scan

If phase 1 finds nothing usable (no key, inactive route, or method not allowed), the table enumerates **all** path-indexed routes and returns the first one where:

- `IsActive` is true,
- `MatchesPath(path)` is true, and
- `SupportsMethod(method)` is true.

The scan order is the enumeration order of the underlying `ConcurrentDictionary`, which is not a documented guarantee. See [Overlapping patterns](#overlapping-patterns).

If phase 2 finds nothing, the result is `null`.

## Pattern syntax

`GatewayRoute.MatchesPath(requestPath)` compares the pattern and the path segment by segment.

### Segment splitting

- Both the pattern and the request path are split on `/`.
- Empty segments are removed. Leading, trailing, and repeated slashes are therefore ignored on both sides: `/api//users/` is equivalent to `api/users`.

### Segment rules

| Pattern segment | Matches |
|---|---|
| Literal text (e.g. `users`) | The same text in the request segment, compared with `OrdinalIgnoreCase`. |
| `*` | Exactly one request segment, any value. |
| `{name}` (starts with `{`, ends with `}`) | Exactly one request segment, any value. The value is **not captured** and is not available to the caller. |

Rules that do not exist:

- No prefix matching: `/api/users` does not match `/api/users/42`.
- No multi-segment wildcard: `*` and `{...}` match one segment only. `**` is not recognized and behaves like a literal.
- No constraints on placeholders: `{id}` accepts any text, including non-numeric values.
- No query-string handling: the query is not part of the path passed to the matcher.

### Examples

Assuming an active route with `PathPattern = "/api/users/{id}/orders"` and `AllowedMethods = ["GET"]`:

| Request | Method | Result |
|---|---|---|
| `/api/users/42/orders` | GET | Match |
| `/api/USERS/abc/Orders` | GET | Match (case-insensitive) |
| `/api/users/42/orders/` | GET | Match (trailing slash ignored) |
| `/api/users/42` | GET | No match (segment count differs) |
| `/api/users/42/orders` | POST | No match (method not allowed) |

## Method matching

`SupportsMethod` checks `AllowedMethods` with a case-insensitive comparison. A route with an empty `AllowedMethods` array fails validation (`GatewayRoute.Validate`), so a stored route always supports at least one method.

## Overlapping patterns

When more than one active route can match the same request, phase 1 wins if one of the patterns is a literal equal to the path. Otherwise, the winner is decided by phase 2 scan order, which is not defined by the gateway.

Example: routes `/api/users/*` and `/api/users/{id}` both match `/api/users/42`. Either may be returned, and the choice is not guaranteed to be stable across process restarts or table rebuilds.

To avoid this, keep patterns non-overlapping. If two routes must coexist, make their literal segments differ (for example, `/api/users/me` and `/api/users/{id}`), and be aware that the literal route wins only when the request path matches it exactly.

## Target selection after matching

Once a route is found, `RoutingService.SelectTarget(route, clientIp)` picks a backend:

1. Targets with `IsHealthy == false` are removed. If none remain, a `GatewayException` with code `NO_HEALTHY_TARGETS` (503) is thrown.
2. Targets whose circuit is `Open` are removed. If every healthy target has an open circuit, all healthy targets are kept, so that half-open probing can proceed. As a result, the `ALL_CIRCUITS_OPEN` branch in `SelectTarget` is not reached in practice. Targets without a circuit breaker are never removed by this step.
3. The remaining targets are chosen by the configured `LoadBalancingStrategy`:
   - `RoundRobin` (default): shared counter, modulo the number of available targets.
   - `IpHash`: deterministic FNV-1a hash of the client IP. Falls back to round-robin when the IP is empty.
   - `LeastConnections`: connection counts are not tracked, so the target with the lowest `Weight` is chosen.

## Related

- `docs/RoutingService.md` - service API reference
- `docs/GatewayRoute.md` - route model and fields
- `docs/RouteTarget.md` - target model and health/circuit state
