# Route Configuration Guide

This guide covers the route configuration model in detail, including path matching, policy attachment, and advanced routing scenarios.

## Route Structure

Every `GatewayRoute` consists of:

| Property | Required | Description |
|----------|----------|-------------|
| `Name` | Yes | Human-readable identifier |
| `PathPattern` | Yes | URL pattern to match (supports wildcards) |
| `AllowedMethods` | Yes | HTTP methods (GET, POST, etc.) |
| `Targets` | Yes | Downstream service endpoints |
| `RateLimitPolicy` | No | Request rate limiting |
| `CircuitBreakerPolicy` | No | Failure protection |
| `CachePolicy` | No | Response caching |
| `AuthenticationPolicy` | No | JWT/API key validation |
| `TimeoutSeconds` | No | Downstream timeout (default: 30s) |
| `CustomHeaders` | No | Headers injected into proxied requests |

## Path Pattern Matching

Path patterns support three types of segments:

### Literal Segments
Matched case-insensitively against the request path.

```
/api/users/profile  ->  matches /api/users/profile
                        matches /API/Users/Profile
                        does NOT match /api/users/settings
```

### Wildcards (`*`)
Match any single path segment.

```
/api/*/profile      ->  matches /api/users/profile
                        matches /api/admins/profile
                        does NOT match /api/users/v2/profile
```

### Named Parameters (`{param}`)
Match any single segment and capture the value.

```
/api/users/{userId} ->  matches /api/users/42
                        matches /api/users/abc-123
                        does NOT match /api/users/42/orders
```

### Combining Patterns

```
/api/{version}/orders/{orderId}/*
```

This pattern matches paths like `/api/v2/orders/ORD-123/items`.

## Policy Configuration

### Rate Limiting

Control request throughput per client or globally:

```csharp
route.RateLimitPolicy = new RateLimitPolicy
{
    RequestsPerMinute = 100,
    RequestsPerHour = 5000,
    BurstSize = 20,
    KeyType = RateLimitKeyType.ClientIP  // or ApiKey, UserId
};
```

### Circuit Breaker

Protect against cascading failures from unhealthy downstream services:

```csharp
route.CircuitBreakerPolicy = new CircuitBreakerPolicy
{
    Enabled = true,
    FailureThreshold = 5,       // failures before opening
    TimeoutSeconds = 30,        // time in open state before half-open
    SuccessThreshold = 3        // successes in half-open to close
};
```

**State transitions:**
```
Closed --(failures >= threshold)--> Open --(timeout elapsed)--> HalfOpen
HalfOpen --(success >= threshold)--> Closed
HalfOpen --(any failure)--> Open
```

### Response Caching

Cache responses to reduce downstream load:

```csharp
route.CachePolicy = new CachePolicy
{
    Enabled = true,
    DurationSeconds = 300,
    VaryByHeaders = ["Accept", "Accept-Language"],
    VaryByQueryParameters = ["page", "pageSize"]
};
```

### Authentication

Require valid credentials for route access:

```csharp
route.AuthenticationPolicy = new AuthenticationPolicy
{
    RequireAuthentication = true,
    AuthType = AuthenticationType.JWT,
    AllowedScopes = ["read:users", "write:users"]
};
```

## Load Balancing

When multiple `RouteTarget` entries are configured, the gateway distributes requests using the configured strategy:

```csharp
route.Targets = [
    new RouteTarget { BaseUrl = "http://service-a:8080", Weight = 70 },
    new RouteTarget { BaseUrl = "http://service-b:8080", Weight = 30 }
];
```

## Custom Headers

Inject headers into proxied requests (useful for service mesh tracing):

```csharp
route.CustomHeaders = new Dictionary<string, string>
{
    ["X-Gateway-Route"] = "user-service",
    ["X-Request-Source"] = "api-gateway"
};
```
