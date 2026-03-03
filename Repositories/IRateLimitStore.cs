#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Defines the contract for storing and managing rate limit counters.
/// Implementations can provide in-memory, Redis-backed, or other storage solutions.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Checks if a request is allowed and increments the counter for the given key and policy.
    /// </summary>
    /// <param name="key">The unique key for the rate limit (e.g., IP address, user ID).</param>
    /// <param name="policy">The rate limit policy to apply.</param>
    /// <returns>True if the request is allowed, false otherwise.</returns>
    Task<bool> IsRequestAllowedAsync(string key, RateLimitPolicy policy);

    /// <summary>
    /// Retrieves the current rate limit status for a given key and policy.
    /// </summary>
    /// <param name="key">The unique key for the rate limit.</param>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>The current RateLimitEntry.</returns>
    Task<RateLimitEntry> GetEntryAsync(string key, RateLimitPolicy policy);

    /// <summary>
    /// Resets the rate limit counter for a specific key.
    /// </summary>
    /// <param name="key">The unique key for the rate limit.</param>
    Task ResetKeyAsync(string key);

    /// <summary>
    /// Resets all rate limit counters.
    /// </summary>
    Task ResetAllAsync();
}
