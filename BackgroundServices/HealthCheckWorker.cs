#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.BackgroundServices;

using DotNetApiGateway.Services;
using DotNetApiGateway.Repositories;

/// <summary>
/// Background service that periodically checks health of backend targets.
/// Identifies unresponsive services and updates circuit breaker state.
/// </summary>
public class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly GatewayRouteRepository _routeRepository;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public HealthCheckWorker(
        ILogger<HealthCheckWorker> logger,
        HealthCheckService healthCheckService,
        GatewayRouteRepository routeRepository)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _routeRepository = routeRepository;
    }

    /// <summary>
    /// Execute health checks on a background interval.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Health check worker stopped");
    }

    /// <summary>
    /// Perform health checks for all configured routes and targets.
    /// </summary>
    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        var routes = await _routeRepository.GetAllAsync();

        foreach (var route in routes)
        {
            foreach (var target in route.Targets)
            {
                try
                {
                    var isHealthy = await _healthCheckService.CheckTargetHealthAsync(target);

                    if (!isHealthy)
                    {
                        _logger.LogWarning(
                            "Target {TargetName} ({TargetUrl}) is unhealthy",
                            target.Name,
                            target.BaseUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Health check failed for target {TargetName}",
                        target.Name);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;
            }
        }

        _logger.LogDebug("Health check cycle completed");
    }
}
