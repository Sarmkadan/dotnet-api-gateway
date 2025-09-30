// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Tracks rate limit counters for a client within a time window
/// </summary>
public class RateLimitEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClientId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
    public long RequestCountPerMinute { get; set; } = 0;
    public long RequestCountPerHour { get; set; } = 0;
    public DateTime MinuteWindowStart { get; set; } = DateTime.UtcNow;
    public DateTime HourWindowStart { get; set; } = DateTime.UtcNow;
    public DateTime LastRequestAt { get; set; } = DateTime.UtcNow;
    public int TokensAvailable { get; set; } = 0;
    public DateTime LastTokenRefillAt { get; set; } = DateTime.UtcNow;

    public void IncrementMinuteCounter()
    {
        RequestCountPerMinute++;
        LastRequestAt = DateTime.UtcNow;
    }

    public void IncrementHourCounter()
    {
        RequestCountPerHour++;
    }

    public void ResetMinuteWindow()
    {
        RequestCountPerMinute = 0;
        MinuteWindowStart = DateTime.UtcNow;
    }

    public void ResetHourWindow()
    {
        RequestCountPerHour = 0;
        HourWindowStart = DateTime.UtcNow;
    }

    public bool IsMinuteWindowExpired(int windowMinutes = 1)
    {
        return DateTime.UtcNow - MinuteWindowStart > TimeSpan.FromMinutes(windowMinutes);
    }

    public bool IsHourWindowExpired()
    {
        return DateTime.UtcNow - HourWindowStart > TimeSpan.FromHours(1);
    }

    public TimeSpan GetMinuteWindowTimeRemaining()
    {
        var remaining = TimeSpan.FromMinutes(1) - (DateTime.UtcNow - MinuteWindowStart);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public TimeSpan GetHourWindowTimeRemaining()
    {
        var remaining = TimeSpan.FromHours(1) - (DateTime.UtcNow - HourWindowStart);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public int GetMinuteWindowSecondsRemaining()
    {
        return (int)Math.Ceiling(GetMinuteWindowTimeRemaining().TotalSeconds);
    }

    public int GetHourWindowSecondsRemaining()
    {
        return (int)Math.Ceiling(GetHourWindowTimeRemaining().TotalSeconds);
    }
}
