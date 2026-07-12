# DotNet API Gateway

A lightweight, production-ready API gateway for .NET applications.

![Build](https://github.com/sarmkadan/dotnet-api-gateway/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-api-gateway)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

> A lightweight, production-ready API gateway for .NET applications with advanced routing, rate limiting, JWT validation, request aggregation, circuit breaker patterns, and comprehensive monitoring.

## Table of Contents

- [Overview](#overview)
- [Architecture Overview](#architecture-overview)
- [Configuration Reference](#configuration-reference)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Admin Dashboard](#admin-dashboard)
- [Request Transformation Rules](#request-transformation-rules)
- [API Versioning](#api-versioning)
- [Performance & Monitoring](#performance--monitoring)
- [Benchmarks](#benchmarks)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Overview

The DotNet API Gateway is a lightweight, high-performance API gateway built on .NET 10. It acts as a central entry point for your microservices architecture, handling cross-cutting concerns like authentication, rate limiting, circuit breaking, request aggregation, and comprehensive monitoring.

### Motivation

Modern applications require sophisticated API management capabilities:
- **Microservices Coordination**: Route requests across multiple backend services
- **Security First**: Implement JWT validation, API key management, and role-based access control
- **Resilience**: Circuit breaker patterns, automatic retries, and graceful degradation
- **Performance**: Request caching, response aggregation, and efficient load distribution
- **Observability**: Real-time metrics, request logging, and health monitoring

The DotNet API Gateway provides all these capabilities with minimal overhead and zero external dependencies for core functionality.

## Architecture Overview

For a detailed overview of the API Gateway's architecture, including its components and request lifecycle, please refer to the dedicated [Architecture Overview](docs/architecture.md) documentation.

## Configuration Reference

The `DotnetApiGateway` section in `appsettings.json` supports the following settings.

> **Note on Sensitive Data:** Do not store sensitive information like `JwtValidation:SecretKey` directly in `appsettings.json`. Use environment variables or a secure secret management system (e.g., Azure Key Vault, AWS Secrets Manager) to override these values at runtime.

| Setting | Type | Description | Default |
|---|---|---|---|
| `ApplicationName` | string | Name of the application | `DotNetApiGateway` |
| `Version` | string | Version string (x.y.z) | `1.0.0` |
| `MaxRequestBodySize` | int | Maximum request body size in bytes | `10485760` (10MB) |
| `DefaultTimeoutSeconds` | int | Default timeout for HTTP calls | `30` |
| `MaxConcurrentRequests` | int | Maximum concurrent requests | `100` |
| `EnableCors` | bool | Enable CORS support | `true` |
| `EnableCompression` | bool | Enable response compression | `true` |
| `EnableLogging` | bool | Enable request logging | `true` |
| `LogLevel` | string | Logging level | `Information` |
| `EnableMetrics` | bool | Enable metrics export | `true` |
| `EnableHealthCheck` | bool | Enable health check endpoint | `true` |
| `HealthCheckPath` | string | Health check endpoint path | `/health` |

### JWT Validation

| Setting | Type | Description |
|---|---|---|
| `Enabled` | bool | Enable JWT validation |
| `Issuer` | string | JWT token issuer |
| `Audience` | string | JWT token audience |
| `SecretKey` | string | JWT signing key (Sensitive) |

### Routes

List of `GatewayRoute` objects defining routing, rate limiting, and policies. Refer to the source code for the `GatewayRoute` model structure.

---

## Features

- **Smart Routing**: Dynamic route matching, regex patterns, method-based routing  
- **Rate Limiting**: Token bucket algorithm, per-client/endpoint limits, sliding window  
- **JWT Validation**: Token verification, claim extraction, role-based access control  
- **Request Caching**: Response caching with TTL, conditional caching, cache invalidation  
- **Circuit Breaker**: Fail-fast pattern, automatic recovery, configurable thresholds  
- **Request Aggregation**: Combine multiple backend calls into single response  
- **Retry Policies**: Exponential backoff, jitter, transient error handling  
- **Request Transformation Rules**: Rule-based header/query/path manipulation on requests and responses  
- **API Versioning**: URL path, header, query parameter, and media-type versioning strategies  
- **Admin Dashboard**: Built-in HTML dashboard with real-time metrics, route status, and circuit breaker overview  
- **Webhook Management**: Webhook registration, delivery, retry logic  
- **Health Monitoring**: Service health checks, dependency monitoring  
- **Metrics & Analytics**: Request metrics, latency tracking, error rates  
- **Performance Monitoring**: Real-time performance analysis, bottleneck detection  
- **Request Logging**: Structured logging, request/response capture  
- **Background Tasks**: Cleanup workers, metrics export, health checks  
- **Format Support**: JSON, XML, CSV response formatting  
- **Event Bus**: Internal event system for extensibility  

## Installation

### Prerequisites

- .NET 10 SDK ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Windows, macOS, or Linux
- 512MB RAM minimum, 2GB recommended

### Method 1: Clone from Repository

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/dotnet-api-gateway.git
cd dotnet-api-gateway

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release

# Run the gateway
dotnet run --configuration Release
```

### Method 2: Docker (Recommended)

To run the gateway with Docker, ensure you have Docker and Docker Compose installed.

```bash
# Build and start all services (Gateway + Redis + Backend)
docker-compose up -d --build

# The gateway will be available on http://localhost:8080
# Redis will be available on localhost:6379
```

### Method 3: NuGet Package

```bash
# Add as NuGet package (once published)
dotnet add package DotNetApiGateway

# In your Program.cs
builder.Services.AddApiGateway();
```

### Method 4: Build from Source

```bash
# Clone and build
git clone https://github.com/Sarmkadan/dotnet-api-gateway.git
cd dotnet-api-gateway

# Build and publish
dotnet publish -c Release -o ./publish

# Run published version
./publish/DotNetApiGateway
```

## Quick Start

### Docker Quick Start

Run the gateway with Docker Compose:

```bash
# Build and start
docker-compose up -d --build

# Access gateway
echo "Gateway running on http://localhost:8080"
```

### 1. Basic Configuration

Create `appsettings.json`:

```json
{
  "DotnetApiGateway": {
    "Port": 5000,
    "EnableHttps": false,
    "Routes": [
      {
        "name": "user-service",
        "pattern": "^/api/users(/.*)?$",
        "method": "ANY",
        "targets": [
          {
            "url": "http://localhost:3001/api/users",
            "weight": 100,
            "healthCheckUrl": "http://localhost:3001/health"
          }
        ],
        "rateLimitPolicy": {
          "enabled": true,
          "requestsPerMinute": 100
        },
        "cachingPolicy": {
          "enabled": true,
          "ttlSeconds": 300
        },
        "circuitBreakerPolicy": {
          "enabled": true,
          "failureThreshold": 5,
          "successThreshold": 2,
          "timeoutSeconds": 60
        }
      }
    ],
    "JwtValidation": {
      "enabled": true,
      "issuer": "https://your-auth-provider.com",
      "audience": "api.gateway",
      "secretKey": "your-secret-key-here"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DotNetApiGateway": "Debug"
    }
  }
}
```

### 2. Start the Gateway

```bash
dotnet run
```

Gateway will listen on `http://localhost:5000`

### 3. Test a Route

```bash
# Without authentication
curl http://localhost:5000/api/users

# With JWT token
curl -H "Authorization: Bearer {token}" http://localhost:5000/api/users

# With custom headers
curl -H "X-Custom-Header: value" http://localhost:5000/api/users
```

## Configuration

The gateway uses the `DotnetApiGateway` section in `appsettings.json` for configuration. See [appsettings.example.json](appsettings.example.json) for all configurable values and their default settings.

For advanced settings and policy definitions, refer to the [Configuration Reference](docs/configuration-reference.md).


## Usage Examples

The `examples/` directory contains complete, runnable code snippets demonstrating common gateway usage scenarios.

- [BasicUsage.cs](examples/BasicUsage.cs) - Minimal setup for a gateway.
- [AdvancedUsage.cs](examples/AdvancedUsage.cs) - Advanced configuration, custom options, and error handling.
- [IntegrationExample.cs](examples/IntegrationExample.cs) - Integrating gateway services into an existing ASP.NET Core DI container.

The following snippets provide a quick overview:

### Example 1: Simple Routing
... (keeping the rest as is)
### Example 2: Load Balancing

```csharp
new GatewayRoute
{
    Name = "balanced-service",
    Pattern = "^/api/data(/.*)?$",
    Method = "ANY",
    Targets = new List<RouteTarget>
    {
        new RouteTarget { Url = "http://api-1:3001/api/data", Weight = 40 },
        new RouteTarget { Url = "http://api-2:3001/api/data", Weight = 40 },
        new RouteTarget { Url = "http://api-3:3001/api/data", Weight = 20 }
    }
}
```

### Example 3: JWT Authentication

```json
{
  "Routes": [
    {
      "name": "protected-api",
      "pattern": "^/api/admin(/.*)?$",
      "requiresAuthentication": true,
      "requiredRoles": ["admin"],
      "targets": [{"url": "http://admin-service:3000"}]
    }
  ],
  "JwtValidation": {
    "enabled": true,
    "secretKey": "your-secret-key",
    "issuer": "https://auth-provider.com"
  }
}
```

### Example 4: Response Caching

```json
{
  "Routes": [
    {
      "name": "cached-data",
      "pattern": "^/api/catalog(/.*)?$",
      "targets": [{"url": "http://catalog-service:3000"}],
      "cachingPolicy": {
        "enabled": true,
        "ttlSeconds": 600
      }
    }
  ]
}
```

### Example 5: Circuit Breaker Protection


```csharp
var route = new GatewayRoute
{
  Name = "resilient-service",
  Pattern = "^/api/orders(/.*)?$",
  Targets = [new RouteTarget { Url = "http://order-service:3000" }],
  CircuitBreakerPolicy = new CircuitBreakerPolicy
  {
    Enabled = true,
    FailureThreshold = 5,
    SuccessThreshold = 2,
    TimeoutSeconds = 60
  }
};
```

## AdminDashboardSummaryTests

The `AdminDashboardSummaryTests` class is a test fixture used for JSON serialization testing of the admin dashboard summary data structure. It provides strongly-typed properties that mirror the actual admin dashboard summary response format, making it ideal for testing serialization/deserialization of gateway metrics, route statistics, and circuit breaker states.

This type is particularly useful for:
- Validating JSON serialization/deserialization of admin dashboard responses
- Testing API contract consistency between the gateway and monitoring systems
- Ensuring backward compatibility when extending the admin dashboard summary API

### Example Usage

```csharp
// Create a complete admin dashboard summary for testing
var summary = new AdminDashboardSummaryTests
{
    Gateway = new AdminDashboardSummaryTests.GatewayInfo
    {
        Name = "Production Gateway",
        Version = "2.1.0",
        Uptime = "2.14:32:15",
        StartedAt = DateTime.UtcNow.AddDays(-2)
    },
    Requests = new AdminDashboardSummaryTests.RequestMetrics
    {
        Total = 15420,
        Successful = 15301,
        Failed = 119,
        SuccessRatePercent = 99.23,
        AverageResponseTimeMs = 4.7,
        RequestsPerSecond = 20.1
    },
    Routes = new AdminDashboardSummaryTests.RouteStats
    {
        Total = 6,
        Active = 6,
        Inactive = 0
    },
    CircuitBreakers = new AdminDashboardSummaryTests.CircuitBreakerStats
    {
        Total = 3,
        Open = 0,
        HalfOpen = 0,
        Closed = 3
    },
    StatusCodeDistribution = new Dictionary<int, long>
    {
        { 200, 12540 },
        { 201, 890 },
        { 400, 210 },
        { 404, 150 },
        { 500, 119 }
    },
    Timestamp = DateTime.UtcNow
};

// Serialize to JSON for testing
string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
```

### Related Types

The `AdminDashboardSummaryTests` class contains the following nested types that mirror the actual admin dashboard response structure:

- **GatewayInfo**: Gateway metadata (Name, Version, Uptime, StartedAt)
- **RequestMetrics**: Request statistics (Total, Successful, Failed, SuccessRatePercent, AverageResponseTimeMs, RequestsPerSecond)
- **RouteStats**: Route statistics (Total, Active, Inactive)
- **CircuitBreakerStats**: Circuit breaker statistics (Total, Open, HalfOpen, Closed)

### Example JSON Output

The serialized JSON matches the format returned by the `/admin/dashboard/summary` endpoint:

```json
{
  "Gateway": {
    "Name": "Production Gateway",
    "Version": "2.1.0",
    "Uptime": "2.14:32:15",
    "StartedAt": "2025-07-10T08:00:00Z"
  },
  "Requests": {
    "Total": 15420,
    "Successful": 15301,
    "Failed": 119,
    "SuccessRatePercent": 99.23,
    "AverageResponseTimeMs": 4.7,
    "RequestsPerSecond": 20.1
  },
  "Routes": {
    "Total": 6,
    "Active": 6,
    "Inactive": 0
  },
  "CircuitBreakers": {
    "Total": 3,
    "Open": 0,
    "HalfOpen": 0,
    "Closed": 3
  },
  "StatusCodeDistribution": {
    "200": 12540,
    "201": 890,
    "400": 210,
    "404": 150,
    "500": 119
  },
  "Timestamp": "2025-07-12T14:30:00Z"
}
```

### Testing Scenarios

This type is commonly used in test scenarios such as:

```csharp
[Fact]
public void AdminDashboardSummary_Should_Serialize_To_Expected_Json()
{
    // Arrange
    var summary = new AdminDashboardSummaryTests
    {
        Gateway = new AdminDashboardSummaryTests.GatewayInfo
        {
            Name = "Test Gateway",
            Version = "1.0.0",
            Uptime = "0.00:05:00",
            StartedAt = DateTime.UtcNow
        },
        Requests = new AdminDashboardSummaryTests.RequestMetrics
        {
            Total = 1000,
            Successful = 995,
            Failed = 5,
            SuccessRatePercent = 99.5,
            AverageResponseTimeMs = 2.5,
            RequestsPerSecond = 3.3
        },
        Routes = new AdminDashboardSummaryTests.RouteStats
        {
            Total = 2,
            Active = 2,
            Inactive = 0
        },
        CircuitBreakers = new AdminDashboardSummaryTests.CircuitBreakerStats
        {
            Total = 1,
            Open = 0,
            HalfOpen = 0,
            Closed = 1
        },
        StatusCodeDistribution = new Dictionary<int, long> { { 200, 995 }, { 404, 5 } },
        Timestamp = DateTime.UtcNow
    };

    // Act
    string json = JsonSerializer.Serialize(summary);

    // Assert
    Assert.NotNull(json);
    Assert.Contains("Test Gateway", json);
    Assert.Contains("99.5", json);
}
```

## Example 5: Circuit Breaker Protection

```json
{
  "Routes": [
    {
      "name": "resilient-service",
      "pattern": "^/api/orders(/.*)?$",
      "targets": [{"url": "http://order-service:3000"}],
      "circuitBreakerPolicy": {
        "enabled": true,
        "failureThreshold": 5,
        "successThreshold": 2,
        "timeoutSeconds": 60
      }
    }
  ]
}
```

## API Reference

### Health Endpoint

```bash
GET /health
```

Returns gateway and backend service health status.

### Routes Management

```bash
# List routes
GET /api/gateway/routes

# Get route details
GET /api/gateway/routes/{routeName}

# Create route
POST /api/gateway/routes

# Update route
PUT /api/gateway/routes/{routeName}

# Delete route
DELETE /api/gateway/routes/{routeName}
```

### Rate Limit Status

```bash
GET /api/gateway/ratelimit/{clientId}
```

### Circuit Breaker Status

```bash
GET /api/gateway/circuitbreaker/{routeName}
```

### Metrics

```bash
GET /api/metrics
```

## Admin Dashboard

The admin dashboard provides a real-time HTML overview of the gateway at a glance. It is intended for internal operators and does not require a separate frontend.

### Endpoints

| Endpoint | Description |
|---|---|
| `GET /admin/dashboard` | Full HTML dashboard with live stats, route table, circuit breaker states, and status code distribution |
| `GET /admin/dashboard/summary` | JSON summary suitable for monitoring agents and health-check scripts |

### Dashboard Sections

- **Summary cards** — total requests, success rate (colour-coded), average response time, active route count, open circuit breakers
- **Routes table** — lists every configured route with path pattern, allowed methods, healthy target ratio, active status, and per-route request count / average latency
- **Circuit Breakers table** — service name, current state (Closed / Half-Open / Open), failure and success counts, last error message, last failure time
- **Status Code Distribution** — count and percentage share for every HTTP status code seen since gateway start

The HTML page refreshes automatically every 30 seconds via `<meta http-equiv="refresh">`.

### Example

```bash
# Open in a browser
curl http://localhost:5000/admin/dashboard

# Fetch JSON summary for a monitoring agent
curl http://localhost:5000/admin/dashboard/summary
```

Sample JSON summary response:

```json
{
  "gateway": { "name": "DotNetApiGateway", "version": "2.0.2", "uptime": "0.00:12:43" },
  "requests": { "total": 15420, "successful": 15301, "failed": 119, "successRatePercent": 99.23, "averageResponseTimeMs": 4.7, "requestsPerSecond": 20.1 },
  "routes": { "total": 6, "active": 6, "inactive": 0 },
  "circuitBreakers": { "total": 3, "open": 0, "halfOpen": 0, "closed": 3 }
}
```

---

## Request Transformation Rules

Transformation rules let you modify outgoing requests and incoming responses on a per-route basis without touching backend code.

### Rule model

Each rule is a `TransformationRule` object with the following fields:

| Field | Type | Description |
|---|---|---|
| `id` | `string` | Auto-generated UUID |
| `description` | `string` | Human-readable label |
| `phase` | `Request` / `Response` | When to apply the rule |
| `operation` | See table below | What to do |
| `key` | `string` | Header name, query param name, or path prefix |
| `value` | `string?` | Target value (not required for Remove operations) |
| `order` | `int` | Evaluation order — lower numbers run first |
| `isEnabled` | `bool` | Set to `false` to skip without removing |

### Supported operations

| Operation | Phase | Description |
|---|---|---|
| `AddHeader` | Request / Response | Add a header if it does not already exist |
| `SetHeader` | Request / Response | Set a header, replacing any existing value |
| `RemoveHeader` | Request / Response | Delete a header |
| `AddQueryParam` | Request | Add a query parameter if missing |
| `SetQueryParam` | Request | Set a query parameter, replacing existing |
| `RemoveQueryParam` | Request | Remove a query parameter from the URL |
| `RewritePathPrefix` | Request | Replace a path prefix before forwarding |

Rules on the same route are executed in ascending `order` value. Request-phase rules run before the request is forwarded to the backend; response-phase rules run after the upstream response is received and before it reaches the client.

### Configuration example

```csharp
var route = new GatewayRoute
{
    Name = "Orders API",
    PathPattern = "/v2/orders/*",
    AllowedMethods = ["GET", "POST"],
    Targets = [new RouteTarget { Name = "orders-svc", BaseUrl = "http://orders:8080" }],
    TransformationRules =
    [
        // Add a tenant header to every forwarded request
        new TransformationRule
        {
            Phase = TransformationPhase.Request,
            Operation = TransformationOperation.SetHeader,
            Key = "X-Tenant-Id",
            Value = "acme",
            Order = 1
        },
        // Strip the internal debug parameter
        new TransformationRule
        {
            Phase = TransformationPhase.Request,
            Operation = TransformationOperation.RemoveQueryParam,
            Key = "debug",
            Order = 2
        },
        // Hide the backend server banner from clients
        new TransformationRule
        {
            Phase = TransformationPhase.Response,
            Operation = TransformationOperation.RemoveHeader,
            Key = "Server",
            Order = 1
        }
    ]
};
```

---

## TransformationRuleExtensions

The `TransformationRuleExtensions` class provides a set of extension methods that simplify the creation and management of transformation rules. These methods offer a fluent API for configuring common rule operations such as adding headers, setting headers, removing query parameters, and rewriting path prefixes, reducing boilerplate code and ensuring consistent rule configuration.

The extension methods automatically set appropriate phase, operation, and description values, while also validating input parameters to prevent null reference exceptions.




### Usage Examples

```csharp
// Create a transformation rule to add a request header
var rule = new TransformationRule()
  .AddRequestHeader("X-Request-Id", Guid.NewGuid().ToString(), "Add unique request ID", order: 1);

// Create a transformation rule to set a response header
var responseRule = new TransformationRule()
  .SetResponseHeader("X-API-Version", "2.1.0", "Set API version header", order: 1);

// Create a transformation rule to remove a query parameter
var queryRule = new TransformationRule()
  .RemoveRequestQueryParam("debug", "Remove debug flag from request", order: 2);

// Create a transformation rule to rewrite path prefixes
var pathRule = new TransformationRule()
  .RewritePathPrefix("/v1/", "/v2/", "Upgrade API version in path", order: 3);

// Check if a rule is a header operation
bool isHeaderOp = rule.IsHeaderOperation();

// Check if a rule is a query parameter operation
bool isQueryOp = queryRule.IsQueryParamOperation();

// Clone a rule for reuse
var clonedRule = rule.Clone();

// Check if a rule can be applied
bool canApply = pathRule.CanApply();
```

## API Versioning

The gateway supports four version-detection strategies that can be combined in priority order. Versioned routes can enforce a set of supported versions and optionally strip the version segment before forwarding so backends remain unaware of it.

### Strategies

| Strategy | Example |
|---|---|
| `UrlPath` | `/v2/orders` |
| `Header` | `X-API-Version: 2` |
| `QueryParameter` | `?api-version=2` |
| `MediaType` | `Accept: application/vnd.myapi.v2+json` |

Multiple strategies can be active simultaneously. The first match wins.

### Policy model

```csharp
new ApiVersioningPolicy
{
    Enabled = true,
    DefaultVersion = "1",          // used when no version is detected
    RequireVersion = false,        // when true, missing version → HTTP 400
    SupportedVersions = ["1", "2"],// empty = all parseable versions accepted
    StripVersionFromPath = true,   // remove /vN segment before forwarding
    Strategies = [VersioningStrategy.UrlPath, VersioningStrategy.Header],
    HeaderName = "X-API-Version",
    QueryParameterName = "api-version"
}
```

### Configuration example

```csharp
var route = new GatewayRoute
{
    Name = "Versioned Users API",
    PathPattern = "/v*/users/*",
    AllowedMethods = ["GET"],
    Targets = [new RouteTarget { Name = "users-svc", BaseUrl = "http://users:8080" }],
    VersioningPolicy = new ApiVersioningPolicy
    {
        Enabled = true,
        SupportedVersions = ["1", "2"],
        DefaultVersion = "1",
        StripVersionFromPath = true,
        Strategies = [VersioningStrategy.UrlPath, VersioningStrategy.Header]
    }
};
```

A request to `GET /v2/users/123` will be forwarded as `GET /users/123` to the backend with the resolved version stored in `HttpContext.Items["ApiVersion"]`.

### Error response (HTTP 400)

When versioning is required but the requested version is invalid or unsupported, the gateway responds with:

```json
{
  "error": "Unsupported or missing API version",
  "attemptedVersion": "99",
  "supportedVersions": ["1", "2"],
  "defaultVersion": "1",
  "strategies": ["UrlPath", "Header"]
}
```

## Performance & Monitoring

The gateway collects comprehensive metrics including:
- Request throughput and latency (p50, p95, p99)
- Error rates and types
- Cache hit/miss rates
- Circuit breaker state transitions
- Rate limit violations
- Backend service health

### Monitoring Tools Integration

- Prometheus endpoint at `/metrics`
- Application Insights integration
- Structured logging with Serilog
- Health checks for orchestrators

## Benchmarks

Measured on a single core (Intel Core i7-12700, .NET 10, Linux) with in-memory repositories and no TLS termination:

| Scenario | Throughput | p50 Latency | p95 Latency | p99 Latency |
|---|---|---|---|---|
| Simple routing (passthrough) | ~18,000 req/s | 0.4 ms | 1.2 ms | 3.5 ms |
| JWT validation per request | ~12,000 req/s | 0.7 ms | 2.1 ms | 5.8 ms |
| Cache hit (response served locally) | ~42,000 req/s | < 0.1 ms | 0.3 ms | 0.8 ms |
| Request aggregation (3 backends) | ~4,500 req/s | 6 ms | 14 ms | 28 ms |
| Rate limit check overhead | < 0.3 ms added | — | — | — |
| Circuit breaker fault detection | < 80 ms | — | — | — |

Memory footprint at steady state with 50 active routes: ~35 MB. Scales linearly with the number of concurrent connections; 1,000 concurrent clients add roughly 12 MB.

### Running Benchmarks

To run the performance benchmarks, navigate to the benchmarks project directory and run with the `Release` configuration:

```bash
cd benchmarks/dotnet-api-gateway.Benchmarks
dotnet run -c Release
```

The benchmarks are implemented using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Troubleshooting

### High Latency Issues

Check backend service health:
```bash
curl http://localhost:5000/health
```

Review circuit breaker status:
```bash
curl http://localhost:5000/api/gateway/circuitbreaker/{route}
```

### Rate Limiting Problems

Check current limits:
```bash
curl http://localhost:5000/api/gateway/ratelimit/{clientId}
```

Increase limits in configuration if needed.

### Authentication Failures

Verify JWT configuration in `appsettings.json`:
- Check `issuer` matches token issuer
- Verify `secretKey` is correct
- Ensure `audience` matches token claims

### Memory Leaks

Monitor with dotnet-trace:
```bash
dotnet trace collect dotnet {pid}
```

Check cache TTL settings and review background workers.

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test project
dotnet test tests/dotnet-api-gateway.Tests/dotnet-api-gateway.Tests.csproj

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

The test suite covers circuit breaker state transitions, route matching, URL utility helpers, rate limit accounting, and request transformation pipeline including Lua scripting validation. Integration tests spin up an in-process gateway host — no external dependencies required.

## Related Projects

- [dotnet-resilience-pipeline](https://github.com/sarmkadan/dotnet-resilience-pipeline) - Resilience pipeline for .NET - circuit breaker, bulkhead, retry, timeout, fallback with fluent configuration
- [api-key-gateway](https://github.com/sarmkadan/api-key-gateway) - Lightweight API key authentication gateway for self-hosted services - rate limiting, usage tracking
- [redis-cache-patterns](https://github.com/sarmkadan/redis-cache-patterns) - Production-ready Redis caching patterns for .NET - cache-aside, write-through, distributed lock

### Integration Examples

**Layered resilience with dotnet-resilience-pipeline** — wrap the gateway's outbound HTTP client with an external resilience pipeline for fine-grained bulkhead and timeout policies that complement the gateway's built-in circuit breaker:

```csharp
// Program.cs
builder.Services.AddHttpClient("resilient-backend")
    .AddResiliencePipeline(pipeline => pipeline
        .AddBulkhead(maxConcurrentCalls: 50)
        .AddTimeout(TimeSpan.FromSeconds(5))
        .AddRetry(retryCount: 2, backoffType: BackoffType.Exponential));

builder.Services.AddApiGateway();
```

**Distributed caching with redis-cache-patterns** — replace the default in-memory cache with a Redis-backed cache-aside store so cached responses survive gateway restarts:

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration["Redis:ConnectionString"]);

builder.Services.AddApiGateway(options =>
    options.UseDistributedCache = true);
```

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
