#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetApiGateway.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Factory for creating rate limit stores.
/// </summary>
public sealed class RateLimitStoreFactory : IRateLimitStoreFactory, IDisposable
{
    private readonly InMemoryRateLimitStore _inMemoryStore;
    private readonly ILogger<RedisRateLimitStore> _redisLogger;
    private readonly ConcurrentDictionary<string, RedisRateLimitStore> _redisStores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitStoreFactory"/> class.
    /// </summary>
    /// <param name="inMemoryStore">The in-memory rate limit store.</param>
    /// <param name="redisLogger">The Redis logger.</param>
    public RateLimitStoreFactory(InMemoryRateLimitStore inMemoryStore, ILogger<RedisRateLimitStore> redisLogger)
    {
        _inMemoryStore = inMemoryStore;
        _redisLogger = redisLogger;
    }

    /// <summary>
    /// Gets the rate limit store for the specified policy.
    /// </summary>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>The rate limit store.</returns>
    public IRateLimitStore GetStore(RateLimitPolicy policy)
    {
        if (!policy.Enabled)
        {
            // If rate limiting is disabled, return a no-op store or just allow
            // For now, we'll return the in-memory store, which will handle policy.Enabled = false
            return _inMemoryStore;
        }

        if (policy.StorageType == RateLimitStorageType.Redis)
        {
            if (string.IsNullOrWhiteSpace(policy.RedisConnectionString))
            {
                _redisLogger.LogError("RedisConnectionString is required for Redis rate limiting. Falling back to InMemory store for policy {PolicyId}.", policy.Id);
                return _inMemoryStore;
            }

            return _redisStores.GetOrAdd(policy.RedisConnectionString, cs => new RedisRateLimitStore(cs, _redisLogger));
        }

        return _inMemoryStore;
    }

    /// <summary>
    /// Gets all rate limit stores.
    /// </summary>
    /// <returns>An enumerable of rate limit stores.</returns>
    public IEnumerable<IRateLimitStore> GetAllStores()
    {
        yield return _inMemoryStore;
        foreach (var redisStore in _redisStores.Values)
        {
            yield return redisStore;
        }
    }

    /// <summary>
    /// Gets all rate limit entries from all stores.
    /// </summary>
    /// <returns>A collection of all rate limit entries.</returns>
    public async Task<IEnumerable<RateLimitEntry>> GetAllEntriesAsync()
    {
        var allEntries = new List<RateLimitEntry>();

        // Get entries from in-memory store
        var inMemoryEntries = await _inMemoryStore.GetAllEntriesAsync();
        allEntries.AddRange(inMemoryEntries);

        // Get entries from all Redis stores
        foreach (var redisStore in _redisStores.Values)
        {
            var redisEntries = await redisStore.GetAllEntriesAsync();
            allEntries.AddRange(redisEntries);
        }

        return allEntries;
    }

    /// <summary>
    /// Disposes of the rate limit stores.
    /// </summary>
    public void Dispose()
    {
        foreach (var redisStore in _redisStores.Values)
        {
            // Assuming RedisRateLimitStore might have a Dispose method to close connections
            // (StackExchange.Redis ConnectionMultiplexer is IDisposable)
            if (redisStore is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _redisStores.Clear();
    }
}
