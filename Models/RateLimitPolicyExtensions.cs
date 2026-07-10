#nullable enable

namespace DotNetApiGateway.Models;

/// <summary>
/// Extension methods for <see cref="RateLimitPolicy"/> providing convenient operations
/// </summary>
public static class RateLimitPolicyExtensions
{
    /// <summary>
    /// Creates a copy of the current rate limit policy with updated properties
    /// </summary>
    /// <param name="policy">The source rate limit policy</param>
    /// <param name="requestsPerMinute">New requests per minute value</param>
    /// <param name="requestsPerHour">New requests per hour value</param>
    /// <returns>A new RateLimitPolicy instance with updated values</returns>
    public static RateLimitPolicy WithRequestsPerMinute(this RateLimitPolicy policy, int requestsPerMinute)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var copy = new RateLimitPolicy
        {
            Id = policy.Id,
            RequestsPerMinute = requestsPerMinute,
            RequestsPerHour = policy.RequestsPerHour,
            Strategy = policy.Strategy,
            KeyGenerator = policy.KeyGenerator,
            BypassForAuthenticatedUsers = policy.BypassForAuthenticatedUsers,
            BurstSize = policy.BurstSize,
            Enabled = policy.Enabled,
            StorageType = policy.StorageType,
            RedisConnectionString = policy.RedisConnectionString
        };

        return copy;
    }

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated properties
    /// </summary>
    /// <param name="policy">The source rate limit policy</param>
    /// <param name="requestsPerHour">New requests per hour value</param>
    /// <returns>A new RateLimitPolicy instance with updated values</returns>
    public static RateLimitPolicy WithRequestsPerHour(this RateLimitPolicy policy, int requestsPerHour)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var copy = new RateLimitPolicy
        {
            Id = policy.Id,
            RequestsPerMinute = policy.RequestsPerMinute,
            RequestsPerHour = requestsPerHour,
            Strategy = policy.Strategy,
            KeyGenerator = policy.KeyGenerator,
            BypassForAuthenticatedUsers = policy.BypassForAuthenticatedUsers,
            BurstSize = policy.BurstSize,
            Enabled = policy.Enabled,
            StorageType = policy.StorageType,
            RedisConnectionString = policy.RedisConnectionString
        };

        return copy;
    }

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated strategy
    /// </summary>
    /// <param name="policy">The source rate limit policy</param>
    /// <param name="strategy">New rate limiting strategy</param>
    /// <returns>A new RateLimitPolicy instance with updated strategy</returns>
    public static RateLimitPolicy WithStrategy(this RateLimitPolicy policy, RateLimitStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var copy = new RateLimitPolicy
        {
            Id = policy.Id,
            RequestsPerMinute = policy.RequestsPerMinute,
            RequestsPerHour = policy.RequestsPerHour,
            Strategy = strategy,
            KeyGenerator = policy.KeyGenerator,
            BypassForAuthenticatedUsers = policy.BypassForAuthenticatedUsers,
            BurstSize = policy.BurstSize,
            Enabled = policy.Enabled,
            StorageType = policy.StorageType,
            RedisConnectionString = policy.RedisConnectionString
        };

        return copy;
    }

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated storage type
    /// </summary>
    /// <param name="policy">The source rate limit policy</param>
    /// <param name="storageType">New storage type</param>
    /// <param name="redisConnectionString">Optional Redis connection string for distributed storage</param>
    /// <returns>A new RateLimitPolicy instance with updated storage settings</returns>
    public static RateLimitPolicy WithStorage(this RateLimitPolicy policy, RateLimitStorageType storageType, string? redisConnectionString = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var copy = new RateLimitPolicy
        {
            Id = policy.Id,
            RequestsPerMinute = policy.RequestsPerMinute,
            RequestsPerHour = policy.RequestsPerHour,
            Strategy = policy.Strategy,
            KeyGenerator = policy.KeyGenerator,
            BypassForAuthenticatedUsers = policy.BypassForAuthenticatedUsers,
            BurstSize = policy.BurstSize,
            Enabled = policy.Enabled,
            StorageType = storageType,
            RedisConnectionString = redisConnectionString ?? policy.RedisConnectionString
        };

        return copy;
    }
}