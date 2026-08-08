#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.RateLimiting;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for enforcing rate limiting on requests using System.Threading.RateLimiting
/// with partitioned rate limiters for better performance and scalability.
/// </summary>
public sealed class RateLimitingService : IDisposable
{
    private readonly IRateLimitStoreFactory? _rateLimitStoreFactory; // Keep for backward compatibility
    private readonly ILogger<RateLimitingService> _logger;
    private readonly Dictionary<string, (RateLimiter Limiter, DateTime CreatedAt)> _activeLimiters = new();
    private readonly object _limitersLock = new();
    private bool _disposed;

    public RateLimitingService(IRateLimitStoreFactory rateLimitStoreFactory, ILogger<RateLimitingService> logger)
    {
        ArgumentNullException.ThrowIfNull(rateLimitStoreFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _rateLimitStoreFactory = rateLimitStoreFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a request is allowed based on the provided rate limit policy.
    /// Uses System.Threading.RateLimiting with partitioned rate limiters.
    /// </summary>
    /// <param name="key">The unique key for the rate limit (e.g., client IP, user ID).</param>
    /// <param name="policy">The rate limit policy to apply.</param>
    /// <returns>True if the request is allowed, false otherwise.</returns>
    public async Task<bool> IsAllowedAsync(string key, RateLimitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.Enabled || !policy.IsEnabled())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Rate limit key cannot be null or empty. Bypassing rate limit.");
            return true;
        }

        try
        {
            // Create a partitioned rate limiter key based on route and client
            var limiterKey = $"{key}_{policy.Id}";
            var rateLimiter = GetOrCreateRateLimiter(limiterKey, policy);

            var lease = await rateLimiter.AcquireAsync(1);
            return lease.IsAcquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring rate limit lease for key {Key}", key);
            return true; // Allow request on error to prevent blocking legitimate traffic
        }
    }

    /// <summary>
    /// Retrieves the current rate limit information for a given key and policy.
    /// </summary>
    /// <param name="key">The unique key for the rate limit.</param>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>Rate limit information.</returns>
    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key, RateLimitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(key) || !policy.Enabled || !policy.IsEnabled())
        {
            return new RateLimitInfo
            {
                Limit = 0,
                Remaining = 0,
                Reset = 0
            };
        }

        try
        {
            var limiterKey = $"{key}_{policy.Id}";
            var rateLimiter = GetOrCreateRateLimiter(limiterKey, policy);

            var lease = await rateLimiter.AcquireAsync(1);
            var metadata = lease.GetMetadata();

            // Calculate remaining based on metadata or policy
            int remaining = metadata?.Remaining ?? GetLimitForPolicy(policy);
            int limit = metadata?.Limit ?? GetLimitForPolicy(policy);
            int reset = metadata?.RetryAfter.Seconds ?? 60;

            return new RateLimitInfo
            {
                Limit = limit,
                Remaining = remaining,
                Reset = reset
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit info for key {Key}", key);
            return new RateLimitInfo
            {
                Limit = GetLimitForPolicy(policy),
                Remaining = GetLimitForPolicy(policy),
                Reset = 60
            };
        }
    }

    private int GetLimitForPolicy(RateLimitPolicy policy)
    {
        return policy.Strategy == RateLimitStrategy.TokenBucket
            ? policy.BurstSize
            : (policy.RequestsPerMinute > 0 ? policy.RequestsPerMinute : policy.RequestsPerHour);
    }

    private RateLimiter GetOrCreateRateLimiter(string limiterKey, RateLimitPolicy policy)
    {
        lock (_limitersLock)
        {
            // Clean up old limiters (older than 1 hour)
            var now = DateTime.UtcNow;
            foreach (var kvp in _activeLimiters.ToList())
            {
                if ((now - kvp.Value.CreatedAt).TotalHours > 1)
                {
                    try
                    {
                        kvp.Value.Limiter.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                    _activeLimiters.Remove(kvp.Key);
                }
            }

            if (_activeLimiters.TryGetValue(limiterKey, out var existingLimiter))
            {
                return existingLimiter.Limiter;
            }

            var newLimiter = BuiltInRateLimiter.GetPartitionedRateLimiter(limiterKey, policy, _logger);
            _activeLimiters[limiterKey] = (newLimiter, DateTime.UtcNow);

            return newLimiter;
        }
    }

    /// <summary>
    /// Resets the rate limits for a specific key across all configured stores.
    /// Note: With System.Threading.RateLimiting, individual key resets are not directly supported.
    /// This method maintains backward compatibility.
    /// </summary>
    /// <param name="key">The unique key to reset (e.g., client IP, user ID).</param>
    public async Task ResetKeyLimitsAsync(string key)
    {
        if (_rateLimitStoreFactory != null)
        {
            foreach (var store in _rateLimitStoreFactory.GetAllStores())
            {
                await store.ResetKeyAsync(key);
            }
        }

        _logger.LogInformation("Rate limits for key {Key} reset across all stores.", key);
    }

    /// <summary>
    /// Resets all rate limit counters across all configured stores.
    /// Note: With System.Threading.RateLimiting, all limiters are disposed and recreated.
    /// This method maintains backward compatibility.
    /// </summary>
    public async Task ResetAllLimitsAsync()
    {
        if (_rateLimitStoreFactory != null)
        {
            foreach (var store in _rateLimitStoreFactory.GetAllStores())
            {
                await store.ResetAllAsync();
            }
        }

        // Dispose all active limiters
        lock (_limitersLock)
        {
            foreach (var limiter in _activeLimiters.Values.Select(x => x.Limiter))
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

        _logger.LogInformation("All rate limits reset across all stores.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose all active limiters
        lock (_limitersLock)
        {
            foreach (var limiter in _activeLimiters.Values.Select(x => x.Limiter))
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

        // Dispose the factory
        (_rateLimitStoreFactory as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Provides a snapshot of current rate limit status for external display.
/// </summary>
public sealed class RateLimitInfo
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public int Reset { get; set; }
}