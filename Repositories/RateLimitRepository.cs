#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Repository for managing rate limit entries
/// </summary>
public class RateLimitRepository : IRepository<RateLimitEntry>
{
    private readonly Dictionary<string, RateLimitEntry> _entries = [];
    private readonly ReaderWriterLockSlim _lock = new();

    public async Task<RateLimitEntry?> GetByIdAsync(string id)
    {
        _lock.EnterReadLock();
        try
        {
            return _entries.TryGetValue(id, out var entry) ? entry : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<IEnumerable<RateLimitEntry>> GetAllAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return _entries.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<RateLimitEntry> AddAsync(RateLimitEntry entity)
    {
        _lock.EnterWriteLock();
        try
        {
            _entries[entity.Id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<RateLimitEntry> UpdateAsync(RateLimitEntry entity)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_entries.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Rate limit entry with ID {entity.Id} not found");

            _entries[entity.Id] = entity;
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
            return _entries.Remove(id);
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
            return _entries.ContainsKey(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<RateLimitEntry?> GetByClientAndRouteAsync(string clientId, string routeId)
    {
        _lock.EnterReadLock();
        try
        {
            return _entries.Values.FirstOrDefault(e =>
                e.ClientId == clientId && e.RouteId == routeId);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task<IEnumerable<RateLimitEntry>> GetByClientAsync(string clientId)
    {
        _lock.EnterReadLock();
        try
        {
            return _entries.Values.Where(e => e.ClientId == clientId).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task CleanupExpiredEntriesAsync()
    {
        _lock.EnterWriteLock();
        try
        {
            var now = DateTime.UtcNow;
            var keysToRemove = _entries
                .Where(kvp => now - kvp.Value.LastRequestAt > TimeSpan.FromHours(24))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _entries.Remove(key);
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
            _entries.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
