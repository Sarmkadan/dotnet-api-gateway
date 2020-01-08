#nullable enable

namespace DotNetApiGateway.Models;

/// <summary>
/// Extension methods for <see cref="RateLimitPolicy"/> providing convenient fluent-style operations
/// for creating modified copies of rate limit policies.
/// </summary>
public static class RateLimitPolicyExtensions
{
    /// <summary>
    /// Creates a copy of the current rate limit policy with updated requests per minute value.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <param name="requestsPerMinute">New requests per minute value to set.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with the updated <see cref="RateLimitPolicy.RequestsPerMinute"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithRequestsPerMinute(this RateLimitPolicy policy, int requestsPerMinute)
        => policy.WithProperty(p => p.RequestsPerMinute = requestsPerMinute);

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated requests per hour value.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <param name="requestsPerHour">New requests per hour value to set.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with the updated <see cref="RateLimitPolicy.RequestsPerHour"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithRequestsPerHour(this RateLimitPolicy policy, int requestsPerHour)
        => policy.WithProperty(p => p.RequestsPerHour = requestsPerHour);

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated rate limiting strategy.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <param name="strategy">New <see cref="RateLimitStrategy"/> to use.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with the updated <see cref="RateLimitPolicy.Strategy"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithStrategy(this RateLimitPolicy policy, RateLimitStrategy strategy)
        => policy.WithProperty(p => p.Strategy = strategy);

    /// <summary>
    /// Creates a copy of the current rate limit policy with updated storage settings.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <param name="storageType">New <see cref="RateLimitStorageType"/> to use.</param>
    /// <param name="redisConnectionString">Optional Redis connection string for distributed storage.
    /// If <see langword="null"/>, preserves the existing <see cref="RateLimitPolicy.RedisConnectionString"/>.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with the updated storage settings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithStorage(this RateLimitPolicy policy, RateLimitStorageType storageType, string? redisConnectionString = null)
        => policy.WithProperty(p => {
            p.StorageType = storageType;
            p.RedisConnectionString = redisConnectionString ?? p.RedisConnectionString;
        });

    /// <summary>
    /// Creates a copy of the current rate limit policy with the enabled state set to <see langword="true"/>.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with <see cref="RateLimitPolicy.Enabled"/> set to <see langword="true"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithEnabled(this RateLimitPolicy policy)
        => policy.WithProperty(p => p.Enabled = true);

    /// <summary>
    /// Creates a copy of the current rate limit policy with the enabled state set to <see langword="false"/>.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy and modify.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with <see cref="RateLimitPolicy.Enabled"/> set to <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <see langword="null"/>.</exception>
    public static RateLimitPolicy WithDisabled(this RateLimitPolicy policy)
        => policy.WithProperty(p => p.Enabled = false);

    /// <summary>
    /// Helper method to create a copy of a rate limit policy with property modifications.
    /// </summary>
    /// <param name="policy">The source rate limit policy to copy.</param>
    /// <param name="setter">Action that modifies the properties of the copied policy.</param>
    /// <returns>A new <see cref="RateLimitPolicy"/> instance with the specified properties updated.</returns>
    private static RateLimitPolicy WithProperty(this RateLimitPolicy policy, Action<RateLimitPolicy> setter)
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
            StorageType = policy.StorageType,
            RedisConnectionString = policy.RedisConnectionString
        };

        setter(copy);
        return copy;
    }
}