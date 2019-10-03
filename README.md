# DotNet API Gateway

A lightweight, production-ready API gateway for .NET built with modern C# and ASP.NET Core 10.

## Features

- **Intelligent Routing**: Pattern-based request routing with wildcard and parameter support
- **Rate Limiting**: Token bucket, sliding window, and fixed window algorithms
- **JWT Validation**: Secure token validation with configurable policies
- **Request Aggregation**: Execute multiple backend requests sequentially, in parallel, or until first success
- **Circuit Breaker**: Fault tolerance with configurable failure thresholds and recovery mechanisms
- **Response Caching**: Smart caching strategies with cache key generation
- **Load Balancing**: Round-robin, least connections, and IP-hash load balancing
- **Health Checks**: Built-in health monitoring for backend services
- **Request Logging**: Complete request/response tracking with timestamps
- **Error Handling**: Comprehensive exception handling with custom error codes

## Architecture

### Core Components

- **Models**: Domain entities representing routes, policies, and requests
- **Services**: Business logic for routing, rate limiting, JWT validation, circuit breaking
- **Repositories**: In-memory data access layer with thread-safe operations
- **Middleware**: Request/response processing pipeline
- **Configuration**: Dependency injection and configuration management

## Building & Running

### Prerequisites

- .NET 10 SDK
- Visual Studio / VS Code (optional)

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

The gateway will start on `http://localhost:5000` (HTTP) and `http://localhost:5001` (HTTPS).

## API Endpoints

### Health & Info

- `GET /health` - Gateway health status
- `GET /gateway/info` - Gateway information and available endpoints
- `GET /gateway/routes` - List all active routes
- `GET /gateway/circuit-breakers` - Show circuit breaker statuses
- `POST /gateway/rate-limit-info` - Get rate limit info for a client

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "GatewayConfiguration": {
    "ApplicationName": "DotNetApiGateway",
    "MaxRequestBodySize": 10485760,
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentRequests": 100,
    "EnableLogging": true,
    "EnableMetrics": true
  }
}
```

## Usage Example

### Create a Route

```csharp
var route = new GatewayRoute
{
    Name = "user-service",
    PathPattern = "/users/{id}",
    AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE" },
    Targets = new[]
    {
        new RouteTarget
        {
            Name = "user-api-1",
            BaseUrl = "https://api1.example.com",
            Weight = 1,
            IsHealthy = true
        }
    },
    RateLimitPolicy = new RateLimitPolicy
    {
        RequestsPerMinute = 1000,
        RequestsPerHour = 50000,
        Strategy = RateLimitStrategy.TokenBucket
    },
    CircuitBreakerPolicy = new CircuitBreakerPolicy
    {
        FailureThreshold = 5,
        TimeoutSeconds = 60
    }
};

await routingService.CreateRouteAsync(route);
```

### Validate JWT

```csharp
var policy = new AuthenticationPolicy
{
    Enabled = true,
    Type = AuthenticationType.Bearer,
    JwtSecret = "your-secret-key",
    ValidateExpiration = true
};

var clientIdentity = await jwtService.ValidateTokenAsync(token, policy);
```

### Check Rate Limits

```csharp
var allowed = await rateLimitService.IsAllowedAsync(
    clientId: "client-123",
    routeId: "user-service",
    policy: route.RateLimitPolicy
);

if (!allowed)
    // Return 429 Too Many Requests
```

## Project Structure

```
DotNetApiGateway/
├── Models/               # Domain entities
├── Services/             # Business logic services
├── Repositories/         # Data access layer
├── Configuration/        # DI and configuration
├── Constants/           # Enums and constants
├── Exceptions/          # Custom exception types
├── Program.cs           # Entry point
├── GlobalUsings.cs      # Global using directives
└── appsettings.json     # Configuration file
```

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- Title: CTO & Software Architect

---

Built with ❤️ for the .NET community
