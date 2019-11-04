#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for enforcing rate limiting on requests
/// </summary>
public sealed class RateLimitingService : IDisposable
{
    private readonly RateLimitRepository _rateLimitRepository;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly Timer _cleanupTimer;

    public RateLimitingService(RateLimitRepository rateLimitRepository, ILogger<RateLimitingService> logger)
    {
        _rateLimitRepository = rateLimitRepository;
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public async Task<bool> IsAllowedAsync(string clientId, string routeId, RateLimitPolicy policy)
    {
        if (!policy.IsEnabled())
            return true;

        var entry = await _rateLimitRepository.GetByClientAndRouteAsync(clientId, routeId);

        if (entry is null)
        {
            entry = new RateLimitEntry
            {
                ClientId = clientId,
                RouteId = routeId,
                RequestCountPerMinute = 1,
                RequestCountPerHour = 1,
                TokensAvailable = policy.BurstSize
            };
            await _rateLimitRepository.AddAsync(entry);
            return true;
        }

        // Check and reset windows if expired
        if (entry.IsMinuteWindowExpired())
            entry.ResetMinuteWindow();

        if (entry.IsHourWindowExpired())
            entry.ResetHourWindow();

        // Check minute limit
        if (entry.RequestCountPerMinute >= policy.RequestsPerMinute)
            return false;

        // Check hour limit
        if (entry.RequestCountPerHour >= policy.RequestsPerHour)
            return false;

        // Increment counters
        entry.IncrementMinuteCounter();
        entry.IncrementHourCounter();
        await _rateLimitRepository.UpdateAsync(entry);

        return true;
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId, string routeId, RateLimitPolicy policy)
    {
        var entry = await _rateLimitRepository.GetByClientAndRouteAsync(clientId, routeId);

        if (entry is null)
        {
            return new RateLimitInfo
            {
                Limit = policy.RequestsPerMinute,
                Remaining = policy.RequestsPerMinute,
                Reset = (int)TimeSpan.FromMinutes(1).TotalSeconds
            };
        }

        // Refresh windows if expired
        if (entry.IsMinuteWindowExpired())
            entry.ResetMinuteWindow();

        var remaining = Math.Max(0, policy.RequestsPerMinute - (int)entry.RequestCountPerMinute);

        return new RateLimitInfo
        {
            Limit = policy.RequestsPerMinute,
            Remaining = remaining,
            Reset = (int)entry.GetMinuteWindowSecondsRemaining()
        };
    }

    public async Task ResetClientLimitAsync(string clientId)
    {
        var entries = await _rateLimitRepository.GetByClientAsync(clientId);
        foreach (var entry in entries)
        {
            entry.ResetMinuteWindow();
            entry.ResetHourWindow();
            await _rateLimitRepository.UpdateAsync(entry);
        }
    }

    private async void CleanupExpiredEntries(object? state)
    {
        try
        {
            await _rateLimitRepository.CleanupExpiredEntriesAsync();
        }
        catch (ObjectDisposedException)
        {
            // Timer fired after disposal - ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired rate limit entries");
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}

public sealed class RateLimitInfo
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public int Reset { get; set; }
}
