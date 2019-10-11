#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for managing response caching with configurable strategies
/// </summary>
public sealed class CacheService
{
    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Timer _cleanupTimer;

    public CacheService()
    {
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public bool TryGetCachedResponse(string cacheKey, out CacheEntry? entry)
    {
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cacheEntry))
            {
                if (cacheEntry.IsExpired())
                {
                    entry = null;
                    return false;
                }

                cacheEntry.HitCount++;
                cacheEntry.LastAccessAt = DateTime.UtcNow;
                entry = cacheEntry;
                return true;
            }

            entry = null;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void SetCachedResponse(
        string cacheKey,
        int statusCode,
        string responseBody,
        Dictionary<string, string> headers,
        int durationSeconds)
    {
        var entry = new CacheEntry
        {
            Key = cacheKey,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            Headers = headers,
            ExpiresAt = DateTime.UtcNow.AddSeconds(durationSeconds),
            CachedAt = DateTime.UtcNow
        };

        _lock.EnterWriteLock();
        try
        {
            _cache[cacheKey] = entry;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void InvalidateCache(string cacheKey)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Remove(cacheKey);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void InvalidateCacheByPrefix(string prefix)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
                _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public CacheStatistics GetStatistics()
    {
        _lock.EnterReadLock();
        try
        {
            var totalHits = _cache.Values.Sum(e => e.HitCount);
            var stats = new CacheStatistics
            {
                EntriesCount = _cache.Count,
                TotalHits = totalHits,
                TotalSizeBytes = _cache.Values.Sum(e => e.GetSizeBytes()),
                OldestEntry = _cache.Values.OrderBy(e => e.CachedAt).FirstOrDefault()?.CachedAt,
                MostAccessedEntry = _cache.Values.OrderByDescending(e => e.HitCount).FirstOrDefault()?.Key
            };

            return stats;
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
            _cache.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

public sealed class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime LastAccessAt { get; set; }
    public long HitCount { get; set; } = 0;

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    public long GetSizeBytes()
    {
        var size = System.Text.Encoding.UTF8.GetByteCount(Key);
        size += System.Text.Encoding.UTF8.GetByteCount(ResponseBody);
        foreach (var header in Headers)
        {
            size += System.Text.Encoding.UTF8.GetByteCount(header.Key);
            size += System.Text.Encoding.UTF8.GetByteCount(header.Value);
        }
        return size;
    }
}

public sealed class CacheStatistics
{
    public int EntriesCount { get; set; }
    public long TotalHits { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime? OldestEntry { get; set; }
    public string? MostAccessedEntry { get; set; }

    public double GetHitRate()
    {
        return EntriesCount > 0 ? (double)TotalHits / EntriesCount : 0;
    }

    public double GetAverageSizePerEntryBytes()
    {
        return EntriesCount > 0 ? (double)TotalSizeBytes / EntriesCount : 0;
    }
}
