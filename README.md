# DotNet API Gateway

![CI](https://github.com/sarmkadan/dotnet-api-gateway/actions/workflows/ci.yml/badge.svg)
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

## Features

- **Smart Routing**: Dynamic route matching, regex patterns, method-based routing  
- **Rate Limiting**: Token bucket algorithm, per-client/endpoint limits, sliding window  
- **JWT Validation**: Token verification, claim extraction, role-based access control  
- **Request Caching**: Response caching with TTL, conditional caching, cache invalidation  
- **Circuit Breaker**: Fail-fast pattern, automatic recovery, configurable thresholds  
- **Request Aggregation**: Combine multiple backend calls into single response  
- **Retry Policies**: Exponential backoff, jitter, transient error handling  
- **Request Transformation**: Header manipulation, payload transformation, compression, and **Lua scripting support**  
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

```bash
# Build Docker image
docker build -t dotnet-api-gateway:latest .

# Run with Docker Compose
docker-compose up -d
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
  "GatewayConfiguration": {
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

## Configuration Reference

For a comprehensive guide to all configuration options, including gateway-level settings and detailed policy configurations for routes (Authentication, Caching, Circuit Breaker, Rate Limiting, Request Coalescing), please see the [Configuration Reference](docs/configuration-reference.md) documentation.

## Usage Examples

### Example 1: Simple Routing

```csharp
// Route all /api/users requests to backend service
var gatewayConfig = new GatewayConfiguration
{
    Port = 5000,
    Routes = new List<GatewayRoute>
    {
        new GatewayRoute
        {
            Name = "user-service",
            Pattern = "^/api/users(/.*)?$",
            Method = "ANY",
            Targets = new List<RouteTarget>
            {
                new RouteTarget
                {
                    Url = "http://localhost:3001/api/users",
                    Weight = 100
                }
            }
        }
    }
};
```

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
