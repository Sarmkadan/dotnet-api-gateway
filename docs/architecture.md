# DotNet API Gateway - Architecture Guide

This document provides an in-depth look at the architecture, design patterns, and internal workings of the DotNet API Gateway.

## System Architecture

### High-Level Overview

```
┌──────────────────────────────────────────────────────────────┐
│  Client Requests                                             │
│  (HTTP/HTTPS via curl, browsers, mobile apps, etc.)         │
└─────────────────────────┬──────────────────────────────────┘
                          │
          ┌───────────────▼────────────────┐
          │  Gateway Entry Point           │
          │  (Kestrel HTTP Server)         │
          │  Port: 5000 (HTTP)             │
          │  Port: 5001 (HTTPS - optional) │
          └───────────────┬────────────────┘
                          │
      ┌───────────────────┼────────────────────┐
      │   Middleware Pipeline                  │
      │                                        │
      │  1. Request Logging Middleware         │
      │  2. Error Handling Middleware          │
      │  3. Authentication Middleware          │
      │  4. Request Validation Middleware      │
      │  5. Performance Monitoring Middleware  │
      └────────────────┬──────────────────────┘
                       │
         ┌─────────────▼────────────────┐
         │  Gateway Middleware          │
         │  (Core Request Handler)      │
         │                              │
         │  - Route Matching            │
         │  - Policy Application        │
         │  - Context Enrichment        │
         └─────────────┬────────────────┘
                       │
     ┌─────────────────┼──────────────────────┐
     │                 │                      │
┌────▼──────┐  ┌──────▼──────┐    ┌─────────▼─────┐
│ Routing   │  │ Rate Limit  │    │  Circuit      │
│ Service   │  │ Service     │    │  Breaker      │
└────┬──────┘  └──────┬──────┘    └────────┬──────┘
     │                │                   │
     └────────┬───────┴───────────────────┘
              │
      ┌───────▼──────────────┐
      │  Cache Service       │
      │  (In-memory)         │
      └───────┬──────────────┘
              │
      ┌───────▼──────────────┐
      │ HTTP Client Factory  │
      │ (Backend Request)    │
      └───────┬──────────────┘
              │
    ┌─────────▼─────────┐
    │ Backend Services  │
    │ (Load Balanced)   │
    └───────────────────┘
```

## Component Breakdown

### 1. Presentation Layer

**Kestrel Server** - High-performance HTTP server built into ASP.NET Core
- Listens on ports 5000 (HTTP) and 5001 (HTTPS)
- Handles raw HTTP protocol parsing
- Manages SSL/TLS when enabled

### 2. Middleware Pipeline

Middleware components process requests in order:

#### RequestLoggingMiddleware
- Captures incoming request metadata
- Logs method, path, headers, timestamp
- Useful for debugging and auditing

#### ErrorHandlingMiddleware
- Wraps entire request in try-catch
- Formats exceptions into HTTP responses
- Returns appropriate status codes (500, 404, etc.)

#### AuthenticationMiddleware
- Validates authentication (JWT, API Key)
- Extracts user identity
- Stores identity in request context for downstream use

#### RequestValidationMiddleware
- Validates request format and size
- Checks required headers
- Prevents malformed requests from reaching business logic

#### PerformanceMonitoringMiddleware
- Measures request execution time
- Tracks latency percentiles (p50, p95, p99)
- Captures performance metrics

### 3. Gateway Middleware (Core)

**GatewayMiddleware** - The heart of request routing:

```csharp
public class GatewayMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Extract request context
        var requestContext = ExtractRequestContext(context);
        
        // 2. Find matching route
        var route = routingService.FindRoute(requestContext);
        
        // 3. Apply policies (rate limit, JWT, etc.)
        ValidateAndEnrichRequest(requestContext, route);
        
        // 4. Check cache
        if (cacheService.TryGetCached(route, out var cachedResponse))
        {
            await RespondWithCached(context, cachedResponse);
            return;
        }
        
        // 5. Call backend(s)
        var response = await CallBackendService(route, requestContext);
        
        // 6. Cache response
        cacheService.Cache(route, response);
        
        // 7. Return to client
        await RespondToClient(context, response);
    }
}
```

### 4. Routing Service

Matches incoming requests to configured routes:

**Routing Algorithm**:
1. Extract path from incoming request
2. Iterate through all configured routes
3. Match request path against route regex pattern
4. Return first matching route or 404

```csharp
// Example: Request path "/api/users/123" 
// Matches route with pattern "^/api/users(/.*)?$"

var route = routes.FirstOrDefault(r => 
    Regex.IsMatch(requestPath, r.Pattern) &&
    (r.Method == "ANY" || r.Method == httpMethod)
);
```

### 5. Policy Services

