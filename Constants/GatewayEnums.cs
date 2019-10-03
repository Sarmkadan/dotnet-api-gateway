// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Constants;

/// <summary>
/// HTTP methods supported by the gateway
/// </summary>
public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
    HEAD,
    OPTIONS
}

/// <summary>
/// Circuit breaker states for fault tolerance
/// </summary>
public enum CircuitBreakerState
{
    Closed,      // Normal operation, requests pass through
    Open,        // Service is failing, requests are rejected
    HalfOpen     // Testing if service recovered
}

/// <summary>
/// Rate limiting strategy types
/// </summary>
public enum RateLimitStrategy
{
    TokenBucket,     // Token bucket algorithm
    SlidingWindow,   // Sliding window algorithm
    FixedWindow      // Fixed window algorithm
}

/// <summary>
/// Request aggregation types
/// </summary>
public enum AggregationStrategy
{
    Sequential,      // Execute requests one by one
    Parallel,        // Execute requests in parallel
    FirstSuccess     // Stop at first successful response
}

/// <summary>
/// Authentication types
/// </summary>
public enum AuthenticationType
{
    None,
    Bearer,
    ApiKey,
    BasicAuth
}

/// <summary>
/// Cache strategies
/// </summary>
public enum CacheStrategy
{
    NoCache,
    CacheControl,
    Etag
}

/// <summary>
/// Load balancing strategies
/// </summary>
public enum LoadBalancingStrategy
{
    RoundRobin,      // Distribute equally across backends
    LeastConnections, // Route to least busy backend
    IpHash           // Route based on client IP
}
