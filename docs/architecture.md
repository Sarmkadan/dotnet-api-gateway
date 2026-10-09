# Project structure

This page maps the repository layout: what each folder holds and where to look for a given concern. For the runtime request pipeline and design trade-offs, see [ARCHITECTURE.md](ARCHITECTURE.md). If this page and the code disagree, the code wins.

## Solution layout

```
dotnet-api-gateway/
├── DotNetApiGateway.csproj        # Main project (Microsoft.NET.Sdk.Web, net10.0)
├── dotnet-api-gateway.sln         # Solution file
├── dotnet-api-gateway.slnx        # Solution file (XML format)
├── Program.cs                     # Composition root and endpoints
├── GlobalUsings.cs                # Shared global usings
├── Directory.Build.props          # Shared MSBuild properties (LangVersion, Nullable, ImplicitUsings)
├── global.json                    # SDK pin (10.0.100, rollForward latestMinor)
├── appsettings.json               # Runtime gateway configuration
├── appsettings.example.json       # Configuration template
├── Dockerfile, docker-compose.yml # Container build and local stack
├── Makefile                       # Build, test, lint, publish targets
├── BackgroundServices/            # Hosted-service style workers
├── Configuration/                 # Options, validation and DI registration
├── Constants/                     # Enums and constant values
├── Controllers/                   # Management HTTP API
├── Events/                        # In-process event bus
├── Exceptions/                    # Typed gateway exceptions
├── Extensions/                    # HttpContext extension helpers
├── Formatters/                    # JSON, XML and CSV response formatters
├── Middleware/                    # ASP.NET Core middleware components
├── Models/                        # Route model and policy objects
├── Repositories/                  # Storage abstractions and implementations
├── Services/                      # Domain logic
├── Utilities/                     # Stateless helper classes
├── docs/                          # Component documentation
├── examples/                      # Standalone usage samples (excluded from the main build)
├── tests/                         # xUnit test project
└── benchmarks/                    # BenchmarkDotNet project
```

The main project compiles everything under the root except `tests/**`, `benchmarks/**` and `examples/**`, which are removed from its `Compile` items in the csproj. Those folders are separate projects or sample code and do not ship in the gateway assembly.

## Folder reference

