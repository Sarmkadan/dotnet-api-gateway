#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines rate limiting rules for a route
/// </summary>
public sealed class RateLimitPolicy
{
    /// <summary>
    /// Unique identifier of the policy. Defaults to a new GUID string.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Maximum number of requests allowed within a one-minute window. Must be at least 1 when validated.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 1000;

    /// <summary>
    /// Maximum number of requests allowed within a one-hour window. Must be at least <see cref="RequestsPerMinute"/> when validated.
    /// </summary>
    public int RequestsPerHour { get; set; } = 50000;

    /// <summary>
    /// Algorithm used to enforce the limits. Defaults to <see cref="RateLimitStrategy.TokenBucket"/>.
    /// </summary>
    public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.TokenBucket;

    /// <summary>
    /// Identifies the key used to group requests for limiting (for example, "ClientIp"). Defaults to "ClientIp".
    /// </summary>
    public string KeyGenerator { get; set; } = "ClientIp";

    /// <summary>
    /// When <c>true</c>, authenticated users are exempt from rate limiting. Defaults to <c>false</c>.
    /// </summary>
    public bool BypassForAuthenticatedUsers { get; set; } = false;

    /// <summary>
    /// Maximum number of requests permitted in a single burst. Must be between 1 and <see cref="RequestsPerMinute"/> when validated.
    /// </summary>
    public int BurstSize { get; set; } = 10;

    /// <summary>
    /// Whether this policy is active. Disabled policies are not enforced. Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Backing store for rate limit counters. Defaults to <see cref="RateLimitStorageType.InMemory"/>.
    /// </summary>
    public RateLimitStorageType StorageType { get; set; } = RateLimitStorageType.InMemory;

    /// <summary>
    /// Redis connection string used when <see cref="StorageType"/> is Redis. May contain credentials; never log it.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Checks that the policy limits are internally consistent.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="RequestsPerMinute"/> or <see cref="RequestsPerHour"/> is less than 1,
    /// when <see cref="RequestsPerHour"/> is less than <see cref="RequestsPerMinute"/>,
    /// or when <see cref="BurstSize"/> is outside the range 1 to <see cref="RequestsPerMinute"/>.
    /// </exception>
    public void Validate()
    {
        if (RequestsPerMinute < 1)
            throw new ArgumentException("RequestsPerMinute must be at least 1");

        if (RequestsPerHour < 1)
            throw new ArgumentException("RequestsPerHour must be at least 1");

        if (RequestsPerHour < RequestsPerMinute)
            throw new ArgumentException("RequestsPerHour must be >= RequestsPerMinute");

        if (BurstSize < 1 || BurstSize > RequestsPerMinute)
            throw new ArgumentException("BurstSize must be between 1 and RequestsPerMinute");
    }

    public int GetLimitForWindow(string windowType)
    {
        return windowType.ToLower() switch
        {
            "minute" => RequestsPerMinute,
            "hour" => RequestsPerHour,
            _ => RequestsPerMinute
        };
    }

    public bool IsEnabled()
    {
        return Enabled && (RequestsPerMinute > 0 || RequestsPerHour > 0);
    }

    // RedisConnectionString is intentionally omitted: it may contain credentials and must not leak into logs.
    public override string ToString()
    {
        return $"RateLimitPolicy {{ Id={Id}, RequestsPerMinute={RequestsPerMinute}, RequestsPerHour={RequestsPerHour}, " +
               $"Strategy={Strategy}, KeyGenerator={KeyGenerator}, BypassForAuthenticatedUsers={BypassForAuthenticatedUsers}, " +
               $"BurstSize={BurstSize}, Enabled={Enabled}, StorageType={StorageType} }}";
    }
}
