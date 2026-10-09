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

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckService"/> class.
    /// Sets the HTTP timeout to 5 seconds and starts the periodic health check timer
    /// (first run after 30 seconds, then every 30 seconds).
    /// </summary>
    /// <param name="httpClient">HTTP client used to probe target health endpoints. The service takes ownership and disposes it.</param>
    /// <param name="logger">Logger for health check results and failures.</param>
    public HealthCheckService(HttpClient httpClient, ILogger<HealthCheckService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Checks the health of a single backend target and records the result on the target.
    /// The target is added to the tracked set so the periodic timer re-checks it.
    /// </summary>
    /// <param name="target">The backend target to check.</param>
    /// <returns>
    /// <c>true</c> if the health check endpoint returned a success status code;
    /// <c>false</c> if it failed, timed out, or threw an exception.
    /// If the target has no health check path configured, returns the target's current <see cref="RouteTarget.IsHealthy"/> value without probing.
    /// </returns>
    public async Task<bool> CheckTargetHealthAsync(RouteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

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

    /// <summary>
    /// Checks the health of multiple backend targets concurrently.
    /// </summary>
    /// <param name="targets">The backend targets to check.</param>
    /// <returns>A dictionary mapping each target's <see cref="RouteTarget.Id"/> to its health result.</returns>
    /// <seealso cref="CheckTargetHealthAsync"/>
    public async Task<Dictionary<string, bool>> CheckAllTargetsAsync(IEnumerable<RouteTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

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

    /// <summary>
    /// Timer callback that starts a periodic health sweep over all tracked targets.
    /// Skips the tick if the previous sweep is still running or if no targets are tracked.
    /// </summary>
    /// <param name="state">Unused timer state.</param>
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

    /// <summary>
    /// Runs one periodic sweep over the given targets and releases the in-flight flag when done.
    /// Exceptions are logged rather than propagated, since the caller is fire-and-forget.
    /// </summary>
    /// <param name="targets">Snapshot of the tracked targets to check.</param>
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

    /// <summary>
    /// Builds an aggregate health snapshot of the gateway based on the currently tracked targets.
    /// The gateway is considered healthy when no tracked target is unhealthy.
    /// </summary>
    /// <returns>A <see cref="GatewayHealth"/> snapshot including uptime, version, and target counts.</returns>
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

    /// <summary>
    /// Stops the periodic health check timer and disposes the underlying <see cref="HttpClient"/>.
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer.Dispose();
        _httpClient.Dispose();
    }
}

/// <summary>
/// Aggregate health snapshot of the gateway, as returned by <see cref="HealthCheckService.GetGatewayHealth"/>.
/// </summary>
public sealed class GatewayHealth
{
    /// <summary>
    /// Gets or sets a value indicating whether all tracked targets are healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the time elapsed since the <see cref="HealthCheckService"/> instance was created.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the gateway version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional diagnostic details, such as tracked and unhealthy target counts and unhealthy target names.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = [];
}
