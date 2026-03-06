# FAQ - DotNet API Gateway

Frequently asked questions and answers about the DotNet API Gateway.

## General Questions

### Q: What is the DotNet API Gateway?

**A:** The DotNet API Gateway is a lightweight, production-ready API gateway built on .NET 10. It serves as a central entry point for your microservices, handling authentication, rate limiting, caching, circuit breaking, request routing, and comprehensive monitoring.

### Q: Why use an API Gateway?

**A:** An API Gateway provides:
- **Single entry point** for clients
- **Cross-cutting concerns** (auth, logging, monitoring)
- **Backend abstraction** (clients don't need to know all backend URLs)
- **Security enforcement** (authentication, rate limiting)
- **Resilience patterns** (circuit breaker, retries, fallbacks)
- **Performance optimization** (caching, compression, load balancing)

### Q: Is the gateway stateless?

**A:** Mostly yes. The gateway stores minimal in-memory state:
- Route configuration
- Rate limit counters
- Circuit breaker states
- Cache entries

In multi-instance deployments, you should use distributed storage (Redis) for shared state.

### Q: What are the performance characteristics?

**A:** Typical metrics:
- **Throughput**: 5,000-10,000 req/sec per instance
- **Latency**: 10-100ms per request (overhead)
- **Memory**: 100-500MB depending on cache size
- **CPU**: 10-30% on modern hardware

Performance depends on configuration (caching, transformations, backend latency).

---

## Installation & Setup

### Q: What .NET versions are supported?

**A:** Only .NET 10 is supported. The project uses latest C# features and APIs available in .NET 10.

### Q: Can I run it on .NET 8 or 9?

**A:** Not officially. The project targets `net10.0`. Downgrading would require removing .NET 10-specific features. Consider upgrading your .NET runtime.

### Q: How do I upgrade the gateway?

**A:** 
```bash
# Pull latest code
git pull origin main

# Restore dependencies
dotnet restore

# Build and run
dotnet run -c Release
```

### Q: Can I run multiple instances behind a load balancer?

**A:** Yes! The gateway is stateless (with caveats):

```
Load Balancer
    ↓
├─ Gateway Instance 1
├─ Gateway Instance 2
└─ Gateway Instance 3
```

Use sticky sessions if you need session affinity. For distributed caching, use Redis.

### Q: How do I enable HTTPS?

**A:** In `appsettings.json`:

```json
{
  "GatewayConfiguration": {
    "EnableHttps": true,
    "CertificatePath": "/path/to/cert.pfx",
    "CertificatePassword": "password"
  }
}
```

Or use environment variable:
```bash
export CERT_PASSWORD="your-password"
```

---

## Configuration

### Q: Where is the configuration file?

**A:** In `appsettings.json` in the root directory. You can also create environment-specific files:
- `appsettings.Development.json`
- `appsettings.Production.json`
- `appsettings.Staging.json`

### Q: How do I add a new route?

**A:** Add to `appsettings.json`:

```json
{
  "Routes": [
    {
      "name": "my-service",
      "pattern": "^/api/myservice(/.*)?$",
      "method": "ANY",
      "targets": [
        {
          "url": "http://backend:3000/api/myservice",
          "weight": 100
        }
      ]
    }
  ]
}
```

### Q: Do I need to restart the gateway after configuration changes?

**A:** Yes, configuration is loaded at startup. Changes require a restart. Future versions may support hot-reload.

### Q: What regex patterns are supported?

**A:** Standard .NET regex patterns:

```
^/api/users(/.*)?$      - Matches /api/users and /api/users/*
^/api/[a-z]+/.*         - Matches /api/{lowercase}/anything
^/api/(users|orders)$   - Matches /api/users or /api/orders
```

Test regex: https://regex101.com

### Q: Can I have overlapping route patterns?

**A:** Yes, but the first matching route wins. Order matters! More specific patterns should come first.

---

## Features & Functionality

### Q: How does rate limiting work?

**A:** The gateway uses a token bucket algorithm:

```
Bucket capacity: 100 tokens
Refill rate: 60 tokens per minute

Request arrives:
├─ Token available? → Process request, consume token
└─ No token? → Return 429 Too Many Requests
```

Per-client rate limits prevent one client from affecting others.

### Q: Can I rate-limit by IP address instead of client ID?

**A:** Currently rate limiting uses client ID from JWT claims or API key. To use IP address, you'd need to modify the authentication logic.

### Q: How does caching work?

**A:** Responses are cached based on:
- Route configuration (cache policy enabled/disabled)
- TTL (time to live in seconds)
- Cache key (method + path + query parameters)

Cache is in-memory. When TTL expires, the entry is automatically removed.

### Q: Can I cache different responses differently?

**A:** Currently, caching is per-route with uniform TTL. Future versions may support conditional caching based on response headers.

### Q: How does the circuit breaker interact with rate limiting?

**A:** The gateway processes each request through its middleware pipeline in this order:

```
Request → Routing → Rate Limiting → Forwarding (Circuit Breaker check)
```

**Key implication:** Rate limiting is evaluated *before* the circuit breaker state is checked. This means:

- A request that would be immediately rejected by an open circuit breaker is still counted against the client's rate limit quota.
- If a downstream service is failing and the circuit opens, clients can still exhaust their rate limit window with requests that are instantly rejected.

**Why this ordering?**  
Rate limiting is enforced at the gateway edge to protect both the gateway and downstream services from overload. Allowing circuit-breaker-blocked requests to bypass rate limit counting could let clients issue unlimited requests while the circuit is open, which would create a burst the moment the circuit closes.

**Recommended practice:**  
Configure your circuit breaker `timeoutSeconds` to be shorter than the rate limit window so the circuit has a chance to close (and succeed) within a single rate limit period. Also set appropriate `failureThreshold` values so the circuit does not open on transient errors alone.

```json
{
  "rateLimitPolicy": {
    "requestsPerMinute": 100,
    "burstSize": 20
  },
  "circuitBreakerPolicy": {
    "failureThreshold": 5,
    "successThreshold": 2,
    "timeoutSeconds": 15
  }
}
```

With this configuration the circuit can recover multiple times within the one-minute rate limit window, and clients have budget for both successful requests and the handful of requests that hit the open circuit.

### Q: How does the circuit breaker work?

**A:** State machine:
- **CLOSED**: Normal operation, requests forwarded
- **OPEN**: Threshold failures reached, requests rejected immediately (fail-fast)
- **HALF_OPEN**: Testing recovery, some requests allowed
- Back to **CLOSED**: When successes reach threshold

### Q: When does the circuit breaker reset?

**A:** After `timeoutSeconds` with `successThreshold` consecutive successful requests.

Example:
```json
{
  "failureThreshold": 5,
  "successThreshold": 2,
  "timeoutSeconds": 60
}
```

Opens after 5 failures. Resets after 2 successes within 60 seconds.

### Q: Can I use request aggregation?

**A:** Yes, the `RequestAggregationService` combines multiple backend calls:

```csharp
var requests = new List<(string path, string method)>
{
    ("/api/users/123", "GET"),
    ("/api/posts/user/123", "GET")
};

var result = await aggregationService.AggregateAsync(requests);
```

### Q: Does the gateway support WebSockets?

**A:** Not currently. The gateway handles HTTP/HTTPS only. WebSocket support is planned for future versions.

### Q: Does it support GraphQL?

**A:** Not natively. But you can route GraphQL requests to a GraphQL backend service.

---

## Authentication & Security

### Q: How do I enable JWT authentication?

**A:** In `appsettings.json`:

```json
{
  "JwtValidation": {
    "enabled": true,
    "secretKey": "your-secret-key",
    "issuer": "https://auth-server.com",
    "audience": "api.gateway",
    "validateIssuer": true,
    "validateAudience": true,
    "validateLifetime": true
  }
}
```

### Q: How do I require authentication for specific routes?

**A:** Per-route:

```json
{
  "name": "secure-api",
  "requiresAuthentication": true,
  "requiredRoles": ["admin", "user"]
}
```

### Q: Where do I put the JWT token?

**A:** In the Authorization header:

```bash
curl -H "Authorization: Bearer {token}" http://localhost:5000/api/data
```

### Q: Can I use API keys instead of JWT?

**A:** Yes, configure API key authentication:

```json
{
  "ApiKeyAuthentication": {
    "enabled": true,
    "headerName": "X-API-Key",
    "keys": {
      "client-1": "secret-key-1"
    }
  }
}
```

Request:
```bash
curl -H "X-API-Key: secret-key-1" http://localhost:5000/api/data
```

### Q: How do I store JWT secrets securely?

**A:** Never hardcode secrets! Use:
- Environment variables
- Key Vaults (Azure Key Vault, AWS Secrets Manager)
- Kubernetes Secrets
- Docker Secrets

### Q: Is CORS supported?

**A:** Yes:

```json
{
  "CorsPolicy": {
    "enabled": true,
    "allowedOrigins": ["https://app.example.com"],
    "allowedMethods": ["GET", "POST"],
    "allowedHeaders": ["*"]
  }
}
```

### Q: Can I rate-limit by authentication provider?

**A:** Not directly. Rate limiting is per-client ID. You could extend to extract provider info from JWT claims.

---

## Monitoring & Debugging

### Q: Where are logs stored?

**A:** By default, logs go to console output. Configure additional sinks:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DotNetApiGateway": "Debug"
    }
  }
}
```

For file logging, integrate Serilog.

### Q: How do I check the gateway is working?

**A:** 
```bash
# Health check
curl http://localhost:5000/health

