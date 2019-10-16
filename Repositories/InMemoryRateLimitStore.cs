#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetApiGateway.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// In-memory implementation of IRateLimitStore, suitable for single-instance deployments.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _storage = new();
    private readonly ILogger<InMemoryRateLimitStore> _logger;

    public InMemoryRateLimitStore(ILogger<InMemoryRateLimitStore> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsRequestAllowedAsync(string key, RateLimitPolicy policy)
    {
        var now = DateTime.UtcNow;
        var entry = _storage.GetOrAdd(key, _ => new RateLimitEntry { Key = key, LastRequest = now });

        lock (entry)
        {
            if (policy.Strategy == RateLimitStrategy.FixedWindow)
            {
                var windowStart = GetFixedWindowStart(now, policy.RequestsPerMinute > 0 ? 60 : 3600); // Use 1 min or 1 hr window
                if (entry.LastRequest < windowStart)
                {
                    entry.Count = 0;
                    entry.LastRequest = now;
                }

                entry.Count++;
                var allowed = entry.Count <= GetLimitForFixedWindow(policy, windowStart);
                if (!allowed)
                {
                    _logger.LogWarning("FixedWindow rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}",
                        key, entry.Count, GetLimitForFixedWindow(policy, windowStart));
                }
                return Task.FromResult(allowed);
            }
            else if (policy.Strategy == RateLimitStrategy.SlidingWindow)
            {
                // This is a simplified sliding window. For true sliding window,
                // each request timestamp needs to be stored and aggregated.
                // For a more robust solution, a distributed store like Redis is recommended.
                // Here, we approximate by checking against the RequestsPerMinute.
                // A true in-memory sliding window would require a different data structure
                // e.g., a Queue of timestamps, and counting those within the window.
                // For simplicity and given the issue context, we'll keep this basic.

                var windowStart = now.AddMinutes(-1);
                if (entry.LastRequest < windowStart)
                {
                    entry.Count = 0; // Reset count if outside 1-minute window
                    entry.LastRequest = now;
                }
                
                entry.Count++;
                var allowed = entry.Count <= policy.RequestsPerMinute;
                if (!allowed)
                {
                    _logger.LogWarning("SlidingWindow rate limit approximated exceeded for key {Key}. Count: {Count}, Limit: {Limit}",
                        key, entry.Count, policy.RequestsPerMinute);
                }
                return Task.FromResult(allowed);
            }
            else // TokenBucket
            {
                var timeElapsed = (now - entry.LastRequest).TotalSeconds;
                entry.Tokens += timeElapsed * (policy.RequestsPerMinute / 60.0); // Refill tokens based on rpm
                entry.Tokens = Math.Min(entry.Tokens, policy.BurstSize); // Cap at burst size

                if (entry.Tokens >= 1)
                {
                    entry.Tokens--;
                    entry.LastRequest = now;
                    return Task.FromResult(true);
                }
                _logger.LogWarning("TokenBucket rate limit exceeded for key {Key}. Tokens: {Tokens}, BurstSize: {BurstSize}",
                    key, entry.Tokens, policy.BurstSize);
                return Task.FromResult(false);
            }
        }
    }

    public Task<RateLimitEntry> GetEntryAsync(string key, RateLimitPolicy policy)
    {
        _storage.TryGetValue(key, out var entry);
        var now = DateTime.UtcNow;

        if (entry is null)
        {
            return Task.FromResult(new RateLimitEntry { Key = key, LastRequest = now, RemainingTimeSeconds = GetPolicyWindowSeconds(policy) });
        }

        lock (entry)
        {
            int remainingTimeSeconds = 0;
            int currentLimit = policy.RequestsPerMinute; // Default for display

            if (policy.Strategy == RateLimitStrategy.FixedWindow)
            {
                var windowLengthSeconds = policy.RequestsPerMinute > 0 ? 60 : 3600;
                var windowStart = GetFixedWindowStart(now, windowLengthSeconds);
                var windowEnd = windowStart.AddSeconds(windowLengthSeconds);
                remainingTimeSeconds = (int)Math.Max(0, (windowEnd - now).TotalSeconds);
                currentLimit = GetLimitForFixedWindow(policy, windowStart);

                // Update entry's count if window expired
                if (entry.LastRequest < windowStart)
                {
                    entry.Count = 0;
                }
            }
            else if (policy.Strategy == RateLimitStrategy.SlidingWindow)
            {
                var windowDuration = TimeSpan.FromMinutes(1); // Assumed 1-minute window for simplified sliding window
                remainingTimeSeconds = (int)Math.Max(0, (windowDuration - (now - entry.LastRequest)).TotalSeconds);
            }
            else // TokenBucket
            {
                // Refill tokens for display purposes (not affecting actual IsRequestAllowedAsync logic)
                var timeElapsed = (now - entry.LastRequest).TotalSeconds;
                entry.Tokens = Math.Min(entry.Tokens + (timeElapsed * (policy.RequestsPerMinute / 60.0)), policy.BurstSize);

                if (entry.Tokens >= 1)
                {
                    remainingTimeSeconds = 1; // Indicate tokens are available
                }
                else
                {
                    remainingTimeSeconds = (int)Math.Ceiling(1 / (policy.RequestsPerMinute / 60.0)); // Time until next token
                }
                currentLimit = policy.BurstSize;
            }

            return Task.FromResult(new RateLimitEntry
            {
                Key = key,
                Count = entry.Count,
                LastRequest = entry.LastRequest,
                Tokens = entry.Tokens,
                RemainingTimeSeconds = remainingTimeSeconds
            });
        }
    }

    private int GetPolicyWindowSeconds(RateLimitPolicy policy)
    {
        if (policy.RequestsPerMinute > 0) return 60;
        if (policy.RequestsPerHour > 0) return 3600;
        return 0;
    }

    public Task ResetKeyAsync(string key)
    {
        _storage.TryRemove(key, out _);
        _logger.LogInformation("Rate limit for key {Key} reset in-memory.", key);
        return Task.CompletedTask;
    }

    public Task ResetAllAsync()
    {
        _storage.Clear();
        _logger.LogInformation("All in-memory rate limits reset.");
        return Task.CompletedTask;
    }

    private DateTime GetFixedWindowStart(DateTime now, int windowSeconds)
    {
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / (windowSeconds / 60) * (windowSeconds / 60), 0, DateTimeKind.Utc);
    }

    private int GetLimitForFixedWindow(RateLimitPolicy policy, DateTime windowStart)
    {
        // Decide whether to use RequestsPerMinute or RequestsPerHour based on current window.
        // This is a simplification; a more robust fixed window would align to full minutes/hours for its window.
        // For per-minute, align to minute. For per-hour, align to hour.
        if ((DateTime.UtcNow - windowStart).TotalMinutes < 60) // If within the current hour, apply minute limit first
        {
            return policy.RequestsPerMinute;
        }
        return policy.RequestsPerHour;
    }
}
