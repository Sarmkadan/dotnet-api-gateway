#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for enforcing rate limiting on requests using pluggable storage.
/// </summary>
public sealed class RateLimitingService : IDisposable
{
    private readonly IRateLimitStoreFactory _rateLimitStoreFactory;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IRateLimitStoreFactory rateLimitStoreFactory, ILogger<RateLimitingService> logger)
    {
        _rateLimitStoreFactory = rateLimitStoreFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a request is allowed based on the provided rate limit policy.
    /// </summary>
    /// <param name="key">The unique key for the rate limit (e.g., client IP, user ID).</param>
    /// <param name="policy">The rate limit policy to apply.</param>
    /// <returns>True if the request is allowed, false otherwise.</returns>
    public async Task<bool> IsAllowedAsync(string key, RateLimitPolicy policy)
    {
        if (!policy.IsEnabled())
            return true;

        var store = _rateLimitStoreFactory.GetStore(policy);
        return await store.IsRequestAllowedAsync(key, policy);
    }

    /// <summary>
    /// Retrieves the current rate limit information for a given key and policy.
    /// </summary>
    /// <param name="key">The unique key for the rate limit.</param>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>Rate limit information.</returns>
    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key, RateLimitPolicy policy)
    {
        var store = _rateLimitStoreFactory.GetStore(policy);
        var entry = await store.GetEntryAsync(key, policy);

        // Map the generic RateLimitEntry to RateLimitInfo for external consumption
        int limit = policy.RequestsPerMinute;
        int remaining = policy.RequestsPerMinute - entry.Count;

        if (policy.Strategy == RateLimitStrategy.TokenBucket)
        {
            limit = policy.BurstSize;
            remaining = (int)entry.Tokens;
        }

        return new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            Reset = entry.RemainingTimeSeconds
        };
    }

    /// <summary>
    /// Resets the rate limits for a specific key across all configured stores.
    /// </summary>
    /// <param name="key">The unique key to reset (e.g., client IP, user ID).</param>
    public async Task ResetKeyLimitsAsync(string key)
    {
        foreach (var store in _rateLimitStoreFactory.GetAllStores())
        {
            await store.ResetKeyAsync(key);
        }
        _logger.LogInformation("Rate limits for key {Key} reset across all stores.", key);
    }

    /// <summary>
    /// Resets all rate limit counters across all configured stores.
    /// </summary>
    public async Task ResetAllLimitsAsync()
    {
        foreach (var store in _rateLimitStoreFactory.GetAllStores())
        {
            await store.ResetAllAsync();
        }
        _logger.LogInformation("All rate limits reset across all stores.");
    }

    public void Dispose()
    {
        // Dispose the factory, which will dispose managed Redis stores
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
