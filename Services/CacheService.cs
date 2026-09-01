#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using System.Collections.Concurrent;

/// <summary>
/// Service for managing response caching with configurable strategies
/// </summary>
public sealed class CacheService : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Timer _cleanupTimer;

    public CacheService()
    {
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public bool TryGetCachedResponse(string cacheKey, out CacheEntry? entry)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey);

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cacheEntry))
            {
                _lock.EnterWriteLock();
                try
                {
                    if (cacheEntry.IsExpired())
                    {
                        _cache.TryRemove(new KeyValuePair<string, CacheEntry>(cacheKey, cacheEntry));
                        entry = null;
                        return false;
                    }

                    cacheEntry.IncrementHitCount();
                    cacheEntry.LastAccessAt = DateTime.UtcNow;
                    entry = cacheEntry;
                    return true;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            entry = null;
            return false;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void SetCachedResponse(
        string cacheKey,
        int statusCode,
        string responseBody,
        Dictionary<string, string> headers,
        int durationSeconds)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey);
        ArgumentException.ThrowIfNullOrEmpty(responseBody);
        ArgumentNullException.ThrowIfNull(headers);

        var entry = new CacheEntry
        {
            Key = cacheKey,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            Headers = headers,
            ExpiresAt = DateTime.UtcNow.AddSeconds(durationSeconds),
            CachedAt = DateTime.UtcNow
        };

        _cache[cacheKey] = entry;
    }

    public void InvalidateCache(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey);
        _cache.TryRemove(cacheKey, out _);
    }

    public void InvalidateCacheByPrefix(string prefix)
    {
        InvalidateByPrefix(prefix);
    }

    public int InvalidateByPrefix(string keyPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyPrefix);

        _lock.EnterWriteLock();
        try
        {
            var removedCount = 0;
            foreach (var key in _cache.Keys)
            {
                if (key.StartsWith(keyPrefix, StringComparison.Ordinal) &&
                    _cache.TryRemove(key, out _))
                    removedCount++;
            }

            return removedCount;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToArray();
        var totalHits = entries.Sum(e => e.HitCount);
        var stats = new CacheStatistics
        {
            EntriesCount = entries.Length,
            TotalHits = totalHits,
            TotalSizeBytes = entries.Sum(e => e.GetSizeBytes()),
            OldestEntry = entries.OrderBy(e => e.CachedAt).FirstOrDefault()?.CachedAt,
            MostAccessedEntry = entries.OrderByDescending(e => e.HitCount).FirstOrDefault()?.Key
        };

        return stats;
    }

    /// <summary>
    /// Remove all expired entries from the cache and return the number removed.
    /// </summary>
    public Task<int> RemoveExpiredEntriesAsync()
    {
        var removedCount = 0;
        foreach (var entry in _cache)
        {
            if (entry.Value.IsExpired() &&
                _cache.TryRemove(new KeyValuePair<string, CacheEntry>(entry.Key, entry.Value)))
                removedCount++;
        }

        return Task.FromResult(removedCount);
    }

    /// <summary>
    /// Get a strongly-typed cached value, deserialized from the underlying cache entry.
    /// </summary>
    public Task<T?> GetAsync<T>(string cacheKey) where T : class
    {
        if (TryGetCachedResponse(cacheKey, out var entry) && entry is not null)
        {
            try
            {
                return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(entry.ResponseBody));
            }
            catch (System.Text.Json.JsonException)
            {
                return Task.FromResult<T?>(null);
            }
        }

        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Store a strongly-typed value in the cache, serialized as JSON, for the given duration.
    /// </summary>
    public Task SetAsync<T>(string cacheKey, T value, TimeSpan duration) where T : class
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        SetCachedResponse(cacheKey, 200, json, [], (int)Math.Max(1, duration.TotalSeconds));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidate all cached entries whose key starts with the given prefix.
    /// </summary>
    public Task InvalidatePrefixAsync(string prefix)
    {
        InvalidateCacheByPrefix(prefix);
        return Task.CompletedTask;
    }

    public void ClearAll()
    {
        Clear();
    }

    public int Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            var removedCount = _cache.Count;
            _cache.Clear();
            return removedCount;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        foreach (var entry in _cache)
        {
            if (entry.Value.IsExpired())
                _cache.TryRemove(new KeyValuePair<string, CacheEntry>(entry.Key, entry.Value));
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _lock.Dispose();
    }
}

public sealed class CacheEntry
{
    private long _hitCount;

    public string Key { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime LastAccessAt { get; set; }
    public long HitCount
    {
        get => Interlocked.Read(ref _hitCount);
        set => Interlocked.Exchange(ref _hitCount, value);
    }

    internal void IncrementHitCount()
    {
        Interlocked.Increment(ref _hitCount);
    }

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

    public override string ToString()
    {
        return $"CacheEntry {{ Key = {Key}, StatusCode = {StatusCode}, ResponseBody = {ResponseBody}, Headers = {Headers}, ExpiresAt = {ExpiresAt}, CachedAt = {CachedAt} }}";
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
