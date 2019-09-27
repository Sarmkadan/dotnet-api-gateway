#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.BackgroundServices;

using DotNetApiGateway.Services;

/// <summary>
/// Background service that periodically exports and aggregates metrics.
/// Logs performance summaries and identifies performance issues.
/// </summary>
public class MetricsExportWorker : BackgroundService
{
    private readonly ILogger<MetricsExportWorker> _logger;
    private readonly MetricsService _metricsService;
    private readonly TimeSpan _exportInterval = TimeSpan.FromMinutes(5);
    private long _lastTotalRequests = 0;

    public MetricsExportWorker(
        ILogger<MetricsExportWorker> logger,
        MetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Export metrics on a scheduled interval.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics export worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExportMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting metrics");
            }

            await Task.Delay(_exportInterval, stoppingToken);
        }

        _logger.LogInformation("Metrics export worker stopped");
    }

    /// <summary>
    /// Export current metrics summary to logs.
    /// </summary>
    private async Task ExportMetricsAsync()
    {
        var totalRequests = await _metricsService.GetTotalRequestCountAsync();
        var successfulRequests = await _metricsService.GetSuccessfulRequestCountAsync();
        var failedRequests = await _metricsService.GetFailedRequestCountAsync();
        var avgResponseTime = await _metricsService.GetAverageResponseTimeAsync();

        var requestsSinceLastExport = totalRequests - _lastTotalRequests;
        var successRate = totalRequests > 0 ? (successfulRequests * 100.0) / totalRequests : 0;

        _logger.LogInformation(
            "Metrics export - Total: {Total}, Requests since last export: {RequestsSinceLastExport}, " +
            "Success rate: {SuccessRate:F2}%, Avg response time: {AvgResponseTime:F2}ms, " +
            "Failed: {Failed}",
            totalRequests,
            requestsSinceLastExport,
            successRate,
            avgResponseTime,
            failedRequests);

        _lastTotalRequests = totalRequests;
    }
}
