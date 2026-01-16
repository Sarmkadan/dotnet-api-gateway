// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;

/// <summary>
/// Service for advanced analytics and insights about gateway operations.
/// Provides detailed reporting on performance, usage, and error patterns.
/// </summary>
public class AnalyticsService
{
    private readonly MetricsService _metricsService;
    private readonly GatewayRouteRepository _routeRepository;
    private readonly RateLimitRepository _rateLimitRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        MetricsService metricsService,
        GatewayRouteRepository routeRepository,
        RateLimitRepository rateLimitRepository,
        ILogger<AnalyticsService> logger)
    {
        _metricsService = metricsService;
        _routeRepository = routeRepository;
        _rateLimitRepository = rateLimitRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive gateway health report.
    /// </summary>
    public async Task<GatewayHealthReport> GetHealthReportAsync()
    {
        var totalRequests = await _metricsService.GetTotalRequestCountAsync();
        var successfulRequests = await _metricsService.GetSuccessfulRequestCountAsync();
        var failedRequests = await _metricsService.GetFailedRequestCountAsync();
        var avgResponseTime = await _metricsService.GetAverageResponseTimeAsync();

        var successRate = totalRequests > 0 ? (successfulRequests * 100.0) / totalRequests : 0;
        var errorRate = totalRequests > 0 ? (failedRequests * 100.0) / totalRequests : 0;

        // Determine health status
        string healthStatus;
        if (successRate >= 99.9)
            healthStatus = "Excellent";
        else if (successRate >= 99.0)
            healthStatus = "Good";
        else if (successRate >= 95.0)
            healthStatus = "Fair";
        else
            healthStatus = "Poor";

        return new GatewayHealthReport
        {
            Timestamp = DateTime.UtcNow,
            HealthStatus = healthStatus,
            SuccessRate = successRate,
            ErrorRate = errorRate,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AverageResponseTimeMs = avgResponseTime
        };
    }

    /// <summary>
    /// Get performance trends over time.
    /// </summary>
    public async Task<PerformanceTrend> GetPerformanceTrendAsync(int lastNMinutes = 60)
    {
        var trend = new PerformanceTrend
        {
            Period = $"Last {lastNMinutes} minutes",
            CollectionTime = DateTime.UtcNow,
            Samples = new List<PerformanceSample>()
        };

        // In production, you would track metrics in time-series database
        // For now, provide current snapshot
        var avgResponseTime = await _metricsService.GetAverageResponseTimeAsync();
        var totalRequests = await _metricsService.GetTotalRequestCountAsync();

        trend.Samples.Add(new PerformanceSample
        {
            Timestamp = DateTime.UtcNow,
            AverageResponseTimeMs = avgResponseTime,
            RequestsPerSecond = totalRequests > 0 ? totalRequests / (lastNMinutes * 60) : 0
        });

        return trend;
    }

    /// <summary>
    /// Get top routes by request volume.
    /// </summary>
    public async Task<List<RouteAnalytics>> GetTopRoutesByVolumeAsync(int limit = 10)
    {
        var routes = await _routeRepository.GetAllAsync();
        var analytics = new List<RouteAnalytics>();

        foreach (var route in routes)
        {
            var metrics = await _metricsService.GetRouteMetricsAsync(route.Id);
            analytics.Add(new RouteAnalytics
            {
                RouteId = route.Id,
                RouteName = route.Name,
                TotalRequests = metrics?.TotalRequests ?? 0,
                SuccessfulRequests = metrics?.SuccessfulRequests ?? 0,
                FailedRequests = metrics?.FailedRequests ?? 0,
                AverageResponseTimeMs = metrics?.AverageResponseTimeMs ?? 0
            });
        }

        return analytics
            .OrderByDescending(r => r.TotalRequests)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Get routes with highest error rates.
    /// </summary>
    public async Task<List<RouteAnalytics>> GetProblematicRoutesAsync(int limit = 10)
    {
        var routes = await _routeRepository.GetAllAsync();
        var analytics = new List<RouteAnalytics>();

        foreach (var route in routes)
        {
            var metrics = await _metricsService.GetRouteMetricsAsync(route.Id);
            if (metrics?.TotalRequests == 0)
                continue;

            var errorRate = (metrics.FailedRequests * 100.0) / metrics.TotalRequests;

            analytics.Add(new RouteAnalytics
            {
                RouteId = route.Id,
                RouteName = route.Name,
                TotalRequests = metrics.TotalRequests,
                SuccessfulRequests = metrics.SuccessfulRequests,
                FailedRequests = metrics.FailedRequests,
                AverageResponseTimeMs = metrics.AverageResponseTimeMs,
                ErrorRate = errorRate
            });
        }

        return analytics
            .OrderByDescending(r => r.ErrorRate)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Get slowest routes by average response time.
    /// </summary>
    public async Task<List<RouteAnalytics>> GetSlowestRoutesAsync(int limit = 10)
    {
        var routes = await _routeRepository.GetAllAsync();
        var analytics = new List<RouteAnalytics>();

        foreach (var route in routes)
        {
            var metrics = await _metricsService.GetRouteMetricsAsync(route.Id);
            analytics.Add(new RouteAnalytics
            {
                RouteId = route.Id,
                RouteName = route.Name,
                TotalRequests = metrics?.TotalRequests ?? 0,
                AverageResponseTimeMs = metrics?.AverageResponseTimeMs ?? 0
            });
        }

        return analytics
            .OrderByDescending(r => r.AverageResponseTimeMs)
            .Take(limit)
            .ToList();
    }
}

/// <summary>
/// Comprehensive health report for the gateway.
/// </summary>
public class GatewayHealthReport
{
    public DateTime Timestamp { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
}

/// <summary>
/// Performance trend metrics.
/// </summary>
public class PerformanceTrend
{
    public string Period { get; set; } = string.Empty;
    public DateTime CollectionTime { get; set; }
    public List<PerformanceSample> Samples { get; set; } = new();
}

/// <summary>
/// Single performance measurement sample.
/// </summary>
public class PerformanceSample
{
    public DateTime Timestamp { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double RequestsPerSecond { get; set; }
}

/// <summary>
/// Analytics for a specific route.
/// </summary>
public class RouteAnalytics
{
    public string RouteId { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double ErrorRate { get; set; }
    public double SuccessRate => TotalRequests > 0 ? (SuccessfulRequests * 100.0) / TotalRequests : 0;
}
