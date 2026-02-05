#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.BackgroundServices;

using DotNetApiGateway.Services;

/// <summary>
/// Background service that periodically cleans up expired cache entries.
/// Prevents memory leaks from accumulating stale cached responses.
/// </summary>
public class CacheCleanupWorker : BackgroundService
{
    private readonly ILogger<CacheCleanupWorker> _logger;
    private readonly CacheService _cacheService;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    public CacheCleanupWorker(
        ILogger<CacheCleanupWorker> logger,
        CacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Execute cache cleanup on scheduled interval.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache cleanup worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Cache cleanup worker stopped");
    }

    /// <summary>
    /// Remove expired entries from cache.
    /// </summary>
    private async Task PerformCleanupAsync()
    {
        var startTime = DateTime.UtcNow;
        var cleanupCount = await _cacheService.RemoveExpiredEntriesAsync();

        if (cleanupCount > 0)
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Cache cleanup completed: {CleanupCount} entries removed in {ElapsedMs:F2}ms",
                cleanupCount,
                elapsedMs);
        }
        else
        {
            _logger.LogDebug("Cache cleanup: No expired entries found");
        }
    }
}
