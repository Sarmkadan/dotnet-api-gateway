// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for managing circuit breaker state and preventing cascading failures
/// </summary>
public class CircuitBreakerService
{
    private readonly CircuitBreakerRepository _repository;

    public CircuitBreakerService(CircuitBreakerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CircuitBreakerStatus> GetOrCreateStatusAsync(string serviceName)
    {
        var status = await _repository.GetByServiceNameAsync(serviceName);

        if (status == null)
        {
            status = new CircuitBreakerStatus { ServiceName = serviceName };
            await _repository.AddAsync(status);
        }

        return status;
    }

    public async Task<bool> IsCircuitOpenAsync(string serviceName)
    {
        var status = await GetOrCreateStatusAsync(serviceName);
        return status.State == CircuitBreakerState.Open;
    }

    public async Task<bool> CanAttemptAsync(string serviceName, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return true;

        var status = await GetOrCreateStatusAsync(serviceName);

        if (status.State == CircuitBreakerState.Closed)
            return true;

        if (status.State == CircuitBreakerState.Open)
        {
            var timeSinceOpen = DateTime.UtcNow - status.LastStateChangeAt;
            if (timeSinceOpen >= TimeSpan.FromSeconds(policy.TimeoutSeconds))
            {
                status.ChangeState(CircuitBreakerState.HalfOpen);
                await _repository.UpdateAsync(status);
                return true;
            }

            throw new CircuitBreakerException(serviceName, (long)TimeSpan.FromSeconds(policy.TimeoutSeconds).TotalSeconds);
        }

        // HalfOpen - allow attempt
        return true;
    }

    public async Task RecordSuccessAsync(string serviceName, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return;

        var status = await GetOrCreateStatusAsync(serviceName);
        status.RecordSuccess();

        if (status.State == CircuitBreakerState.HalfOpen)
        {
            status.SuccessCount++;
            if (status.SuccessCount >= policy.SuccessThreshold)
            {
                status.ChangeState(CircuitBreakerState.Closed);
            }
        }
        else if (status.State == CircuitBreakerState.Closed)
        {
            status.FailureCount = Math.Max(0, status.FailureCount - 1);
        }

        await _repository.UpdateAsync(status);
    }

    public async Task RecordFailureAsync(string serviceName, string error, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return;

        var status = await GetOrCreateStatusAsync(serviceName);
        status.RecordFailure(error);

        if (status.State == CircuitBreakerState.HalfOpen)
        {
            status.ChangeState(CircuitBreakerState.Open);
        }
        else if (status.State == CircuitBreakerState.Closed)
        {
            status.FailureCount++;
            if (status.FailureCount >= policy.FailureThreshold)
            {
                status.ChangeState(CircuitBreakerState.Open);
            }
        }

        await _repository.UpdateAsync(status);
    }

    public async Task<IEnumerable<CircuitBreakerStatus>> GetOpenCircuitsAsync()
    {
        return await _repository.GetOpenCircuitsAsync();
    }

    public async Task<CircuitBreakerStatus?> GetStatusAsync(string serviceName)
    {
        return await _repository.GetByServiceNameAsync(serviceName);
    }

    public async Task<IEnumerable<CircuitBreakerStatus>> GetAllStatusesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task ResetCircuitAsync(string serviceName)
    {
        var status = await GetOrCreateStatusAsync(serviceName);
        status.Reset();
        await _repository.UpdateAsync(status);
    }

    public async Task ResetAllCircuitsAsync()
    {
        await _repository.ResetAllAsync();
    }
}
