#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetApiGateway.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Repositories;

public sealed class RateLimitStoreFactory : IRateLimitStoreFactory, IDisposable
{
    private readonly InMemoryRateLimitStore _inMemoryStore;
    private readonly ILogger<RedisRateLimitStore> _redisLogger;
    private readonly ConcurrentDictionary<string, RedisRateLimitStore> _redisStores = new();

    public RateLimitStoreFactory(InMemoryRateLimitStore inMemoryStore, ILogger<RedisRateLimitStore> redisLogger)
    {
        _inMemoryStore = inMemoryStore;
        _redisLogger = redisLogger;
    }

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

    public IEnumerable<IRateLimitStore> GetAllStores()
    {
        yield return _inMemoryStore;
        foreach (var redisStore in _redisStores.Values)
        {
            yield return redisStore;
        }
    }

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
