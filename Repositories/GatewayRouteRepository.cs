#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Primitives;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Repository for managing gateway route configurations with hot-reload support.
/// Provides change tokens for detecting route configuration changes and compiled route tables
/// for efficient route matching.
/// </summary>
public class GatewayRouteRepository : IRepository<GatewayRoute>
{
    private readonly Dictionary<string, GatewayRoute> _routes = [];
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly RouteConfigurationChangeTokenSource _changeTokenSource = new();
    private CompiledRouteTable _compiledRouteTable = new();
    private int _version = 0;

    /// <summary>
    /// Gets the change token that signals when route configuration changes.
    /// </summary>
    public IChangeToken ChangeToken => _changeTokenSource;

    /// <summary>
    /// Gets the current compiled route table for efficient route matching.
    /// </summary>
    public CompiledRouteTable CompiledRouteTable
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _compiledRouteTable;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public async Task<GatewayRoute?> GetByIdAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

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
        ArgumentNullException.ThrowIfNull(entity);

        entity.Validate();
        entity.CreatedAt = DateTime.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            _routes[entity.Id] = entity;
            RecompileRouteTable();
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<GatewayRoute> UpdateAsync(GatewayRoute entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.Validate();
        entity.ModifiedAt = DateTime.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            if (!_routes.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Route with ID {entity.Id} not found");

            _routes[entity.Id] = entity;
            RecompileRouteTable();
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        _lock.EnterWriteLock();
        try
        {
            var removed = _routes.Remove(id);
            if (removed)
            {
                RecompileRouteTable();
            }
            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

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
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(method);

        // Use compiled route table for efficient lookup
        var table = CompiledRouteTable;
        return table.FindRoute(path, method);
    }

    public async Task<IEnumerable<GatewayRoute>> GetRoutesByNameAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

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
            RecompileRouteTable();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<int> GetCountAsync()
    {
        _lock.EnterReadLock();
        try { return Task.FromResult(_routes.Count); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Forces recompilation of the route table and signals any change tokens.
    /// This should be called after any route modification.
    /// </summary>
    private void RecompileRouteTable()
    {
        _lock.EnterReadLock();
        try
        {
            var activeRoutes = _routes.Values.Where(r => r.IsActive).ToList();
            _compiledRouteTable = new CompiledRouteTable(activeRoutes, ++_version);
            _changeTokenSource.SignalChange();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the current hot-reload status including route table version and configuration.
    /// </summary>
    /// <returns>Hot reload status information.</returns>
    public object GetHotReloadStatus()
    {
        _lock.EnterReadLock();
        try
        {
            return new
            {
                routeTableVersion = _version,
                routeCount = _compiledRouteTable.Count,
                changeTokenActive = _changeTokenSource.HasChanged,
                lastChangeVersion = _changeTokenSource.GetChangeCount(),
                timestamp = DateTime.UtcNow,
                isHealthy = true
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}