# List routes
curl http://localhost:5000/api/gateway/routes

# Check metrics
curl http://localhost:5000/api/metrics
```

### Q: How do I monitor performance?

**A:** Check `/api/metrics`:

```bash
curl http://localhost:5000/api/metrics | jq

# Monitor in real-time
watch -n 1 'curl -s http://localhost:5000/api/metrics | jq'
```

### Q: How do I debug routing issues?

**A:**
1. Enable debug logging
2. Check route pattern matches
3. Verify backend service is running
4. Test route directly: `curl -v http://localhost:5000/api/path`

### Q: How do I check request/response details?

**A:** Enable request logging middleware:

```json
{
  "GatewayConfiguration": {
    "EnableRequestLogging": true
  }
}
```

Then check logs for detailed request/response info.

### Q: Can I export metrics to Prometheus?

**A:** Implement Prometheus exporter:

```bash
dotnet add package prometheus-net
```

Add endpoint and export metrics.

### Q: How do I find memory leaks?

**A:**
```bash
# Collect memory trace
dotnet trace collect dotnet {PID}

# Analyze
dotnet gcdump analyze core_{timestamp}.gcdump
```

---

## Troubleshooting

### Q: I'm getting 404 errors for valid routes

**A:** Check:
1. Route pattern matches request path (test regex)
2. Route is enabled in configuration
3. Gateway was restarted after configuration change
4. Check logs for route matching errors

