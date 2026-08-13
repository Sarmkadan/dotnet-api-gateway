#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

/// <summary>
/// Utility for tracking and analyzing rate limit metrics.
/// Provides insights into rate limit usage patterns and violations.
/// </summary>
public sealed class RateLimitMetrics
{
    private readonly Dictionary<string, ClientRateLimitStats> _clientStats = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Record a request for a client.
    /// </summary>
    public void RecordRequest(string clientId, bool limited = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        if (string.IsNullOrWhiteSpace(clientId))
            return;

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_clientStats.TryGetValue(clientId, out var stats))
            {
                stats.TotalRequests++;
                if (limited)
                    stats.LimitedRequests++;
            }
            else
            {
                _lock.EnterWriteLock();
                try
                {
                    _clientStats[clientId] = new ClientRateLimitStats
                    {
                        ClientId = clientId,
                        TotalRequests = 1,
                        LimitedRequests = limited ? 1 : 0,
                        FirstRequestTime = DateTime.UtcNow,
                        LastRequestTime = DateTime.UtcNow
                    };
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Get statistics for a specific client.
    /// </summary>
    public ClientRateLimitStats? GetClientStats(string clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        _lock.EnterReadLock();
        try
        {
            return _clientStats.TryGetValue(clientId, out var stats) ? stats : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get all client statistics.
    /// </summary>
    public List<ClientRateLimitStats> GetAllStats()
    {
        _lock.EnterReadLock();
        try
        {
            return new List<ClientRateLimitStats>(_clientStats.Values);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get top N clients by request count.
    /// </summary>
    public List<ClientRateLimitStats> GetTopClients(int limit = 10)
    {
        _lock.EnterReadLock();
        try
        {
            return _clientStats.Values
                .OrderByDescending(s => s.TotalRequests)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get clients with highest rate limit violation rate.
    /// </summary>
    public List<ClientRateLimitStats> GetViolatingClients(int limit = 10)
    {
        _lock.EnterReadLock();
        try
        {
            return _clientStats.Values
                .Where(s => s.ViolationRate > 0)
                .OrderByDescending(s => s.ViolationRate)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get overall metrics.
    /// </summary>
    public RateLimitOverallMetrics GetOverallMetrics()
    {
        _lock.EnterReadLock();
        try
        {
            return new RateLimitOverallMetrics
            {
                TotalClients = _clientStats.Count,
                TotalRequests = _clientStats.Values.Sum(s => s.TotalRequests),
                TotalLimitedRequests = _clientStats.Values.Sum(s => s.LimitedRequests),
                AverageRequestsPerClient = _clientStats.Count == 0 ? 0 : _clientStats.Values.Average(s => s.TotalRequests)
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Calculate the throttle rate for a client.
    /// </summary>
    public double ThrottleRate(string clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        _lock.EnterReadLock();
        try
        {
            if (_clientStats.TryGetValue(clientId, out var stats))
            {
                return stats.TotalRequests == 0 ? 0 : (stats.LimitedRequests * 100.0) / stats.TotalRequests;
            }
            return 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get top N clients by throttle rate.
    /// </summary>
    public List<ClientRateLimitStats> TopOffenders(int n)
    {
        _lock.EnterReadLock();
        try
        {
            return _clientStats.Values
                .OrderByDescending(s => s.ViolationRate)
                .Take(n)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Convert rate limit metrics to a summary string.
    /// </summary>
    public string ToSummary()
    {
        _lock.EnterReadLock();
        try
        {
            var overallMetrics = GetOverallMetrics();
            return $"Total Clients: {overallMetrics.TotalClients}, Total Requests: {overallMetrics.TotalRequests}, Total Limited Requests: {overallMetrics.TotalLimitedRequests}, Average Requests Per Client: {overallMetrics.AverageRequestsPerClient}";
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clear all statistics.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _clientStats.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Remove old statistics entries (older than specified time).
    /// </summary>
    public int RemoveOldEntries(TimeSpan age)
    {
        _lock.EnterWriteLock();
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(age);
            var keysToRemove = _clientStats
                .Where(kvp => kvp.Value.LastRequestTime < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _clientStats.Remove(key);
            }

            return keysToRemove.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns a concise string representation of the current metrics.
    /// Includes aggregated client information and overall statistics.
    /// </summary>
    public override string ToString()
    {
        _lock.EnterReadLock();
        try
        {
            var overall = GetOverallMetrics();

            // Determine the earliest and latest request times across all clients.
            var firstRequest = _clientStats.Values.OrderBy(s => s.FirstRequestTime).FirstOrDefault()?.FirstRequestTime ?? DateTime.MinValue;
            var lastRequest  = _clientStats.Values.OrderByDescending(s => s.LastRequestTime).FirstOrDefault()?.LastRequestTime ?? DateTime.MinValue;

            // Since this class aggregates many clients, we use a placeholder for ClientId.
            const string clientIdPlaceholder = "All";

            return $"RateLimitMetrics {{ ClientId = {clientIdPlaceholder}, TotalRequests = {overall.TotalRequests}, LimitedRequests = {overall.TotalLimitedRequests}, FirstRequestTime = {firstRequest:u}, LastRequestTime = {lastRequest:u}, TotalClients = {overall.TotalClients} }}";
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}

/// <summary>
/// Rate limit statistics for a specific client.
/// </summary>
public sealed class ClientRateLimitStats
{
    public string ClientId { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long LimitedRequests { get; set; }
    public DateTime FirstRequestTime { get; set; }
    public DateTime LastRequestTime { get; set; }

    public double ViolationRate => TotalRequests == 0 ? 0 : (LimitedRequests * 100.0) / TotalRequests;

    public TimeSpan ActiveDuration => LastRequestTime - FirstRequestTime;
}

/// <summary>
/// Overall rate limit metrics.
/// </summary>
public sealed class RateLimitOverallMetrics
{
    public int TotalClients { get; set; }
    public long TotalRequests { get; set; }
    public long TotalLimitedRequests { get; set; }
    public double AverageRequestsPerClient { get; set; }

    public double OverallViolationRate => TotalRequests == 0 ? 0 : (TotalLimitedRequests * 100.0) / TotalRequests;
}