#### RateLimitingService
- **Algorithm**: Token bucket
- **Storage**: In-memory with client ID key
- **Enforcement**: Returns 429 if limit exceeded
- **Granularity**: Per-client, per-route, per-minute/second

#### CircuitBreakerService
- **State Machine**: CLOSED → OPEN → HALF_OPEN → CLOSED
- **Triggers**: Configurable failure threshold
- **Recovery**: Automatic after timeout or manual success
- **Benefits**: Fail fast, prevent cascading failures

#### CacheService
- **Strategy**: LRU (Least Recently Used) eviction
- **Storage**: In-memory dictionary with TTL
- **Key Pattern**: `{method}:{path}:{query_params}`
- **Invalidation**: TTL-based expiration

### 6. HTTP Client Factory

**HttpClientFactory** - Manages HTTP client pooling:

```
┌─────────────────────────────┐
│  HTTP Client Factory        │
├─────────────────────────────┤
│ ┌────────┐  ┌────────┐      │
│ │Client1 │  │Client2 │  ... │
│ └────────┘  └────────┘      │
└─────────────────────────────┘
      │
      ├─ Connection pooling
      ├─ Keep-alive management
      ├─ SSL certificate validation
      └─ Request/response compression
```

### 7. Data Layer

#### Repositories (In-Memory)

**GatewayRouteRepository**
- Thread-safe dictionary storage
- CRUD operations for routes
- Indexing for fast lookups

**RateLimitRepository**
- Client ID → RateLimitEntry mapping
- Token bucket state per client
- Periodic cleanup of stale entries

**CircuitBreakerRepository**
- Route Name → CircuitBreakerStatus mapping
- Failure/success counters
- State transition tracking

## Request Flow

### Detailed Request Lifecycle

```
1. CLIENT REQUEST RECEIVED
   ↓
2. MIDDLEWARE PIPELINE
   ├─ Logging: Record incoming request
   ├─ Error Handling: Prepare error handlers
   ├─ Authentication: Validate credentials
   ├─ Validation: Check format
   └─ Performance: Start timer
   ↓
3. GATEWAY MIDDLEWARE
   ├─ Extract request context (path, method, headers)
   ├─ Match route using regex
   ├─ If no match → Return 404
   ├─ If matched → Continue
   ↓
4. POLICY VALIDATION
   ├─ Authentication check (if required)
   │  ├─ Validate JWT/API Key
   │  └─ Extract claims/identity
   ├─ Rate limit check
   │  ├─ Check token bucket
   │  └─ If exceeded → Return 429
   └─ Authorization check (if configured)
      └─ Verify required roles
   ↓
5. CACHE CHECK
   ├─ Generate cache key
   ├─ Check if cached response exists
   ├─ If hit → Return cached response
   └─ If miss → Continue to backend
   ↓
6. BACKEND REQUEST
   ├─ Select target (load balancing)
   ├─ Transform request (headers, body)
   ├─ Apply timeout
   ├─ Circuit breaker check
   │  ├─ If OPEN → Return cached/fallback
   │  └─ If CLOSED/HALF_OPEN → Forward
   ├─ Execute HTTP request
   ├─ Capture response
   └─ Update circuit breaker state
   ↓
7. RESPONSE PROCESSING
   ├─ Transform response (headers, body)
   ├─ Cache response (if configured)
   ├─ Format response (JSON/XML/CSV)
   └─ Add gateway headers
   ↓
8. CLIENT RESPONSE
   ├─ Set status code
   ├─ Set headers
   ├─ Write body
   └─ Close connection
   ↓
9. POST-REQUEST PROCESSING
   ├─ Metrics: Record latency, status
   ├─ Logging: Log response details
   ├─ Events: Trigger webhooks (if configured)
   └─ Cleanup: Release resources
```

## Design Patterns

### 1. Chain of Responsibility

**Middleware Pipeline** - Each middleware can process or pass to next:

```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    // Pre-processing
    PreProcess(context);
    
    // Pass to next middleware
    await next(context);
    
    // Post-processing
    PostProcess(context);
}
```

### 2. Service Locator

**ServiceCollection** - Dependency injection container:

```csharp
services.AddScoped<RoutingService>();
services.AddScoped<RateLimitingService>();
services.AddScoped<CircuitBreakerService>();
// Injected where needed via constructor
```

### 3. Repository Pattern

**Repositories** - Abstract data access:

```csharp
public interface IRepository<T>
{
    Task<T?> GetAsync(string key);
    Task AddAsync(string key, T value);
    Task UpdateAsync(string key, T value);
    Task DeleteAsync(string key);
}
```

### 4. Strategy Pattern

**Rate Limiting Strategies**:
- TokenBucket
- SlidingWindow  
- FixedWindow

Each implements same `CheckLimitAsync` interface.

### 5. Circuit Breaker Pattern

