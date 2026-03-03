#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents the current state of a rate limit for a specific key.
/// </summary>
public sealed class RateLimitEntry
{
    /// <summary>
    /// The unique key for which the rate limit is being tracked (e.g., client IP, user ID).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The current count of requests within the active rate limit window.
    /// </summary>
    public int Count { get; set; } = 0;

    /// <summary>
    /// The number of seconds remaining until the rate limit window resets.
    /// </summary>
    public int RemainingTimeSeconds { get; set; } = 0;

    /// <summary>
    /// The current number of tokens available in a token bucket rate limiting strategy.
    /// </summary>
    public double Tokens { get; set; } = 0;

    /// <summary>
    /// The timestamp of the last recorded request or token refill.
    /// </summary>
    public DateTime LastRequest { get; set; } = DateTime.UtcNow;
}
