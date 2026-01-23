// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Repository for managing circuit breaker statuses
/// </summary>
public class CircuitBreakerRepository : IRepository<CircuitBreakerStatus>
{
    private readonly Dictionary<string, CircuitBreakerStatus> _statuses = [];
    private readonly ReaderWriterLockSlim _lock = new();

    public async Task<CircuitBreakerStatus?> GetByIdAsync(string id)
    {
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

    public async Task<CircuitBreakerStatus> AddAsync(CircuitBreakerStatus entity)
    {
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

    public async Task<CircuitBreakerStatus> UpdateAsync(CircuitBreakerStatus entity)
    {
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

    public async Task<bool> DeleteAsync(string id)
    {
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

    public async Task<bool> ExistsAsync(string id)
    {
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

    public async Task<CircuitBreakerStatus?> GetByServiceNameAsync(string serviceName)
    {
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

    public async Task<IEnumerable<CircuitBreakerStatus>> GetOpenCircuitsAsync()
    {
        return await GetByStateAsync(CircuitBreakerState.Open);
    }

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
