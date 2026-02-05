#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

/// <summary>
/// Utility for analyzing and reporting performance metrics.
/// Tracks operation timing and provides statistical analysis.
/// </summary>
public sealed class PerformanceAnalyzer
{
    private readonly List<long> _measurements = new();
    private readonly object _lock = new();

    /// <summary>
    /// Record a measurement in milliseconds.
    /// </summary>
    public void RecordMeasurement(long milliseconds)
    {
        if (milliseconds < 0)
            return;

        lock (_lock)
        {
            _measurements.Add(milliseconds);
        }
    }

    /// <summary>
    /// Get average of all measurements.
    /// </summary>
    public double GetAverage()
    {
        lock (_lock)
        {
            return _measurements.Count == 0 ? 0 : _measurements.Average();
        }
    }

    /// <summary>
    /// Get minimum measurement.
    /// </summary>
    public long GetMinimum()
    {
        lock (_lock)
        {
            return _measurements.Count == 0 ? 0 : _measurements.Min();
        }
    }

    /// <summary>
    /// Get maximum measurement.
    /// </summary>
    public long GetMaximum()
    {
        lock (_lock)
        {
            return _measurements.Count == 0 ? 0 : _measurements.Max();
        }
    }

    /// <summary>
    /// Get median of all measurements.
    /// </summary>
    public long GetMedian()
    {
        lock (_lock)
        {
            if (_measurements.Count == 0)
                return 0;

            var sorted = _measurements.OrderBy(m => m).ToList();
            int mid = sorted.Count / 2;

            if (sorted.Count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2;

            return sorted[mid];
        }
    }

    /// <summary>
    /// Get 95th percentile of measurements.
    /// </summary>
    public long GetPercentile95()
    {
        return GetPercentile(95);
    }

    /// <summary>
    /// Get specified percentile of measurements.
    /// </summary>
    public long GetPercentile(double percentile)
    {
        lock (_lock)
        {
            if (_measurements.Count == 0)
                return 0;

            var sorted = _measurements.OrderBy(m => m).ToList();
            int index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

    /// <summary>
    /// Get total number of measurements.
    /// </summary>
    public int GetCount()
    {
        lock (_lock)
        {
            return _measurements.Count;
        }
    }

    /// <summary>
    /// Get summary statistics.
    /// </summary>
    public PerformanceSummary GetSummary()
    {
        lock (_lock)
        {
            return new PerformanceSummary
            {
                Count = _measurements.Count,
                Average = _measurements.Count == 0 ? 0 : _measurements.Average(),
                Minimum = _measurements.Count == 0 ? 0 : _measurements.Min(),
                Maximum = _measurements.Count == 0 ? 0 : _measurements.Max(),
                Median = GetMedian(),
                Percentile95 = GetPercentile95()
            };
        }
    }

    /// <summary>
    /// Clear all measurements.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _measurements.Clear();
        }
    }
}

/// <summary>
/// Summary of performance statistics.
/// </summary>
public sealed class PerformanceSummary
{
    public int Count { get; set; }
    public double Average { get; set; }
    public long Minimum { get; set; }
    public long Maximum { get; set; }
    public long Median { get; set; }
    public long Percentile95 { get; set; }

    public override string ToString()
    {
        return $"Count: {Count}, Avg: {Average:F2}ms, Min: {Minimum}ms, Max: {Maximum}ms, Med: {Median}ms, P95: {Percentile95}ms";
    }
}
