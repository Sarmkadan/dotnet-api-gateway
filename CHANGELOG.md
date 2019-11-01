# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-09-06

### Added

- **Webhook Management**: Webhook registration, event delivery, and retry logic via `WebhookRegistry` and `WebhookManagementController`
- **Request Transformation**: Header manipulation, payload transformation, and compression policies
- **Performance Analytics**: Real-time bottleneck detection and latency percentile tracking via `PerformanceAnalyzer`
- **Background Workers**: `CacheCleanupWorker`, `HealthCheckWorker`, and `MetricsExportWorker` for housekeeping tasks
- **Event Bus**: Internal pub/sub event system for extensibility across services
- **Response Formatters**: JSON, XML, and CSV response formatting with content negotiation
- **API Documentation**: OpenAPI/Swagger integration for all management endpoints
- **Request Coalescing**: Deduplication of identical in-flight requests to the same backend target

### Changed

- Promoted from pre-release; all public APIs are now stable
- Configuration schema locked — breaking changes require a major version bump
- `GatewayMiddleware` pipeline order finalized: validation → authentication → rate limit → circuit breaker → routing

### Fixed

- Race condition in circuit breaker state transition under high concurrency
- Cache invalidation could leave stale entries for up to 10 seconds past TTL
- CORS preflight responses were missing `Access-Control-Max-Age` header

### Security

- Added HTTPS/TLS support with configurable certificate path
- Implemented per-route request size limits to mitigate large-payload attacks
- Enhanced JWT validation with configurable clock skew and audience enforcement

---

## [0.5.0] - 2025-07-19

### Added

- **Metrics & Analytics**: `MetricsService` collects request throughput, latency (p50/p95/p99), error rates, and cache hit ratios
- **Structured Logging**: `RequestLoggingMiddleware` emits structured log entries with correlation IDs for every proxied request
- **Request Aggregation**: `RequestAggregationService` fans out to multiple backend targets and merges responses into a single payload
- **Health Check Service**: `HealthCheckService` pings backend targets on a configurable interval and updates routing weights

### Changed

- `RateLimitingService` switched from fixed-window to sliding-window algorithm, reducing burst traffic spikes at window boundaries
- `CircuitBreakerService` now transitions through a half-open state before fully closing, reducing false recoveries

### Fixed

- `RoutingService` regex compilation was not cached, causing measurable overhead on repeated pattern matches
- Request body was fully buffered even when not needed for routing decisions
- Graceful shutdown did not drain in-flight requests before stopping the host

---

## [0.4.0] - 2025-06-07

### Added

- **Request Caching**: `CacheService` and `RequestCachingDecorator` provide in-memory response caching with per-route TTL configuration
- **Load Balancing**: Weighted round-robin target selection across multiple `RouteTarget` entries per route
- **Retry Policies**: `RetryPolicy` with exponential backoff and per-attempt jitter for transient backend errors
- **External HTTP Client**: `ExternalApiClient` wraps `HttpClientFactory` with timeout, retry, and cancellation propagation

### Changed

- `GatewayRoute` model extended with `CachingPolicy` and `RetryPolicy` fields
- `ConfigurationValidator` now validates target URL format and rejects duplicate route names at startup

### Fixed

- Header forwarding dropped `X-Forwarded-For` when the gateway was behind a reverse proxy
- Route with an `ANY` method did not match `PATCH` requests

---

## [0.3.0] - 2025-04-26

### Added

- **JWT Validation**: `JwtValidationService` verifies HS256/RS256 tokens, extracts claims, and enforces role-based route access via `requiredRoles`
- **API Key Authentication**: Alternative authentication mode using `X-API-Key` header, configurable per-route
- **CORS Support**: Configurable CORS policy with per-origin and per-method allowlists
- **Circuit Breaker**: `CircuitBreakerService` and `CircuitBreakerRepository` implement the fail-fast pattern with configurable failure threshold and recovery timeout

### Changed

- `AuthenticationPolicy` model unified JWT and API-key configuration under a single policy object
- Middleware pipeline split into distinct `RequestValidationMiddleware` and `GatewayMiddleware` stages

### Fixed

- JWT token expiry was validated against server time without clock-skew tolerance, rejecting tokens from slightly out-of-sync clients
- `ErrorHandlingMiddleware` swallowed the original exception message in production mode, making debugging harder

---

## [0.2.0] - 2025-03-15

### Added

- **Rate Limiting**: `RateLimitingService` and `RateLimitRepository` with token-bucket algorithm; limits configurable per route and per client identity
- **Client Identity Resolution**: `ClientIdentity` model extracts client ID from JWT `sub`, API key, or remote IP address
- **Management API**: `GatewayManagementController` exposes REST endpoints to list routes, inspect rate-limit state, and query circuit-breaker status
- **Health Endpoint**: `/health` returns gateway uptime and per-backend reachability
- **Repositories**: `GatewayRouteRepository` and `RateLimitRepository` provide an in-memory persistence layer with `IRepository<T>` abstraction

### Changed

- Route configuration moved from hard-coded startup code to `appsettings.json` under `GatewayConfiguration.Routes`
- `GatewayConstants` and `GatewayEnums` extracted to a dedicated `Constants/` namespace

### Fixed

- Concurrent requests to the same rate-limit bucket could exceed the configured limit due to a missing lock
- Routes were matched case-sensitively; patterns now use `RegexOptions.IgnoreCase`

---

## [0.1.0] - 2025-02-01

### Added

- **Core Gateway**: HTTP request forwarding via `GatewayMiddleware` with pattern-based route matching
- **Route Model**: `GatewayRoute` with pattern, method filter, and one or more `RouteTarget` backend URLs
- **Configuration**: `GatewayConfiguration` loaded from `appsettings.json` with environment variable override support
- **Middleware Pipeline**: `ErrorHandlingMiddleware` wraps the pipeline and returns structured JSON error responses
- **Dependency Injection**: `ServiceCollectionExtensions.AddApiGateway()` registers all core services in a single call
- **Exceptions**: `GatewayException`, `RouteNotFoundException`, `RateLimitExceededException`, `CircuitBreakerException`, `AuthenticationException`
- **Utilities**: `UrlUtility`, `HeaderUtility`, `ValidationUtility`, `JsonUtility`, `DateTimeUtility`, `CryptoUtility`
- **Docker support**: `Dockerfile` and `docker-compose.yml` for containerised deployments
- **Initial test suite**: xUnit tests for route matching, URL normalisation, and utility helpers

### Security

- Input validation rejects route patterns containing null bytes or path traversal sequences
- Request body size capped at 10 MB by default

---

## Upgrade Guide

### Upgrading from 0.4.x to 0.5.0

Rate limiting now uses a sliding window. Clients that relied on bursting an entire fixed-window quota at the start of each interval will see requests rejected sooner. Review per-route `requestsPerMinute` values and increase `burstSize` if needed.

### Upgrading from 0.5.x to 1.0.0

1. Webhook registration is now persisted in `WebhookRegistry`. Re-register any webhooks previously wired up in code.

2. The circuit-breaker half-open state is enabled by default. Set `successThreshold: 1` in routes that should recover on the first successful probe.

3. Background workers (`CacheCleanupWorker`, `HealthCheckWorker`, `MetricsExportWorker`) are registered automatically via `AddApiGateway()`. Remove any manual `IHostedService` registrations to avoid duplicates.

---

## License

MIT License - Copyright (c) 2025 Vladyslav Zaiets

See LICENSE file for full details.
