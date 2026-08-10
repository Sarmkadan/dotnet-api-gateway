#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Repository for managing circuit breaker statuses
/// </summary>
/// <summary>
/// Repository for managing circuit breaker statuses
/// </summary>
public class CircuitBreakerRepository : IRepository<CircuitBreakerStatus>
{
    private readonly Dictionary<string, CircuitBreakerStatus> _statuses = [];
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Retrieves a circuit breaker status by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the circuit breaker status to retrieve.</param>
    /// <returns>The circuit breaker status associated with the given identifier, or null if not found.</returns>
    public async Task<CircuitBreakerStatus?> GetByIdAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _lock.EnterReadLock();
        try
        {
            return _statuses.TryGetValue(id, out var status) ? status : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Retrieves all circuit breaker statuses.
    /// </summary>
    /// <returns>A collection of all circuit breaker statuses.</returns>
    public async Task<IEnumerable<CircuitBreakerStatus>> GetAllAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return _statuses.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds a new circuit breaker status to the repository.
    /// </summary>
    /// <param name="entity">The circuit breaker status to add.</param>
    /// <returns>The added circuit breaker status.</returns>
    public async Task<CircuitBreakerStatus> AddAsync(CircuitBreakerStatus entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _lock.EnterWriteLock();
        try
        {
            _statuses[entity.Id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Updates an existing circuit breaker status in the repository.
    /// </summary>
    /// <param name="entity">The circuit breaker status to update.</param>
    /// <returns>The updated circuit breaker status.</returns>
    public async Task<CircuitBreakerStatus> UpdateAsync(CircuitBreakerStatus entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _lock.EnterWriteLock();
        try
        {
            if (!_statuses.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Circuit breaker status with ID {entity.Id} not found");

            _statuses[entity.Id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Deletes a circuit breaker status by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the circuit breaker status to delete.</param>
    /// <returns>True if the circuit breaker status was deleted; otherwise, false.</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _lock.EnterWriteLock();
        try
        {
            return _statuses.Remove(id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Checks if a circuit breaker status with the given identifier exists.
    /// </summary>
    /// <param name="id">The identifier to check for existence.</param>
    /// <returns>True if a circuit breaker status with the given identifier exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _lock.EnterReadLock();
        try
        {
            return _statuses.ContainsKey(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Retrieves a circuit breaker status by its service name.
    /// </summary>
    /// <param name="serviceName">The service name of the circuit breaker status to retrieve.</param>
    /// <returns>The circuit breaker status associated with the given service name, or null if not found.</returns>
    public async Task<CircuitBreakerStatus?> GetByServiceNameAsync(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        _lock.EnterReadLock();
        try
        {
            return _statuses.Values.FirstOrDefault(s =>
                s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Retrieves all circuit breaker statuses with the given state.
    /// </summary>
    /// <param name="state">The state of the circuit breaker statuses to retrieve.</param>
    /// <returns>A collection of circuit breaker statuses with the given state.</returns>
    public async Task<IEnumerable<CircuitBreakerStatus>> GetByStateAsync(CircuitBreakerState state)
    {
        _lock.EnterReadLock();
        try
        {
            return _statuses.Values.Where(s => s.State == state).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Retrieves all circuit breaker statuses that are currently open.
    /// </summary>
    /// <returns>A collection of circuit breaker statuses that are currently open.</returns>
    public async Task<IEnumerable<CircuitBreakerStatus>> GetOpenCircuitsAsync()
    {
        return await GetByStateAsync(CircuitBreakerState.Open);
    }

    /// <summary>
    /// Resets all circuit breaker statuses to their initial state.
    /// </summary>
    public async Task ResetAllAsync()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var status in _statuses.Values)
                status.Reset();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all circuit breaker statuses from the repository.
    /// </summary>
    public void ClearAll()
    {
        _lock.EnterWriteLock();
        try
        {
            _statuses.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
