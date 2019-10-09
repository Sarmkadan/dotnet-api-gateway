// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Repository for managing gateway route configurations
/// </summary>
public class GatewayRouteRepository : IRepository<GatewayRoute>
{
    private readonly Dictionary<string, GatewayRoute> _routes = [];
    private readonly ReaderWriterLockSlim _lock = new();

    public async Task<GatewayRoute?> GetByIdAsync(string id)
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.TryGetValue(id, out var route) ? route : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<IEnumerable<GatewayRoute>> GetAllAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<GatewayRoute> AddAsync(GatewayRoute entity)
    {
        entity.Validate();
        entity.CreatedAt = DateTime.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            _routes[entity.Id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<GatewayRoute> UpdateAsync(GatewayRoute entity)
    {
        entity.Validate();
        entity.ModifiedAt = DateTime.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            if (!_routes.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Route with ID {entity.Id} not found");

            _routes[entity.Id] = entity;
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
            return _routes.Remove(id);
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
            return _routes.ContainsKey(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<IEnumerable<GatewayRoute>> GetActiveRoutesAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.Values.Where(r => r.IsActive).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<GatewayRoute?> FindRouteByPathAsync(string path, string method)
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.Values.FirstOrDefault(r =>
                r.IsActive &&
                r.MatchesPath(path) &&
                r.SupportsMethod(method));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<IEnumerable<GatewayRoute>> GetRoutesByNameAsync(string name)
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.Values.Where(r =>
                r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void ClearAll()
    {
        _lock.EnterWriteLock();
        try
        {
            _routes.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
