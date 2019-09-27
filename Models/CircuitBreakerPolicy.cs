#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines circuit breaker rules for fault tolerance
/// </summary>
public sealed class CircuitBreakerPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int FailureThreshold { get; set; } = 5;
    public int SuccessThreshold { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 60;
    public int[] FailureStatusCodes { get; set; } = [500, 502, 503, 504];
    public bool Enabled { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 100;

    public void Validate()
    {
        if (FailureThreshold < 1)
            throw new ArgumentException("FailureThreshold must be at least 1");

        if (SuccessThreshold < 1)
            throw new ArgumentException("SuccessThreshold must be at least 1");

        if (TimeoutSeconds < 10 || TimeoutSeconds > 600)
            throw new ArgumentException("TimeoutSeconds must be between 10 and 600");

        if (MaxRetries < 0 || MaxRetries > 10)
            throw new ArgumentException("MaxRetries must be between 0 and 10");

        if (RetryDelayMilliseconds < 10 || RetryDelayMilliseconds > 5000)
            throw new ArgumentException("RetryDelayMilliseconds must be between 10 and 5000");
    }

    public bool IsFailureStatus(int statusCode)
    {
        return FailureStatusCodes.Contains(statusCode);
    }

    public bool IsEnabled()
    {
        return Enabled;
    }
}
