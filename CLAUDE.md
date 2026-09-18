# CLAUDE.md

ASP.NET Core API gateway (.NET 10, C# latest): routing, rate limiting (in-memory/Redis), circuit breaker, JWT validation, request/response transformation, caching, webhooks, admin dashboard.

## Build

- `dotnet restore` / `dotnet build` (or `make build`)
- `dotnet run` (or `make run`); `make watch` for hot reload
- `dotnet publish -c Release` (or `make publish`)
- Docker: `make docker-build`, `make docker-compose-up` (see `Dockerfile`, `docker-compose.yml`)
- SDK pinned in `global.json` (10.0.100, rollForward latestMinor)

## Test

- `dotnet test` (or `make test`) - runs `tests/dotnet-api-gateway.Tests`
- Single test: `dotnet test --filter "FullyQualifiedName~RoutingServiceTests"`
- Coverage: `make coverage` (coverlet, opencover)
- Stack: xUnit 2.9, FluentAssertions 8, Moq 4.20
- Benchmarks: `benchmarks/dotnet-api-gateway.Benchmarks`
- `validate_header_sanitization.sh` - manual smoke check for header sanitization

## Lint / Format

- `dotnet format` (or `make format`)
- `make lint` = `dotnet build /p:EnforceCodeStyleInBuild=true`
- Rules in `.editorconfig`; `Nullable` and `ImplicitUsings` enabled via `Directory.Build.props`; warnings are not errors
- CI: `make ci-build` (format + lint + test); workflows in `.github/workflows/`

## Layout

- `Program.cs` - entry point, DI wiring, middleware pipeline
- `Configuration/` - `DotnetApiGatewayOptions`, `ServiceCollectionExtensions.AddGatewayServices`, `ConfigurationValidator`
- `Middleware/` - pipeline stages (Gateway, Routing, RateLimiting, ErrorHandling, ExceptionMapping, RequestLogging, PerformanceMonitoring, RequestValidation)
- `Services/` - business logic (RoutingService, RateLimitingService, CircuitBreakerService, JwtValidationService, CacheService, RequestCoalescingService, etc.)
- `Repositories/` - route/circuit-breaker storage, `IRateLimitStore` + InMemory/Redis implementations, `RateLimitStoreFactory`
- `Controllers/` - admin/management API (GatewayManagement, CircuitBreaker, AdminDashboard, RequestTransformation, WebhookManagement)
- `Models/`, `Constants/`, `Exceptions/`, `Events/` (EventBus), `Formatters/` (JSON/XML/CSV), `Utilities/`, `BackgroundServices/`
- `docs/` - per-component docs, `docs/ARCHITECTURE.md`
- `appsettings.json` / `appsettings.example.json` - gateway config

## Conventions

- Namespace `DotNetApiGateway.<Folder>`; `GlobalUsings.cs` holds shared usings
- File header comment with author block at top of source files
- Services registered in `Configuration/ServiceCollectionExtensions.cs`, not ad hoc in `Program.cs`
- Custom exceptions derive from `GatewayException`; mapped to HTTP responses in `ExceptionMappingMiddleware`
- Helpers split into partial-style companion files: `*Extensions.cs`, `*JsonExtensions.cs` next to the main class (same pattern in tests: `*TestsExtensions.cs`, `*TestsValidation.cs`)
- Tests: one `<Class>Tests.cs` per class, integration tests in `tests/.../Integration`
- Do not commit `bin/`, `obj/`, `*.backup`, `.aider*`
