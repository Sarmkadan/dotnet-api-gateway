#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for monitoring health of backend targets.
/// Targets seen by <see cref="CheckTargetHealthAsync"/> or <see cref="CheckAllTargetsAsync"/>
/// are tracked and re-checked periodically by a background timer.
/// </summary>
public sealed class HealthCheckService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Timer _healthCheckTimer;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, RouteTarget> _trackedTargets = new();
    private readonly DateTime _startedAt = DateTime.UtcNow;
    private int _periodicCheckRunning;

    public HealthCheckService(HttpClient httpClient, ILogger<HealthCheckService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task<bool> CheckTargetHealthAsync(RouteTarget target)
    {
        _trackedTargets[target.Id] = target;

        if (string.IsNullOrWhiteSpace(target.HealthCheckPath))
            return target.IsHealthy;

        try
        {
            // The health check path is target-relative already; never run it through
            // route prefix stripping.
            var healthCheckUrl = new Uri(new Uri(target.BaseUrl), target.HealthCheckPath).ToString();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(healthCheckUrl, cts.Token);

            var isHealthy = response.IsSuccessStatusCode;
            target.UpdateHealthStatus(isHealthy);

            _logger.LogInformation(
                "Health check for target {TargetName}: {Status}",
                target.Name,
                isHealthy ? "HEALTHY" : "UNHEALTHY");

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Health check failed for target {TargetName}: {ErrorMessage}",
                target.Name,
                ex.Message);

            target.UpdateHealthStatus(false, ex.Message);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> CheckAllTargetsAsync(IEnumerable<RouteTarget> targets)
    {
        var results = new Dictionary<string, bool>();

        var tasks = targets.Select(async target =>
        {
            var isHealthy = await CheckTargetHealthAsync(target);
            return new { target.Id, IsHealthy = isHealthy };
        });

        var results_enumerable = await Task.WhenAll(tasks);

        foreach (var result in results_enumerable)
        {
            results[result.Id] = result.IsHealthy;
        }

        return results;
    }

    private void PerformHealthChecks(object? state)
    {
        // Skip this tick if the previous sweep is still in flight.
        if (Interlocked.CompareExchange(ref _periodicCheckRunning, 1, 0) != 0)
            return;

        var targets = _trackedTargets.Values.ToList();
        if (targets.Count == 0)
        {
            Volatile.Write(ref _periodicCheckRunning, 0);
            return;
        }

        _logger.LogDebug("Performing periodic health checks for {TargetCount} tracked targets", targets.Count);

        _ = RunPeriodicChecksAsync(targets);
    }

    private async Task RunPeriodicChecksAsync(List<RouteTarget> targets)
    {
        try
        {
            await CheckAllTargetsAsync(targets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Periodic health check sweep failed: {ErrorMessage}", ex.Message);
        }
        finally
        {
            Volatile.Write(ref _periodicCheckRunning, 0);
        }
    }

    public GatewayHealth GetGatewayHealth()
    {
        var targets = _trackedTargets.Values.ToList();
        var unhealthy = targets.Where(t => !t.IsHealthy).ToList();

        var health = new GatewayHealth
        {
            IsHealthy = unhealthy.Count == 0,
            Timestamp = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - _startedAt,
            Version = "2.0.2"
        };

        health.Details["trackedTargets"] = targets.Count;
        health.Details["unhealthyTargets"] = unhealthy.Count;
        if (unhealthy.Count > 0)
        {
            health.Details["unhealthyTargetNames"] = unhealthy.Select(t => t.Name).ToArray();
        }

        return health;
    }

    public void Dispose()
    {
        _healthCheckTimer.Dispose();
        _httpClient.Dispose();
    }
}

public sealed class GatewayHealth
{
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Uptime { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = [];
}
