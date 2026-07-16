# Architecture

This document describes how the gateway is actually wired today - what runs, in what order, and why it is built the way it is. If the code and this file ever disagree, the code wins; fix the doc.

## Overview

DotNetApiGateway is a single ASP.NET Core project (no separate class libraries) that accepts HTTP requests, matches them against configured `GatewayRoute` definitions, and forwards them to upstream targets. Cross-cutting behavior - rate limiting, circuit breaking, transformation, aggregation, metrics - is attached per route through policy objects on the route model rather than global configuration.

Everything is in-process and in-memory by default: route storage, rate limit counters, circuit breaker state and metrics all live in singletons. That is a deliberate choice (see Design decisions), not an accident.

## Project layout

| Folder | Contents |
|---|---|
| `Program.cs` | Composition root: DI setup, middleware order, minimal-API endpoints, and the fallback forwarding endpoint |
| `Configuration/` | `DotnetApiGatewayOptions` (bound from the `DotnetApiGateway` config section, validated on start), `ConfigurationValidator`, `ServiceCollectionExtensions` (`AddGatewayServices`, `UseAllGatewayMiddleware`) |
| `Middleware/` | `ErrorHandlingMiddleware`, `RequestValidationMiddleware`, `RequestLoggingMiddleware`, `RoutingMiddleware`, `RateLimitingMiddleware`, `PerformanceMonitoringMiddleware`, `GatewayMiddleware` - each with its own `Use*` extension |
| `Services/` | The domain logic: `RoutingService`, `RateLimitingService`, `CircuitBreakerService`, `JwtValidationService`, `MetricsService`, `RequestTransformationService`, `ResponseTransformationService` (`IResponseTransformer`), `RequestAggregationService`, `RequestCoalescingService`, `CacheService`, `ApiVersioningService`, `AnalyticsService`, `HealthCheckService`, plus decorators (`RequestCachingDecorator`, `RequestInterceptor`) |
| `Repositories/` | `GatewayRouteRepository` and `CircuitBreakerRepository` (dictionary-backed, lock-guarded), the rate limit store abstraction (`IRateLimitStore`, `InMemoryRateLimitStore`, `RedisRateLimitStore`, `RateLimitStoreFactory`) |
| `Models/` | `GatewayRoute`, `RouteTarget` and the policy objects hung off a route: `RateLimitPolicy`, `CircuitBreakerPolicy`, `AuthenticationPolicy`, `CachePolicy`, `AggregationPolicy`, `RequestCoalescingPolicy`, `ApiVersioningPolicy`, `TransformationRule` |
| `Integration/` | Outbound side: `ExternalApiClient` (typed HttpClient wrapper), `RetryPolicy` (exponential backoff), `WebhookRegistry`, `HttpClientFactory` helpers |
| `Controllers/` | Management surface: `GatewayManagementController`, `AdminDashboardController`, `WebhookManagementController`, `RequestTransformationController` |
| `BackgroundServices/` | `CacheCleanupWorker`, `HealthCheckWorker`, `MetricsExportWorker` (hosted-service style workers) |
| `Events/` | `EventBus` - lightweight in-process pub/sub |
| `Formatters/`, `Utilities/`, `Constants/`, `Exceptions/` | Support code; exceptions carry `StatusCode`/`ErrorCode` so the error path can map them straight onto HTTP responses |
| `tests/`, `benchmarks/`, `examples/` | xUnit test project, BenchmarkDotNet project, runnable usage samples |

## Request flow (as wired in Program.cs)

The pipeline `Program.cs` actually builds is intentionally minimal:

```
request
  -> HTTPS redirect, authorization
  -> RoutingMiddleware        (resolve GatewayRoute, stash in HttpContext.Items)
  -> RateLimitingMiddleware   (enforce the resolved route's RateLimitPolicy)
  -> explicit endpoints       (/health, /gateway/info, /gateway/routes,
                               /gateway/stats, /gateway/circuit-breakers,
                               attribute-routed controllers)
  -> MapFallback              (everything else = proxy traffic)
```

The fallback endpoint is the proxy core. In order it:

1. Checks `HttpContext.Items` for a `RouteNotFoundException` / resolution error left by `RoutingMiddleware` and returns 404/500 accordingly.
2. Pulls the resolved `GatewayRoute` from `Items["GatewayRoute"]`.
3. If the route has an enabled `AggregationPolicy`, delegates to `RequestAggregationService.AggregateAsync` (fan-out to multiple upstreams, merged response) and returns.
4. Otherwise picks a target via `RoutingService.SelectTarget` (round-robin, IP-hash or least-connections over healthy targets), builds the forward URL (honoring the stripped path from `Items["StrippedPath"]`), buffers and copies the body and headers, applies request-phase `TransformationRule`s, sends through `ExternalApiClient` with a per-target timeout (`CancellationTokenSource`), applies `IResponseTransformer` plus response-phase rules, and streams the upstream response back.

Middleware communicates with the fallback endpoint via `HttpContext.Items` keys (`GatewayRoute`, `StrippedPath`, `RouteNotFoundException`, `RouteResolutionError`). That is a stringly-typed contract - cheap and transparent, but if you rename a key, nothing warns you at compile time.

