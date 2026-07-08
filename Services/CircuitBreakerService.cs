#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for managing circuit breaker state and preventing cascading failures.
/// Uses per-service locking to prevent race conditions during concurrent state transitions.
/// </summary>
public sealed class CircuitBreakerService
{
    private readonly CircuitBreakerRepository _repository;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _serviceLocks = new();

    public CircuitBreakerService(CircuitBreakerRepository repository, ILogger<CircuitBreakerService>? logger = null)
    {
        _repository = repository;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreakerService>.Instance;
    }

    /// <summary>
    /// Gets or creates a circuit breaker status entry for the given service.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <returns>Current circuit breaker status.</returns>
    public async Task<CircuitBreakerStatus> GetOrCreateStatusAsync(string serviceName)
    {
        var status = await _repository.GetByServiceNameAsync(serviceName);

        if (status is null)
        {
            status = new CircuitBreakerStatus { ServiceName = serviceName };
            await _repository.AddAsync(status);
            _logger.LogInformation("Created circuit breaker for service: {ServiceName}", serviceName);
        }

        return status;
    }

    /// <summary>
    /// Checks whether the circuit is currently open for the specified service.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <returns>True if the circuit is open and requests should be blocked.</returns>
    public async Task<bool> IsCircuitOpenAsync(string serviceName)
    {
        var status = await GetOrCreateStatusAsync(serviceName);
        return status.State == CircuitBreakerState.Open;
    }

    /// <summary>
    /// Determines whether a request attempt is allowed based on circuit breaker state.
    /// Automatically transitions from Open to HalfOpen after the timeout period.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <param name="policy">The circuit breaker policy configuration.</param>
    /// <returns>True if the request is allowed to proceed.</returns>
    /// <exception cref="CircuitBreakerException">Thrown when circuit is open and timeout has not elapsed.</exception>
    public async Task<bool> CanAttemptAsync(string serviceName, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return true;

        var serviceLock = _serviceLocks.GetOrAdd(serviceName, _ => new SemaphoreSlim(1, 1));
        await serviceLock.WaitAsync();
        try
        {
            var status = await GetOrCreateStatusAsync(serviceName);

            if (status.State == CircuitBreakerState.Closed)
                return true;

            if (status.State == CircuitBreakerState.Open)
            {
                var timeSinceOpen = DateTime.UtcNow - status.LastStateChangeAt;
                if (timeSinceOpen >= TimeSpan.FromSeconds(policy.TimeoutSeconds))
                {
                    var previousState = status.State;
                    status.ChangeState(CircuitBreakerState.HalfOpen);
                    await _repository.UpdateAsync(status);
                    _logger.LogInformation(
                        "Circuit breaker for {ServiceName} transitioned from {PreviousState} to {NewState} after {ElapsedSeconds}s timeout",
                        serviceName, previousState, CircuitBreakerState.HalfOpen, (int)timeSinceOpen.TotalSeconds);
                    return true;
                }

                var remainingSeconds = (long)(TimeSpan.FromSeconds(policy.TimeoutSeconds) - timeSinceOpen).TotalSeconds;
                throw new CircuitBreakerException(serviceName, remainingSeconds);
            }

            // HalfOpen - allow attempt
            return true;
        }
        finally
        {
            serviceLock.Release();
        }
    }

    /// <summary>
    /// Records a successful request for the specified service.
    /// In HalfOpen state, enough consecutive successes will close the circuit.
    /// In Closed state, decrements the failure counter to improve health.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <param name="policy">The circuit breaker policy configuration.</param>
    public async Task RecordSuccessAsync(string serviceName, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return;

        var serviceLock = _serviceLocks.GetOrAdd(serviceName, _ => new SemaphoreSlim(1, 1));
        await serviceLock.WaitAsync();
        try
        {
            var status = await GetOrCreateStatusAsync(serviceName);
            status.RecordSuccess();

            if (status.State == CircuitBreakerState.HalfOpen)
            {
                if (status.SuccessCount >= policy.SuccessThreshold)
                {
                    status.ChangeState(CircuitBreakerState.Closed);
                    _logger.LogInformation(
                        "Circuit breaker for {ServiceName} closed after {SuccessCount} consecutive successes",
                        serviceName, status.SuccessCount);
                }
            }
            else if (status.State == CircuitBreakerState.Closed)
            {
                status.FailureCount = Math.Max(0, status.FailureCount - 1);
            }

            await _repository.UpdateAsync(status);
        }
        finally
        {
            serviceLock.Release();
        }
    }

