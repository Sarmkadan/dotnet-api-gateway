#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Tracks the current state and statistics of a circuit breaker
/// </summary>
public sealed class CircuitBreakerStatus
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServiceName { get; set; } = string.Empty;
    public CircuitBreakerState State { get; set; } = CircuitBreakerState.Closed;
    public int FailureCount { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public DateTime LastStateChangeAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastFailureAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public int TotalFailures { get; set; } = 0;
    public int TotalSuccesses { get; set; } = 0;
    public long TotalRequests { get; set; } = 0;
    public string? LastError { get; set; }

    public void RecordSuccess()
    {
        SuccessCount++;
        TotalSuccesses++;
        TotalRequests++;
        LastSuccessAt = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        FailureCount++;
        TotalFailures++;
        TotalRequests++;
        LastFailureAt = DateTime.UtcNow;
        LastError = error;
    }

    public void ChangeState(CircuitBreakerState newState)
    {
        if (State != newState)
        {
            State = newState;
            LastStateChangeAt = DateTime.UtcNow;

            if (newState == CircuitBreakerState.Closed)
            {
                FailureCount = 0;
                SuccessCount = 0;
            }
            else if (newState == CircuitBreakerState.HalfOpen)
            {
                SuccessCount = 0;
            }
        }
    }

    public void Reset()
    {
        State = CircuitBreakerState.Closed;
        FailureCount = 0;
        SuccessCount = 0;
        LastError = null;
        LastStateChangeAt = DateTime.UtcNow;
    }

    public double GetSuccessRate()
    {
        if (TotalRequests == 0)
            return 1.0;

        return (double)TotalSuccesses / TotalRequests;
    }

    public double GetFailureRate()
    {
        return 1.0 - GetSuccessRate();
    }
}
