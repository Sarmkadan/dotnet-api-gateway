#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Constants;

/// <summary>
/// Core constants used throughout the API gateway
/// </summary>
public static class GatewayConstants
{
    // Routing constants
    public const string DefaultTimeoutSeconds = "30";
    public const int MaxRequestBodySizeMb = 10;
    public const int DefaultMaxConcurrentRequests = 100;

    // Rate limiting constants
    public const int DefaultRateLimitPerMinute = 1000;
    public const int DefaultRateLimitPerHour = 50000;
    public const int RateLimitWindowMinutes = 60;

    // Circuit breaker constants
    public const int DefaultCircuitBreakerThreshold = 5;
    public const int DefaultCircuitBreakerTimeoutSeconds = 60;
    public const int CircuitBreakerResetAttempts = 3;

    // Cache constants
    public const int DefaultCacheDurationMinutes = 5;
    public const int MaxCacheSizeMb = 100;

    // JWT constants
    public const string JwtBearerScheme = "Bearer";
    public const int JwtClockSkewSeconds = 60;

    // Header names
    public const string RateLimitHeader = "X-RateLimit-Limit";
    public const string RateLimitRemainingHeader = "X-RateLimit-Remaining";
    public const string RateLimitResetHeader = "X-RateLimit-Reset";
    public const string RequestIdHeader = "X-Request-Id";
    public const string AuthorizationHeader = "Authorization";

    // Error codes
    public const string ErrorCodeRateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string ErrorCodeCircuitBreakerOpen = "CIRCUIT_BREAKER_OPEN";
    public const string ErrorCodeUnauthorized = "UNAUTHORIZED";
    public const string ErrorCodeInvalidRoute = "INVALID_ROUTE";
}
