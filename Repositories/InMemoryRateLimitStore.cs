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
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _slidingWindows = new();
    private readonly ILogger<InMemoryRateLimitStore> _logger;

    public InMemoryRateLimitStore(ILogger<InMemoryRateLimitStore> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsRequestAllowedAsync(string key, RateLimitPolicy policy)
    {
        var now = DateTime.UtcNow;
        // A token bucket must start full, otherwise the very first request is denied.
        var entry = _storage.GetOrAdd(key, _ => new RateLimitEntry { Key = key, LastRequest = now, Tokens = policy.BurstSize });

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
                var allowed = entry.Count <= GetLimitForFixedWindow(policy);
                if (!allowed)
                {
                    _logger.LogWarning("FixedWindow rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}",
                        key, entry.Count, GetLimitForFixedWindow(policy));
                }
                return Task.FromResult(allowed);
            }
            else if (policy.Strategy == RateLimitStrategy.SlidingWindow)
            {
                // True sliding window: track individual request timestamps and count
                // only those inside the trailing one-minute window.
                var window = _slidingWindows.GetOrAdd(key, _ => new Queue<DateTime>());
                var windowStart = now.AddMinutes(-1);

                while (window.Count > 0 && window.Peek() < windowStart)
                    window.Dequeue();

                var allowed = window.Count < policy.RequestsPerMinute;
                if (allowed)
                {
                    window.Enqueue(now);
                }
                else
                {
                    _logger.LogWarning("SlidingWindow rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}",
                        key, window.Count, policy.RequestsPerMinute);
                }

                entry.Count = window.Count;
                entry.LastRequest = now;
                return Task.FromResult(allowed);
            }
            else // TokenBucket
            {
                var timeElapsed = (now - entry.LastRequest).TotalSeconds;
                entry.Tokens += timeElapsed * (policy.RequestsPerMinute / 60.0); // Refill tokens based on rpm
                entry.Tokens = Math.Min(entry.Tokens, policy.BurstSize); // Cap at burst size
                // Advance the refill anchor even when the request is denied; otherwise
                // repeated denied requests re-credit the same elapsed interval.
                entry.LastRequest = now;

                if (entry.Tokens >= 1)
                {
                    entry.Tokens--;
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

            if (policy.Strategy == RateLimitStrategy.FixedWindow)
            {
                var windowLengthSeconds = policy.RequestsPerMinute > 0 ? 60 : 3600;
                var windowStart = GetFixedWindowStart(now, windowLengthSeconds);
                var windowEnd = windowStart.AddSeconds(windowLengthSeconds);
                remainingTimeSeconds = (int)Math.Max(0, (windowEnd - now).TotalSeconds);

                // Update entry's count if window expired
                if (entry.LastRequest < windowStart)
                {
                    entry.Count = 0;
                }
            }
            else if (policy.Strategy == RateLimitStrategy.SlidingWindow)
            {
                // Time until the oldest tracked request leaves the trailing one-minute window.
                if (_slidingWindows.TryGetValue(key, out var window))
                {
                    var windowStart = now.AddMinutes(-1);
                    while (window.Count > 0 && window.Peek() < windowStart)
                        window.Dequeue();

                    entry.Count = window.Count;
                    remainingTimeSeconds = window.Count > 0
                        ? (int)Math.Max(0, (window.Peek().AddMinutes(1) - now).TotalSeconds)
                        : 0;
                }
            }
            var projectedTokens = entry.Tokens;
            if (policy.Strategy == RateLimitStrategy.TokenBucket)
            {
                // Project the refilled token count for display without mutating the entry,
                // so the read path does not interfere with IsRequestAllowedAsync refill accounting.
                var timeElapsed = (now - entry.LastRequest).TotalSeconds;
                projectedTokens = Math.Min(entry.Tokens + (timeElapsed * (policy.RequestsPerMinute / 60.0)), policy.BurstSize);

                if (projectedTokens >= 1)
                {
                    remainingTimeSeconds = 1; // Indicate tokens are available
                }
                else
                {
                    remainingTimeSeconds = policy.RequestsPerMinute > 0
                        ? (int)Math.Ceiling(1 / (policy.RequestsPerMinute / 60.0)) // Time until next token
                        : GetPolicyWindowSeconds(policy);
                }
            }

            return Task.FromResult(new RateLimitEntry
            {
                Key = key,
                Count = entry.Count,
                LastRequest = entry.LastRequest,
                Tokens = projectedTokens,
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
        _slidingWindows.TryRemove(key, out _);
        _logger.LogInformation("Rate limit for key {Key} reset in-memory.", key);
        return Task.CompletedTask;
    }

    public Task ResetAllAsync()
    {
        _storage.Clear();
        _slidingWindows.Clear();
        _logger.LogInformation("All in-memory rate limits reset.");
        return Task.CompletedTask;
    }

    private DateTime GetFixedWindowStart(DateTime now, int windowSeconds)
    {
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / (windowSeconds / 60) * (windowSeconds / 60), 0, DateTimeKind.Utc);
    }

    private static int GetLimitForFixedWindow(RateLimitPolicy policy)
    {
        // The window length is chosen from the configured limits: a per-minute limit
        // uses a one-minute window, otherwise the per-hour limit applies.
        return policy.RequestsPerMinute > 0 ? policy.RequestsPerMinute : policy.RequestsPerHour;
    }
}