| Folder | Responsibility | Key types |
|---|---|---|
| `Program.cs` | Builds the host, registers services, wires middleware, maps explicit endpoints and the fallback proxy endpoint | `MapFallback` forwarding logic, `/health`, `/gateway/info`, `/gateway/routes`, `/gateway/stats` |
| `Configuration/` | Binds and validates settings, registers services | `DotnetApiGatewayOptions`, `WebhookSecurityOptions`, `ConfigurationValidator`, `ServiceCollectionExtensions` (`AddGatewayServices`) |
| `Middleware/` | Request pipeline components, each with a `Use*` extension | `RoutingMiddleware`, `RateLimitingMiddleware`, `ExceptionMappingMiddleware`, `ErrorHandlingMiddleware`, `RequestValidationMiddleware`, `RequestLoggingMiddleware`, `PerformanceMonitoringMiddleware`, `GatewayMiddleware` |
| `Services/` | Business logic: routing, rate limiting, circuit breaking, JWT, caching, transformation, aggregation, coalescing, metrics, health, versioning, analytics | `RoutingService`, `RateLimitingService`, `CircuitBreakerService`, `JwtValidationService`, `CacheService`, `RequestAggregationService`, `RequestCoalescingService`, `MetricsService`, `HealthCheckService`, `ApiVersioningService`, `AnalyticsService`, `GatewayManagementService` |
| `Services/` (decorators) | Wrap or extend other services | `RequestCachingDecorator`, `RequestInterceptor`, `RequestCachingDecoratorExtensions`, `RequestCoalescingExtensions` |
| `Services/` (transformation) | Request and response transformation | `RequestTransformationService`, `ResponseTransformationService`, `IResponseTransformer` |
| `Models/` | Route model and the policy objects attached to it | `GatewayRoute`, `RouteTarget`, `RateLimitPolicy`, `CircuitBreakerPolicy`, `AuthenticationPolicy`, `CachePolicy`, `AggregationPolicy`, `RequestCoalescingPolicy`, `ApiVersioningPolicy`, `TransformationRule`, `CompiledRouteTable`, `RequestContext`, `ClientIdentity`, `AggregatedRequest`, `AggregatedResponse`, `CircuitBreakerStatus` |
| `Models/` (companions) | Validation and helper extensions kept next to their model | `*Extensions.cs`, `*Validation.cs` (for example `CircuitBreakerPolicyValidation`, `AggregationPolicyValidation`, `WebhookSubscriptionValidation`) |
| `Repositories/` | Storage for routes and circuit breaker state, plus the rate limit store abstraction | `GatewayRouteRepository`, `CircuitBreakerRepository`, `IRepository<T>`, `IRateLimitStore`, `InMemoryRateLimitStore`, `RedisRateLimitStore`, `IRateLimitStoreFactory`, `RateLimitStoreFactory`, `RouteConfigurationChangeTokenSource` |
| `Controllers/` | Management API (route CRUD, circuit breakers, transformations, webhooks, admin dashboard) | `GatewayManagementController`, `CircuitBreakerController`, `RequestTransformationController`, `WebhookManagementController`, `AdminDashboardController` |
| `BackgroundServices/` | Periodic work: cache cleanup, health checks, metrics export | `CacheCleanupWorker`, `HealthCheckWorker`, `MetricsExportWorker` |
| `Events/` | In-process publish and subscribe | `EventBus`, `EventBusExtensions` |
| `Exceptions/` | Typed errors carrying status and error codes for HTTP mapping | `GatewayException` (base), `RouteNotFoundException`, `RateLimitExceededException`, `AuthenticationException`, `CircuitBreakerException`, `GatewayExceptionExtensions` |
| `Formatters/` | Serialization of responses in other formats | `JsonResponseFormatter`, `XmlFormatter`, `CsvFormatter` |
| `Utilities/` | Stateless helpers: JSON, URL, header, date, crypto, validation, conversion, performance analysis | `JsonUtility`, `UrlUtility`, `HeaderUtility`, `HeaderSanitizationUtility`, `CryptoUtility`, `DateTimeUtility`, `ValidationUtility`, `ConversionUtility`, `RateLimitMetrics`, `PerformanceAnalyzer` |
| `Constants/` | Shared constants and enums | `GatewayConstants`, `GatewayEnums` |
| `Extensions/` | Extension methods on ASP.NET Core types | `HttpContextGatewayExtensions` |
| `examples/` | Runnable usage samples, not part of the main build | `BasicRoutingExample`, `RateLimitingExample`, `CachingExample`, `JwtAuthenticationExample`, `CircuitBreakerExample`, `MetricsMonitoringExample` |
| `tests/dotnet-api-gateway.Tests` | xUnit tests; one `<Class>Tests.cs` per class | Run with `dotnet test` |
| `benchmarks/dotnet-api-gateway.Benchmarks` | BenchmarkDotNet suites for hot paths | Run with the benchmarks project |
| `docs/` | Per-component documentation | `ARCHITECTURE.md`, `configuration-reference.md`, `api-reference.md`, `deployment.md` and one page per class |

## Naming and file conventions

- Namespace follows folder: `DotNetApiGateway.<Folder>`.
- Companion files use the `*Extensions.cs` and `*Validation.cs` suffixes and sit beside the main type. Tests follow the same pattern (`*TestsExtensions.cs`, `*TestsValidation.cs`).
- Services are registered in `Configuration/ServiceCollectionExtensions.cs`, not ad hoc in `Program.cs`.
- Custom exceptions derive from `GatewayException` and are mapped to HTTP responses by `ExceptionMappingMiddleware`.
- Do not commit `bin/`, `obj/`, `*.backup` or `.aider*` files.

## Where to start for common changes

| Change | Start in |
|---|---|
| Add a route setting | `Models/GatewayRoute.cs` or a policy class in `Models/`, then validation in `Models/*Validation.cs` |
| Change how a request is matched | `Services/RoutingService.cs`, `Middleware/RoutingMiddleware.cs` |
| Change rate limiting | `Services/RateLimitingService.cs`, `Repositories/IRateLimitStore.cs` and its implementations |
| Add a backing store for rate limits | Implement `IRateLimitStore`, register it through `IRateLimitStoreFactory` |
| Add a new config option | `Configuration/DotnetApiGatewayOptions.cs`, then `Configuration/ConfigurationValidator.cs` |
| Add a management endpoint | `Controllers/` (new controller) and, if needed, `Services/GatewayManagementService.cs` |
| Map a new error to HTTP | New type in `Exceptions/` deriving from `GatewayException`, then `Middleware/ExceptionMappingMiddleware.cs` |
| Add a response format | New class in `Formatters/` |

## Related documents

- [ARCHITECTURE.md](ARCHITECTURE.md) - request pipeline, design decisions, known limitations
- [configuration-reference.md](configuration-reference.md) - configuration keys
- [getting-started.md](getting-started.md) - first run
- [CONTRIBUTING.md](../CONTRIBUTING.md) - contribution workflow