**State Machine**:
```
    ┌──────────┐
    │  CLOSED  │◄──────┐
    └────┬─────┘       │
         │ Failures    │
         │ exceed      │ Successes
         ▼ threshold   │ restore
    ┌──────────┐       │
    │  OPEN    ├──────►HALF_OPEN
    └──────────┘       │
         ▲              │
         │              │
         └──────────────┘
```

## Concurrency & Thread Safety

### Thread-Safe Components

**ConcurrentDictionary** - Used for repositories:
```csharp
private ConcurrentDictionary<string, GatewayRoute> routes;
```

**ReaderWriterLockSlim** - For high-read, low-write scenarios:
```csharp
private ReaderWriterLockSlim cacheLock = new();

public bool TryGetCached(string key, out CachedResponse? response)
{
    cacheLock.EnterReadLock();
    try
    {
        return cache.TryGetValue(key, out response);
    }
    finally
    {
        cacheLock.ExitReadLock();
    }
}
```

**Async/Await** - All I/O operations are asynchronous:
```csharp
var response = await httpClient.GetAsync(backendUrl);
```

## Performance Considerations

### 1. Request Pooling

HTTP Clients are pooled and reused:
- Reduces socket exhaustion
- Improves throughput
- Managed by HttpClientFactory

### 2. Memory Management

```csharp
// Cache with size limits
if (cache.Count > MAX_CACHE_SIZE)
{
    cache.Remove(oldestEntry);
}

// Background cleanup of expired entries
var expired = cache
    .Where(x => x.Value.ExpiresAt < DateTime.UtcNow)
    .ToList();
```

### 3. Lock-Free Operations

- Prefer ConcurrentDictionary over Dictionary+Lock
- Use Interlocked operations for counters
- Minimize critical sections

### 4. Async All The Way

```csharp
// Good: Non-blocking
public async Task<Response> ForwardRequestAsync(...)
{
    return await httpClient.SendAsync(request);
}

// Bad: Blocking (avoids!)
public Response ForwardRequest(...)
{
    return httpClient.Send(request);  // Blocks thread
}
```

## Scalability Features

### 1. Horizontal Scaling

Gateway is stateless (except in-memory cache):
- Deploy multiple instances
- Use load balancer (nginx, HAProxy)
- Share configuration via external source

### 2. Load Balancing Algorithms

**Round Robin**:
```
Server 1 → Server 2 → Server 3 → Server 1 → ...
```

**Weighted**:
```
Server 1 (weight=50) → Server 2 (weight=50) → Server 1 → ...
```

### 3. Circuit Breaker for Degradation

When backend service fails:
- Circuit opens immediately
- Returns cached response (if available)
- Fails fast (no timeout wait)
- Automatic recovery attempt

## Extension Points

### 1. Custom Middleware

```csharp
public class CustomMiddleware
{
    private readonly RequestDelegate next;
    
    public CustomMiddleware(RequestDelegate next) 
        => this.next = next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Custom logic
        await next(context);
    }
}

// Register in Program.cs
app.UseMiddleware<CustomMiddleware>();
```

### 2. Custom Route Handlers

```csharp
services.AddScoped<IRouteHandler, CustomRouteHandler>();
```

### 3. Event Hooks

```csharp
public interface IEventBus
{
    void Subscribe(string eventType, Action<Event> handler);
    void Publish(string eventType, Event @event);
}

// Subscribe to route changes
eventBus.Subscribe("RouteCreated", (e) => {
    // Handle route creation
});
```

## Monitoring & Observability

### Metrics Collection

**Collected Metrics**:
- Total requests per route
- Error rates by type
- Latency percentiles (p50, p95, p99)
- Cache hit/miss rates
- Circuit breaker state changes
- Rate limit violations

### Logging Strategy

**Log Levels**:
- **Debug**: Request/response details, middleware flow
- **Information**: Route creation, service startup
- **Warning**: Rate limits reached, circuit breaker opened
- **Error**: Exceptions, backend failures

### Health Checks

**Endpoints**:
- `/health` - Gateway health
- `/health/{service}` - Individual service health

## Technology Stack

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| Runtime | .NET 10 | Latest, highest performance |
| Web Server | Kestrel | Built-in, high-performance |
| DI Container | Built-in | Lightweight, integrated |
| HTTP | HttpClient | Standard, well-tested |
| Logging | ILogger | Standard, extensible |
| Configuration | IConfiguration | Standard, supports multiple sources |
| Threading | System.Threading | Built-in, optimized |

## Future Architecture Enhancements

1. **Distributed Caching** - Redis backend for multi-instance caching
2. **Message Queue Support** - RabbitMQ/Azure Service Bus integration
3. **GraphQL Support** - Query federation across backends
4. **WebSocket Support** - Real-time bidirectional communication
5. **gRPC Support** - High-performance protocol support

---

For implementation details, see the source code in `/Services`, `/Middleware`, and `/Repositories` directories.
