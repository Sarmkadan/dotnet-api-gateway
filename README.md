[![Build](https://github.com/sarmkadan/dotnet-api-gateway/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-api-gateway/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

# DotNet API Gateway

> A lightweight, production-ready API gateway for .NET applications with advanced routing, rate limiting, JWT validation, request aggregation, circuit breaker patterns, and comprehensive monitoring.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Performance & Monitoring](#performance--monitoring)
- [Troubleshooting](#troubleshooting)
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

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                       │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                ┌──────────────▼───────────────┐
                │    API Gateway Listener      │
                │  (HTTP/HTTPS on :5000)      │
                └──────────────┬───────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
   ┌────▼────┐        ┌────────▼────────┐      ┌────▼────┐
   │ Middleware Pipeline                │      │  Handlers│
   ├──────────┤        ├─────────────────┤      └──────────┘
   │ Auth     │        │ Request Context │
   │ Logging  │        │ Validation      │
   │ Perf Mon │        │ Transformation  │
   └──────────┘        └────────┬────────┘
                                 │
           ┌─────────────────────┼─────────────────────┐
           │                     │                     │
      ┌────▼─────┐        ┌──────▼──────┐       ┌────▼────┐
      │ Routing   │        │ Rate Limit  │       │ Circuit │
      │ Service   │        │ Service     │       │ Breaker │
      └────┬─────┘        └──────┬──────┘       └────┬────┘
           │                     │                     │
      ┌────┴─────────────────────┼─────────────────────┴────┐
      │                          │                          │
 ┌────▼──────┐  ┌──────────┐ ┌──▼────┐  ┌────────────┐ ┌─▼──────┐
 │ Cache      │  │ HTTP     │ │Health │  │Aggregation│ │Webhook │
 │ Service    │  │ Client   │ │Check  │  │Service    │ │Registry│
 └────┬──────┘  └──────┬───┘ │Service│  └────┬──────┘ └────────┘
      │               │     └──┬────┘        │
      │               │        │             │
      └───────┬───────┴────────┴─────────────┘
              │
      ┌───────▼──────────────────┐
      │  Backend Services        │
      │ (Microservices Cluster)  │
      │                          │
      │ ┌──────┐  ┌──────┐      │
      │ │ API1 │  │ API2 │ ...  │
      │ └──────┘  └──────┘      │
      └──────────────────────────┘
```

### Component Layers

**Presentation Layer**: Gateway listeners handling HTTP/HTTPS protocols
**Request Pipeline**: Middleware for logging, validation, and request context enrichment
**Business Logic**: Services for routing, rate limiting, caching, and aggregation
**Integration Layer**: HTTP client factories and webhook management
**Persistence**: In-memory repositories for routes, rate limits, and circuit breaker state

## Features

- **Smart Routing**: Dynamic route matching, regex patterns, method-based routing  
- **Rate Limiting**: Token bucket algorithm, per-client/endpoint limits, sliding window  
- **JWT Validation**: Token verification, claim extraction, role-based access control  
- **Request Caching**: Response caching with TTL, conditional caching, cache invalidation  
- **Circuit Breaker**: Fail-fast pattern, automatic recovery, configurable thresholds  
- **Request Aggregation**: Combine multiple backend calls into single response  
- **Retry Policies**: Exponential backoff, jitter, transient error handling  
- **Request Transformation**: Header manipulation, payload transformation, compression  
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

## Configuration

### Complete Configuration Options

```json
{
  "GatewayConfiguration": {
    "Port": 5000,
    "EnableHttps": false,
    "CertificatePath": "/path/to/cert.pfx",
    "CertificatePassword": "password",
    "RequestTimeoutSeconds": 30,
    "MaxRequestBodySizeBytes": 10485760,
    "EnableRequestLogging": true,
    "EnableMetrics": true,
    "EnableHealthChecks": true,
    "HealthCheckIntervalSeconds": 30,
    
    "Routes": [
      {
        "name": "route-name",
        "pattern": "^/api/endpoint(/.*)?$",
        "method": "GET|POST|PUT|DELETE|PATCH|ANY",
        "description": "Route description",
        "requiresAuthentication": true,
        "requiredRoles": ["admin", "user"],
        "requestTransformPolicy": {
          "addHeaders": {
            "X-Gateway-Version": "1.0"
          },
          "removeHeaders": ["Authorization"],
          "modifyHeaders": {
            "Content-Type": "application/json"
          }
        },
        "targets": [
          {
            "url": "http://backend-service:3000/endpoint",
            "weight": 50,
            "healthCheckUrl": "http://backend-service:3000/health",
            "timeout": 30
          }
        ],
        "rateLimitPolicy": {
          "enabled": true,
          "requestsPerMinute": 100,
          "requestsPerSecond": 10,
          "burst": true,
          "burstSize": 50
        },
        "cachingPolicy": {
          "enabled": true,
          "ttlSeconds": 300,
          "cacheKeyPattern": "{method}:{path}:{querystring}",
          "conditionalCache": true
        },
        "circuitBreakerPolicy": {
          "enabled": true,
          "failureThreshold": 5,
          "successThreshold": 2,
          "timeoutSeconds": 60
        },
        "retryPolicy": {
          "enabled": true,
          "maxRetries": 3,
          "backoffMultiplier": 2.0,
          "initialBackoffMs": 100
        }
      }
    ],
    
    "JwtValidation": {
      "enabled": true,
      "issuer": "https://auth-server.com",
      "audience": "api.gateway",
      "secretKey": "your-secret-key",
      "validateIssuer": true,
      "validateAudience": true,
      "validateLifetime": true,
      "clockSkewSeconds": 5
    },
    
    "ApiKeyAuthentication": {
      "enabled": true,
      "headerName": "X-API-Key",
      "keys": {
        "client-1": "key-1-secret",
        "client-2": "key-2-secret"
      }
    },
    
    "CorsPolicy": {
      "enabled": true,
      "allowedOrigins": ["*"],
      "allowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "allowedHeaders": ["*"],
      "allowCredentials": false,
      "maxAgeSeconds": 3600
    }
  },
  
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment-Specific Configuration

```bash
# Development
dotnet run --launch-profile Development

# Production
dotnet run --configuration Release

# Custom environment
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

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
