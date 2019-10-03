#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for monitoring health of backend targets
/// </summary>
public sealed class HealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly Timer _healthCheckTimer;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(HttpClient httpClient, ILogger<HealthCheckService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task<bool> CheckTargetHealthAsync(RouteTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.HealthCheckPath))
            return target.IsHealthy;

        try
        {
            var healthCheckUrl = target.GetForwardUrl(target.HealthCheckPath);
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
        _logger.LogDebug("Performing periodic health checks");
        // This would be called by the timer to check all targets periodically
        // Implementation would depend on dependency injection of route repository
    }

    public GatewayHealth GetGatewayHealth()
    {
        return new GatewayHealth
        {
            IsHealthy = true,
            Timestamp = DateTime.UtcNow,
            Uptime = TimeSpan.Zero,
            Version = "2.0.2"
        };
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _httpClient?.Dispose();
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
