// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

/// <summary>
/// Utility for tracking and analyzing rate limit metrics.
/// Provides insights into rate limit usage patterns and violations.
/// </summary>
public class RateLimitMetrics
{
    private readonly Dictionary<string, ClientRateLimitStats> _clientStats = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Record a request for a client.
    /// </summary>
    public void RecordRequest(string clientId, bool limited = false)
    {
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
}

/// <summary>
/// Rate limit statistics for a specific client.
/// </summary>
public class ClientRateLimitStats
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
public class RateLimitOverallMetrics
{
    public int TotalClients { get; set; }
    public long TotalRequests { get; set; }
    public long TotalLimitedRequests { get; set; }
    public double AverageRequestsPerClient { get; set; }

    public double OverallViolationRate => TotalRequests == 0 ? 0 : (TotalLimitedRequests * 100.0) / TotalRequests;
}
