#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.RateLimiting;
using DotNetApiGateway.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Services;

/// <summary>
/// Factory for creating System.Threading.RateLimiting rate limiters based on rate limit policies.
/// </summary>
public static class BuiltInRateLimiter
{
    private static readonly Dictionary<string, RateLimiter> _activeLimiters = new();
    private static readonly object _limitersLock = new();

    /// <summary>
    /// Gets or creates a partitioned rate limiter for the given key.
    /// </summary>
    /// <param name="key">The partition key (e.g., routeId_clientId).</param>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>A rate limiter instance.</returns>
    public static RateLimiter GetPartitionedRateLimiter(string key, RateLimitPolicy policy, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.Enabled || !policy.IsEnabled())
        {
            // Return a simple rate limiter that allows all requests
            return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = int.MaxValue,
                Window = TimeSpan.FromHours(1),
                AutoReplenishment = true
            });
        }

        try
        {
            lock (_limitersLock)
            {
                if (_activeLimiters.TryGetValue(key, out var existingLimiter))
                {
                    return existingLimiter;
                }

                var newLimiter = CreateRateLimiterForPolicy(policy);
                _activeLimiters[key] = newLimiter;
                return newLimiter;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create partitioned rate limiter for key {Key}. Using permissive limiter.", key);
            return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = int.MaxValue,
                Window = TimeSpan.FromHours(1),
                AutoReplenishment = true
            });
        }
    }

    /// <summary>
    /// Removes a rate limiter from the cache.
    /// </summary>
    /// <param name="key">The limiter key.</param>
    public static void RemoveRateLimiter(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_limitersLock)
        {
            if (_activeLimiters.TryGetValue(key, out var limiter))
            {
                try
                {
                    limiter.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _activeLimiters.Remove(key);
            }
        }
    }

    /// <summary>
    /// Removes all rate limiters from the cache.
    /// </summary>
    public static void ClearAllRateLimiters()
    {
        lock (_limitersLock)
        {
            foreach (var limiter in _activeLimiters.Values)
            {
                try
                {
                    limiter.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _activeLimiters.Clear();
        }
    }

    private static RateLimiter CreateRateLimiterForPolicy(RateLimitPolicy policy)
    {
        return policy.Strategy switch
        {
            RateLimitStrategy.FixedWindow => CreateFixedWindowRateLimiter(policy),
            RateLimitStrategy.SlidingWindow => CreateSlidingWindowRateLimiter(policy),
            RateLimitStrategy.TokenBucket => CreateTokenBucketRateLimiter(policy),
            _ => CreateFixedWindowRateLimiter(policy) // Default to fixed window
        };
    }

    private static FixedWindowRateLimiter CreateFixedWindowRateLimiter(RateLimitPolicy policy)
    {
        var window = policy.RequestsPerMinute > 0
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromHours(1);

        var options = new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = policy.RequestsPerMinute > 0
                ? policy.RequestsPerMinute
                : policy.RequestsPerHour,
            Window = window
        };

        return new FixedWindowRateLimiter(options);
    }

    private static SlidingWindowRateLimiter CreateSlidingWindowRateLimiter(RateLimitPolicy policy)
    {
        var window = TimeSpan.FromMinutes(1);
        var options = new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = policy.RequestsPerMinute > 0
                ? policy.RequestsPerMinute
                : policy.RequestsPerHour,
            Window = window,
            SegmentsPerWindow = 1 // Single segment for simplicity
        };

        return new SlidingWindowRateLimiter(options);
    }

    private static TokenBucketRateLimiter CreateTokenBucketRateLimiter(RateLimitPolicy policy)
    {
        var options = new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            TokenLimit = policy.BurstSize,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(60.0 / policy.RequestsPerMinute),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        };

        return new TokenBucketRateLimiter(options);
    }
}

/// <summary>
/// Extension methods for RateLimitLease to extract metadata.
/// </summary>
public static class RateLimitLeaseExtensions
{
    /// <summary>
    /// Gets the rate limit metadata from a lease.
    /// </summary>
    /// <param name="lease">The rate limit lease.</param>
    /// <returns>Rate limit metadata or null if not available.</returns>
    public static RateLimitMetadata? GetMetadata(this RateLimitLease lease)
    {
        if (lease is null || !lease.IsAcquired)
        {
            return null;
        }

        var metadata = new RateLimitMetadata
        {
            Limit = GetMetadataValue<int>(lease, "RateLimit.Limit"),
            Remaining = GetMetadataValue<int>(lease, "RateLimit.Remaining"),
            RetryAfter = GetMetadataValue<TimeSpan?>(lease, "RateLimit.RetryAfter") ?? TimeSpan.Zero
        };

        return metadata;
    }

    private static T? GetMetadataValue<T>(RateLimitLease lease, string metadataName)
    {
        if (lease.TryGetMetadata(metadataName, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// Contains metadata about a rate limit.
/// </summary>
public sealed class RateLimitMetadata
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public TimeSpan RetryAfter { get; set; }
}