Note that `UseAllGatewayMiddleware` (error handling + validation + logging + performance monitoring + `GatewayMiddleware`) exists in `ServiceCollectionExtensions` but `Program.cs` deliberately wires only routing + rate limiting and lets `MapFallback` do the forwarding. The full middleware chain is the "batteries included" option for hosts that want the whole pipeline; the default composition keeps the hot path short. Unhandled exceptions in the fallback are caught locally there (typed `GatewayException` -> its `StatusCode`, everything else -> 500).

## Component notes

- **RoutingService / GatewayRouteRepository** - routes are stored in a plain `Dictionary<string, GatewayRoute>` guarded by a `ReaderWriterLockSlim`. Read-heavy workload, rare writes (management API), so the RW lock is the right tool; a `ConcurrentDictionary` would also work but gives weaker snapshot semantics for "get all routes".
- **Rate limiting** - `RateLimitingService` depends only on `IRateLimitStoreFactory`. The factory hands back `InMemoryRateLimitStore` or a per-connection-string `RedisRateLimitStore` depending on the route's `RateLimitPolicy`, so single-node and multi-node deployments use the same service code. Redis stores are cached in a `ConcurrentDictionary` keyed by connection string.
- **Circuit breaking** - `CircuitBreakerService` over `CircuitBreakerRepository`; classic Closed/Open/Half-Open state machine per upstream service name, exposed read-only at `/gateway/circuit-breakers`.
- **Resilience on the outbound call** - `RetryPolicy` (max attempts, initial/max delay, backoff factor) lives inside `ExternalApiClient`, so retries happen below the circuit breaker's failure accounting.
- **Metrics** - `MetricsService` is a singleton aggregating totals, per-route stats and status-code distribution; surfaced at `/gateway/stats`. No external metrics dependency - export is the `MetricsExportWorker`'s job.
- **Management plane** - controllers under `/api/*` (route CRUD, rate limit inspection, webhooks, admin dashboard) share the same singletons as the data plane, so changes take effect immediately without a reload step.

## Key design decisions

1. **Route resolution in middleware, forwarding in MapFallback.** Resolution must run before rate limiting (limits are per-route policy), but forwarding wants minimal-API conveniences (DI-injected parameters, `Results.*`). Splitting them keeps each piece simple at the cost of the `HttpContext.Items` handshake described above.

2. **Policies live on the route, not in global config.** A `GatewayRoute` owns its `RateLimitPolicy`, `CircuitBreakerPolicy`, `TransformationRules`, etc. Trade-off: the route object is fat, but there is exactly one place to look to understand a route's behavior, and per-route overrides need no configuration-merging logic.

3. **In-memory state by default.** Routes, counters, breaker states and metrics are process-local singletons. This makes the gateway zero-dependency to run and trivially fast, and is the honest default for a single-node deployment. The cost: state dies with the process and does not coordinate across replicas. The rate limit store is the one place where this was abstracted (`IRateLimitStore` + Redis implementation) because incorrect limits across replicas are a correctness problem, whereas lost metrics are merely annoying.

4. **Typed exceptions map to HTTP.** `GatewayException` and subclasses (`RouteNotFoundException`, `RateLimitExceededException`, `AuthenticationException`, `CircuitBreakerException`) carry `StatusCode` and `ErrorCode`, so error handling is one catch block instead of per-error mapping tables.

5. **One project, folder-per-concern.** No `Domain`/`Infrastructure` project split. For a gateway of this size the assembly boundary would add ceremony without enforcing anything the folder layout doesn't already communicate. The seams that matter (`IRateLimitStore`, `IRateLimitStoreFactory`, `IResponseTransformer`, `IRepository<T>`) are interfaces, so swapping implementations doesn't require a project split later either.

## Extension points

- **`IRateLimitStore` / `IRateLimitStoreFactory`** - add a new backing store (e.g. a database) without touching `RateLimitingService`.
- **`IResponseTransformer`** - replace `ResponseTransformationService` wholesale via DI.
- **`IRepository<T>`** - `GatewayRouteRepository` implements it; a persistent route store is a drop-in.
- **`TransformationRule`** - declarative request/response header and body rules per route.
- **Middleware `Use*` extensions** - every middleware registers via its own extension method, so hosts can compose their own pipeline order instead of `UseAllGatewayMiddleware`.
- **`EventBus`** - in-process pub/sub for reacting to gateway events without coupling services together.

## Known limitations

- **No shared state across replicas** except rate limiting via Redis. Routes configured through the management API on one node do not appear on another.
- **Route configuration is not persisted.** A restart loses routes added at runtime; the repository is a dictionary.
- **Request bodies are buffered into strings** in the fallback path before forwarding. Fine for JSON APIs, wrong for large uploads or streaming - `MaxRequestBodySize` in options is the guard rail.
- **`HttpContext.Items` string keys** couple middleware and the fallback endpoint without compile-time checking.
- **JWT validation exists as a service** (`JwtValidationService`, `AuthenticationPolicy`) but is not currently enforced in the default `Program.cs` pipeline; hosts that need it must add the middleware chain or wire it explicitly.
- **Background workers** (`CacheCleanupWorker`, `HealthCheckWorker`, `MetricsExportWorker`) are not registered as hosted services in the default composition - opt-in by design, but easy to assume they run when they don't.

## Testing and benchmarks

`tests/dotnet-api-gateway.Tests` covers services, models and middleware integration (routing + rate limiting together). `benchmarks/dotnet-api-gateway.Benchmarks` holds BenchmarkDotNet suites for the hot utility paths (JSON handling and friends). `examples/` contains small self-contained programs demonstrating routing, caching, JWT, circuit breaking and metrics usage.