    /// <summary>
    /// Records a failed request for the specified service.
    /// In HalfOpen state, a single failure immediately reopens the circuit.
    /// In Closed state, reaching the failure threshold opens the circuit.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <param name="error">Description of the error that occurred.</param>
    /// <param name="policy">The circuit breaker policy configuration.</param>
    public async Task RecordFailureAsync(string serviceName, string error, CircuitBreakerPolicy policy)
    {
        if (!policy.Enabled)
            return;

        var serviceLock = _serviceLocks.GetOrAdd(serviceName, _ => new SemaphoreSlim(1, 1));
        await serviceLock.WaitAsync();
        try
        {
            var status = await GetOrCreateStatusAsync(serviceName);
            status.RecordFailure(error);

            if (status.State == CircuitBreakerState.HalfOpen)
            {
                status.ChangeState(CircuitBreakerState.Open);
                _logger.LogWarning(
                    "Circuit breaker for {ServiceName} reopened from HalfOpen after failure: {Error}",
                    serviceName, error);
            }
            else if (status.State == CircuitBreakerState.Closed)
            {
                if (status.FailureCount >= policy.FailureThreshold)
                {
                    status.ChangeState(CircuitBreakerState.Open);
                    _logger.LogWarning(
                        "Circuit breaker for {ServiceName} opened after {FailureCount} failures (threshold: {Threshold}). Last error: {Error}",
                        serviceName, status.FailureCount, policy.FailureThreshold, error);
                }
            }

            await _repository.UpdateAsync(status);
        }
        finally
        {
            serviceLock.Release();
        }
    }

    /// <summary>
    /// Gets all circuit breakers currently in the Open state.
    /// </summary>
    /// <returns>Collection of open circuit breaker statuses.</returns>
    public async Task<IEnumerable<CircuitBreakerStatus>> GetOpenCircuitsAsync()
    {
        return await _repository.GetOpenCircuitsAsync();
    }

    /// <summary>
    /// Gets the current status of a specific service's circuit breaker.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <returns>Circuit breaker status, or null if not found.</returns>
    public async Task<CircuitBreakerStatus?> GetStatusAsync(string serviceName)
    {
        return await _repository.GetByServiceNameAsync(serviceName);
    }

    /// <summary>
    /// Gets all circuit breaker statuses across all services.
    /// </summary>
    /// <returns>Collection of all circuit breaker statuses.</returns>
    public async Task<IEnumerable<CircuitBreakerStatus>> GetAllStatusesAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// <summary>
    /// Resets the circuit breaker for a specific service back to the Closed state.
    /// Clears all failure and success counters.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    public async Task ResetCircuitAsync(string serviceName)
    {
        var serviceLock = _serviceLocks.GetOrAdd(serviceName, _ => new SemaphoreSlim(1, 1));
        await serviceLock.WaitAsync();
        try
        {
            var status = await GetOrCreateStatusAsync(serviceName);
            var previousState = status.State;
            status.Reset();
            await _repository.UpdateAsync(status);
            _logger.LogInformation(
                "Circuit breaker for {ServiceName} manually reset from {PreviousState} to Closed",
                serviceName, previousState);
        }
        finally
        {
            serviceLock.Release();
        }
    }

    /// <summary>
    /// Resets all circuit breakers to the Closed state.
    /// </summary>
    public async Task ResetAllCircuitsAsync()
    {
        _logger.LogInformation("Resetting all circuit breakers");
        await _repository.ResetAllAsync();
    }
}
