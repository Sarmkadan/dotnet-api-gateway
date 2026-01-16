// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Middleware;

using DotNetApiGateway.Services;

/// <summary>
/// Middleware for monitoring and recording request performance metrics.
/// Tracks response times, status codes, and provides performance insights.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly MetricsService _metricsService;
    private readonly long _slowRequestThresholdMs = 1000; // Log warnings for requests > 1s

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, MetricsService metricsService)
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Invoke middleware to capture performance metrics for each request.
    /// Records timing data and identifies slow requests for optimization analysis.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Record metrics
            await _metricsService.RecordRequestAsync(
                method: context.Request.Method,
                path: context.Request.Path.Value ?? "/",
                statusCode: context.Response.StatusCode,
                responseTimeMs: (int)elapsedMs,
                timestamp: startTime
            );

            // Log slow requests
            if (elapsedMs > _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms with status {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs,
                    context.Response.StatusCode);
            }

            // Log performance summary every 100th request
            if (await _metricsService.GetTotalRequestCountAsync() % 100 == 0)
            {
                var avgResponseTime = await _metricsService.GetAverageResponseTimeAsync();
                _logger.LogInformation(
                    "Performance summary: Average response time {AvgMs}ms over {Total} requests",
                    avgResponseTime,
                    await _metricsService.GetTotalRequestCountAsync());
            }
        }
    }
}