### Q: Requests are timing out

**A:**
1. Check backend service health: `curl http://backend:3000/health`
2. Increase timeout: `RequestTimeoutSeconds: 60`
3. Check network connectivity
4. Review gateway metrics for latency

### Q: Getting 429 rate limit errors when not expected

**A:**
1. Check rate limit status: `curl http://localhost:5000/api/gateway/ratelimit/{clientId}`
2. Reset rate limit: `curl -X POST http://localhost:5000/api/gateway/ratelimit/{clientId}/reset`
3. Verify rate limit configuration is correct
4. Check if multiple clients sharing same ID

### Q: Circuit breaker is stuck OPEN

**A:**
1. Check backend service: `curl http://backend:3000/health`
2. Reset circuit: `curl -X POST http://localhost:5000/api/gateway/circuitbreaker/{route}/reset`
3. Review timeout and threshold settings
4. Check gateway logs for failure details

### Q: High memory usage

**A:**
1. Reduce cache TTL
2. Limit cache size: Remove stale entries more aggressively
3. Check request volume
4. Monitor with: `docker stats`

### Q: Gateway crashes under load

**A:**
1. Increase container memory limits
2. Enable async request processing (default enabled)
3. Reduce concurrent request limits
4. Check for memory leaks

---

## Architecture & Design

### Q: Why is the gateway in-memory only?

**A:** Performance. In-memory storage is fast (microseconds). For distributed deployments, you can plug in external caches (Redis).

### Q: Can I use a database for storing routes?

**A:** Currently, routes are in `appsettings.json`. Future versions may support database-driven configuration.

### Q: How does load balancing work?

**A:** Weighted round-robin:

```json
{
  "targets": [
    { "url": "http://api-1", "weight": 50 },
    { "url": "http://api-2", "weight": 30 },
    { "url": "http://api-3", "weight": 20 }
  ]
}
```

### Q: Can I route to other API gateways?

**A:** Yes, an API gateway can route to another API gateway. No issues with chaining.

### Q: Is the gateway designed for gRPC?

**A:** No, it's HTTP/REST only. For gRPC, use gRPC gateway libraries.

---

## Support & Contributing

### Q: Where do I report bugs?

**A:** GitHub Issues: https://github.com/Sarmkadan/dotnet-api-gateway/issues

### Q: How do I contribute?

**A:** Fork → Branch → Code → Pull Request. See CONTRIBUTING.md for guidelines.

### Q: Where can I discuss features?

**A:** GitHub Discussions: https://github.com/Sarmkadan/dotnet-api-gateway/discussions

### Q: Is there commercial support available?

**A:** The project is open-source MIT licensed. For consulting/support, contact the author.

---

## Common Errors

### Error: "Route not found (404)"
- Check route pattern matches request path
- Verify route configuration is correct

### Error: "Authorization header is missing"
- Add `Authorization: Bearer {token}` header
- Enable authentication in route config

### Error: "Certificate not found"
- Verify certificate path in configuration
- Check file permissions
- Ensure certificate is in PFX format

### Error: "Connection refused to backend"
- Verify backend service is running
- Check backend URL is correct
- Verify network connectivity

### Error: "Too many requests (429)"
- Wait for rate limit window to reset
- Reset rate limit manually
- Increase rate limit in configuration

---

## More Help

- **Documentation**: See `/docs` directory
- **Examples**: See `/examples` directory  
- **Source Code**: Browse on GitHub
- **Issues**: Report on GitHub Issues
- **Discussions**: Ask on GitHub Discussions

---

Still have questions? Open an issue on GitHub or check the documentation!
