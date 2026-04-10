#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for collecting and reporting gateway metrics
/// </summary>
public sealed class MetricsService
{
    private readonly ReaderWriterLockSlim _lock = new();
    private long _totalRequests = 0;
    private long _totalSuccessfulRequests = 0;
    private long _totalFailedRequests = 0;
    private double _totalResponseTimeMs = 0;
    private DateTime _startTime = DateTime.UtcNow;
    private readonly Dictionary<string, RouteMetrics> _routeMetrics = [];
    private readonly Dictionary<int, long> _statusCodeCounts = [];

    public void RecordRequest(string routeId, int statusCode, TimeSpan duration)
    {
        _lock.EnterWriteLock();
        try
        {
            _totalRequests++;
            _totalResponseTimeMs += duration.TotalMilliseconds;

            if (statusCode >= 200 && statusCode < 300)
                _totalSuccessfulRequests++;
            else if (statusCode >= 400)
                _totalFailedRequests++;

            // Track status code
            if (!_statusCodeCounts.ContainsKey(statusCode))
                _statusCodeCounts[statusCode] = 0;
            _statusCodeCounts[statusCode]++;

            // Track per-route metrics
            if (!_routeMetrics.ContainsKey(routeId))
                _routeMetrics[routeId] = new RouteMetrics { RouteId = routeId };

            var routeMetric = _routeMetrics[routeId];
            routeMetric.RequestCount++;
            routeMetric.TotalResponseTimeMs += duration.TotalMilliseconds;
            routeMetric.LastRequestAt = DateTime.UtcNow;

            if (duration.TotalMilliseconds > routeMetric.MaxResponseTimeMs)
                routeMetric.MaxResponseTimeMs = duration.TotalMilliseconds;

            if (duration.TotalMilliseconds < routeMetric.MinResponseTimeMs || routeMetric.MinResponseTimeMs == 0)
                routeMetric.MinResponseTimeMs = duration.TotalMilliseconds;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public GatewayMetrics GetMetrics()
    {
        _lock.EnterReadLock();
        try
        {
            var uptime = DateTime.UtcNow - _startTime;
            var avgResponseTime = _totalRequests > 0 ? _totalResponseTimeMs / _totalRequests : 0;
            var successRate = _totalRequests > 0 ? (double)_totalSuccessfulRequests / _totalRequests * 100 : 0;

            return new GatewayMetrics
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _totalSuccessfulRequests,
                FailedRequests = _totalFailedRequests,
                SuccessRate = successRate,
                AverageResponseTimeMs = avgResponseTime,
                Uptime = uptime,
                StartTime = _startTime,
                StatusCodeDistribution = new Dictionary<int, long>(_statusCodeCounts),
                RouteMetrics = _routeMetrics.Values.ToList()
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public RouteMetrics? GetRouteMetrics(string routeId)
    {
        _lock.EnterReadLock();
        try
        {
            return _routeMetrics.TryGetValue(routeId, out var metrics) ? metrics : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Reset()
    {
        _lock.EnterWriteLock();
        try
        {
            _totalRequests = 0;
            _totalSuccessfulRequests = 0;
            _totalFailedRequests = 0;
            _totalResponseTimeMs = 0;
            _startTime = DateTime.UtcNow;
            _routeMetrics.Clear();
            _statusCodeCounts.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}

public sealed class GatewayMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime StartTime { get; set; }
    public Dictionary<int, long> StatusCodeDistribution { get; set; } = [];
    public List<RouteMetrics> RouteMetrics { get; set; } = [];

    public double GetRequestsPerSecond()
    {
        if (Uptime.TotalSeconds == 0)
            return 0;

        return TotalRequests / Uptime.TotalSeconds;
    }
}

public sealed class RouteMetrics
{
    public string RouteId { get; set; } = string.Empty;
    public long RequestCount { get; set; } = 0;
    public double TotalResponseTimeMs { get; set; } = 0;
    public double MinResponseTimeMs { get; set; } = 0;
    public double MaxResponseTimeMs { get; set; } = 0;
    public DateTime LastRequestAt { get; set; }

    public double GetAverageResponseTime()
    {
        return RequestCount > 0 ? TotalResponseTimeMs / RequestCount : 0;
    }

    public double GetRequestsPerMinute()
    {
        var elapsed = DateTime.UtcNow - LastRequestAt;
        if (elapsed.TotalMinutes == 0)
            return 0;

        return RequestCount / elapsed.TotalMinutes;
    }
}
