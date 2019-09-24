// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Thrown when circuit breaker is open and requests cannot be processed
/// </summary>
public class CircuitBreakerException : GatewayException
{
    public string ServiceName { get; set; }
    public long RetryAfterSeconds { get; set; }

    public CircuitBreakerException(
        string serviceName,
        long retryAfterSeconds)
        : base(
            $"Circuit breaker is open for service {serviceName}. Retry after {retryAfterSeconds}s",
            "CIRCUIT_BREAKER_OPEN",
            503)
    {
        ServiceName = serviceName;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
