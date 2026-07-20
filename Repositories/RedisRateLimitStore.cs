#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Redis-backed implementation of IRateLimitStore for distributed rate limiting.
/// Supports FixedWindow, SlidingWindow, and TokenBucket strategies using Redis data structures.
/// </summary>
public sealed class RedisRateLimitStore : IRateLimitStore, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisRateLimitStore> _logger;

    public RedisRateLimitStore(string connectionString, ILogger<RedisRateLimitStore> logger)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
        _logger = logger;
        _logger.LogInformation("RedisRateLimitStore initialized with connection string: {ConnectionString}", connectionString);
    }

    public void Dispose()
    {
        _redis.Dispose();
    }

    public async Task<bool> IsRequestAllowedAsync(string key, RateLimitPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogError("Rate limit key cannot be null or empty.");
            return true; // Allow if key is invalid to prevent blocking legitimate requests
        }

        var now = DateTime.UtcNow;
        var redisKey = GetRedisKey(key, policy);

        switch (policy.Strategy)
        {
            case RateLimitStrategy.FixedWindow:
                return await HandleFixedWindowAsync(redisKey, policy, now);
            case RateLimitStrategy.SlidingWindow:
                return await HandleSlidingWindowAsync(redisKey, policy, now);
            case RateLimitStrategy.TokenBucket:
                return await HandleTokenBucketAsync(redisKey, policy, now);
            default:
                _logger.LogWarning("Unsupported rate limit strategy: {Strategy}. Falling back to allowing request.", policy.Strategy);
                return true;
        }
    }

    public async Task<RateLimitEntry> GetEntryAsync(string key, RateLimitPolicy policy)
    {
        var redisKey = GetRedisKey(key, policy);
        var now = DateTime.UtcNow;

        int count = 0;
        int remainingTimeSeconds = 0;

        var ttl = await _db.KeyTimeToLiveAsync(redisKey);
        if (ttl.HasValue)
        {
            remainingTimeSeconds = (int)ttl.Value.TotalSeconds;
        }
        else
        {
            // If no TTL, it might be a new key or an expired one.
            // For token bucket, we might need to check fields for last refill time.
            remainingTimeSeconds = policy.RequestsPerMinute > 0 ? 60 : 3600; // Default to policy window
        }

        switch (policy.Strategy)
        {
            case RateLimitStrategy.FixedWindow:
                var rawCount = await _db.StringGetAsync(redisKey);
                count = rawCount.TryParse(out int parsedCount) ? parsedCount : 0;
                break;
            case RateLimitStrategy.SlidingWindow:
                var windowDuration = TimeSpan.FromMinutes(1); // Assuming 1-minute window
                var cleanBefore = now - windowDuration;
                count = (int)await _db.SortedSetLengthAsync(redisKey, (double)cleanBefore.Ticks, (double)now.Ticks);
                break;
            case RateLimitStrategy.TokenBucket:
                // For token bucket, count refers to tokens available.
                var hashEntries = await _db.HashGetAllAsync(redisKey);
                var tokens = (double)policy.BurstSize;
                var lastRefillSeconds = ToUnixSeconds(now);

                foreach (var entry in hashEntries)
                {
                    if (entry.Name == "tokens") tokens = (double)entry.Value;
                    if (entry.Name == "last_refill_time") lastRefillSeconds = (double)entry.Value;
                }

                var refillRate = policy.RequestsPerMinute / 60.0;
                var timeElapsed = Math.Max(0, ToUnixSeconds(now) - lastRefillSeconds);
                tokens = Math.Min(tokens + (timeElapsed * refillRate), policy.BurstSize);

                count = (int)tokens; // Approximate tokens as count
                if (tokens >= 1) remainingTimeSeconds = 1; // Indicate a token is available
                else if (refillRate > 0) remainingTimeSeconds = (int)Math.Ceiling(1 / refillRate); // Time until next token
                else remainingTimeSeconds = 0; // No refill if rate is zero

                break;
        }

        return new RateLimitEntry
        {
            Key = key,
            Count = count,
            LastRequest = now, // Approximation
            RemainingTimeSeconds = remainingTimeSeconds
        };
    }

    public async Task ResetKeyAsync(string key)
    {
        // Need to iterate through all possible policy-based keys or pass the policy explicitly
        // For simplicity, we'll assume a single policy context or require policy for precise reset.
        // Or, use Redis SCAN with a pattern if keys are standardized.
        // For now, this is a basic deletion.
        var redisKeyPattern = $"ratelimit:{key}:*"; // Assuming a pattern for keys
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var redisKey in server.KeysAsync(pattern: redisKeyPattern))
        {
            await _db.KeyDeleteAsync(redisKey);
        }
        _logger.LogInformation("Redis rate limits for key {Key} reset.", key);
    }

    public async Task ResetAllAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "ratelimit:*"))
        {
            await _db.KeyDeleteAsync(key);
        }
        _logger.LogInformation("All Redis rate limits reset.");
    }

    private string GetRedisKey(string key, RateLimitPolicy policy)
    {
        // Incorporate policy strategy and Id into the key to avoid collisions
        return $"ratelimit:{key}:{policy.Strategy}:{policy.Id}";
    }

    private async Task<bool> HandleFixedWindowAsync(RedisKey redisKey, RateLimitPolicy policy, DateTime now)
    {
        var windowLengthSeconds = policy.RequestsPerMinute > 0 ? 60 : 3600; // Use 1 min or 1 hr window
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / (windowLengthSeconds / 60) * (windowLengthSeconds / 60), 0, DateTimeKind.Utc);
        var windowEnd = windowStart.AddSeconds(windowLengthSeconds);
        var expiry = (int)(windowEnd - now).TotalSeconds + 1; // Expire key at end of window

        var script = LuaScript.Prepare(
            "local count = redis.call('incr', @key); " +
            "if count == 1 then redis.call('expire', @key, @expiry); end; " +
            "return count;");

        var count = (long)await _db.ScriptEvaluateAsync(script, new { key = redisKey, expiry = expiry });
        // Per-minute windows apply the per-minute limit; hourly windows apply the hourly limit.
        var limit = policy.RequestsPerMinute > 0 ? policy.RequestsPerMinute : policy.RequestsPerHour;

        var allowed = count <= limit;
        if (!allowed)
        {
            _logger.LogWarning("Redis FixedWindow rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}",
                redisKey, count, limit);
        }
        return allowed;
    }

    private async Task<bool> HandleSlidingWindowAsync(RedisKey redisKey, RateLimitPolicy policy, DateTime now)
    {
        var windowDuration = TimeSpan.FromMinutes(1); // Assuming 1-minute window
        var cleanBefore = now - windowDuration;

        // Lua script to:
        // 1. Remove old timestamps from the sorted set
        // 2. Count current elements in the window
        // 3. Add current timestamp
        // 4. Set expiry (optional, good for cleanup)
        var script = LuaScript.Prepare(
            "redis.call('ZREMRANGEBYSCORE', @key, 0, @cleanBefore); " +
            "local count = redis.call('ZCARD', @key); " +
            "if count < @limit then " +
            "    redis.call('ZADD', @key, @now, @now); " +
            "    redis.call('EXPIRE', @key, @expiry); " +
            "    return 1; " + // Allowed
            "else " +
            "    return 0; " + // Not Allowed
            "end;");

        var expirySeconds = (int)windowDuration.TotalSeconds * 2; // Keep for at least 2 window durations
        var limit = policy.RequestsPerMinute;

        var allowed = (long)await _db.ScriptEvaluateAsync(script, new
        {
            key = redisKey,
            cleanBefore = (double)cleanBefore.Ticks,
            now = (double)now.Ticks,
            limit = limit,
            expiry = expirySeconds
        });

        if (allowed == 0)
        {
            _logger.LogWarning("Redis SlidingWindow rate limit exceeded for key {Key}. Limit: {Limit}",
                redisKey, limit);
        }
        return allowed == 1;
    }

    private static double ToUnixSeconds(DateTime utc)
        => (utc - DateTime.UnixEpoch).TotalSeconds;

    private async Task<bool> HandleTokenBucketAsync(RedisKey redisKey, RateLimitPolicy policy, DateTime now)
    {
        var refillRate = policy.RequestsPerMinute / 60.0;
        var bucketCapacity = policy.BurstSize;

        // Hash fields: 'tokens', 'last_refill_time'
        // Lua script to:
        // 1. Get current tokens and last refill time
        // 2. Calculate tokens to add based on elapsed time and refill rate
        // 3. Update tokens (capped by capacity) and last refill time
        // 4. Check if a token can be consumed
        var script = LuaScript.Prepare(
            "local tokens = tonumber(redis.call('HGET', @key, 'tokens')) or @capacity; " +
            "local lastRefillTime = tonumber(redis.call('HGET', @key, 'last_refill_time')) or @now; " +
            "local timeElapsed = @now - lastRefillTime; " +
            "tokens = math.min(tokens + (timeElapsed * @refillRate), @capacity); " +
            "redis.call('HSET', @key, 'last_refill_time', @now); " +
            "if tokens >= 1 then " +
            "    redis.call('HSET', @key, 'tokens', tokens - 1); " +
            "    redis.call('EXPIRE', @key, @expiry); " +
            "    return 1; " + // Allowed
            "else " +
            "    redis.call('HSET', @key, 'tokens', tokens); " + // Update tokens even if not consumed
            "    redis.call('EXPIRE', @key, @expiry); " +
            "    return 0; " + // Not Allowed
            "end;");

        var expirySeconds = policy.RequestsPerMinute > 0 ? (policy.RequestsPerMinute * 60 * 2) : (policy.RequestsPerHour * 3600 * 2); // Keep for a reasonable duration
        if (expirySeconds == 0) expirySeconds = 3600; // Default if both are 0

        var allowed = (long)await _db.ScriptEvaluateAsync(script, new
        {
            key = redisKey,
            capacity = (double)bucketCapacity,
            // The refill rate is tokens per second, so elapsed time must be in seconds
            // (ticks here would over-refill by seven orders of magnitude).
            now = ToUnixSeconds(now),
            refillRate = refillRate,
            expiry = expirySeconds
        });

        if (allowed == 0)
        {
            _logger.LogWarning("Redis TokenBucket rate limit exceeded for key {Key}. Capacity: {Capacity}",
                redisKey, bucketCapacity);
        }
        return allowed == 1;
    }

    public async Task<IEnumerable<RateLimitEntry>> GetAllEntriesAsync()
    {
        var entries = new List<RateLimitEntry>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        // Scan for all rate limit keys
        await foreach (var key in server.KeysAsync(pattern: "ratelimit:*"))
        {
            try
            {
                // Extract key name from the Redis key
                var keyParts = key.ToString().Split(':');
                if (keyParts.Length >= 3)
                {
                    var rateLimitKey = keyParts[1];
                    var strategy = keyParts[2];

                    // For simplicity, we'll create a basic entry with just the key and strategy
                    // A more complete implementation would parse the actual rate limit state
                    entries.Add(new RateLimitEntry
                    {
                        Key = rateLimitKey,
                        Count = 0, // Placeholder - actual count would require additional Redis queries
                        RemainingTimeSeconds = 0,
                        Tokens = 0,
                        LastRequest = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading Redis rate limit entry for key {Key}", key);
            }
        }

        return entries;
    }
}
