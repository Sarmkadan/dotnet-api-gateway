#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for gateway management operations including route CRUD, metrics, and system state management.
/// Provides business logic for managing routes, policies, and gateway state.
/// </summary>
public sealed class GatewayManagementService
{
    private readonly RoutingService _routingService;
    private readonly CircuitBreakerService _circuitBreakerService;
    private readonly RateLimitingService _rateLimitingService;
    private readonly MetricsService _metricsService;
    private readonly GatewayRouteRepository _routeRepository;
    private readonly ILogger<GatewayManagementService> _logger;

    public GatewayManagementService(
        RoutingService routingService,
        CircuitBreakerService circuitBreakerService,
        RateLimitingService rateLimitingService,
        MetricsService metricsService,
        GatewayRouteRepository routeRepository,
        ILogger<GatewayManagementService>? logger = null)
    {
        _routingService = routingService;
        _circuitBreakerService = circuitBreakerService;
        _rateLimitingService = rateLimitingService;
        _metricsService = metricsService;
        _routeRepository = routeRepository;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GatewayManagementService>.Instance;
    }

    /// <summary>
    /// Create a new gateway route with policies and targets.
    /// </summary>
    public async Task<GatewayRoute> CreateRouteAsync(GatewayRoute route)
    {
        if (route is null || string.IsNullOrWhiteSpace(route.Id))
        {
            _logger.LogWarning("Invalid route creation attempt");
            throw new ArgumentException("Route ID and configuration required");
        }

        try
        {
            var created = await _routeRepository.AddAsync(route);
            _logger.LogInformation("Route created: {RouteId}", route.Id);
            return created;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Route creation failed");
            throw;
        }
    }

    /// <summary>
    /// Retrieve all active routes with their configuration and metrics.
    /// </summary>
    public async Task<IEnumerable<object>> GetAllRoutesWithMetricsAsync()
    {
        var routes = await _routeRepository.GetAllAsync();
        var routeMetrics = new List<object>();

        foreach (var route in routes)
        {
            var metrics = await _metricsService.GetRouteMetricsAsync(route.Id);
            routeMetrics.Add(new { route, metrics });
        }

        return routeMetrics;
    }

    /// <summary>
    /// Get a specific route by ID with detailed configuration and stats.
    /// </summary>
    public async Task<(GatewayRoute? route, object? metrics)> GetRouteByIdWithMetricsAsync(string id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route is null)
        {
            return (null, null);
        }

        var metrics = await _metricsService.GetRouteMetricsAsync(id);
        return (route, metrics);
    }

    /// <summary>
    /// Update an existing route configuration with new policies and targets.
    /// </summary>
    public async Task<GatewayRoute> UpdateRouteAsync(string id, GatewayRoute updatedRoute)
    {
        var existing = await _routeRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Route with id {id} not found");
        }

        updatedRoute.Id = id;
        var updated = await _routeRepository.UpdateAsync(updatedRoute);
        _logger.LogInformation("Route updated: {RouteId}", id);
        return updated;
    }

    /// <summary>
    /// Delete a route and clean up associated metrics and state.
    /// </summary>
    public async Task<bool> DeleteRouteAsync(string id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route is null)
        {
            return false;
        }

        var deleted = await _routeRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Route deleted: {RouteId}", id);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent route: {RouteId}", id);
        }
        return deleted;
    }

    /// <summary>
    /// Get detailed metrics for a specific route including response times and error rates.
    /// </summary>
    public async Task<object> GetRouteMetricsAsync(string id)
    {
        return await _metricsService.GetRouteMetricsAsync(id);
    }

    /// <summary>
    /// Get circuit breaker statuses for all configured targets.
    /// </summary>
    public async Task<IEnumerable<object>> GetCircuitBreakerStatusesAsync()
    {
        return await _circuitBreakerService.GetAllStatusesAsync();
    }

    /// <summary>
    /// Reset a circuit breaker to Closed state, allowing traffic to resume.
    /// </summary>
    public async Task<object> ResetCircuitBreakerAsync(string targetId)
    {
        var status = await _circuitBreakerService.GetStatusAsync(targetId);
        if (status is null)
        {
            throw new KeyNotFoundException($"Circuit breaker not found for target {targetId}");
        }

        await _circuitBreakerService.ResetCircuitAsync(targetId);
        _logger.LogInformation("Circuit breaker reset: {TargetId}", targetId);
        var updatedStatus = await _circuitBreakerService.GetStatusAsync(targetId);
        return updatedStatus!;
    }

    /// <summary>
    /// Get the current rate limit status for a specific key.
    /// </summary>
    public async Task<object> GetRateLimitStatusAsync(string key)
    {
        // Try to get a rate limit policy from an active route
        var activeRoute = (await _routeRepository.GetAllAsync())
            .FirstOrDefault(r => r.IsActive && r.RateLimitPolicy?.IsEnabled() == true);

        var policy = activeRoute?.RateLimitPolicy ?? new RateLimitPolicy
        {
            RequestsPerMinute = 1000,
            BurstSize = 10,
            Enabled = true
        };

        return await _rateLimitingService.GetRateLimitInfoAsync(key, policy);
    }

    /// <summary>
    /// Resets the rate limit for a specific key.
    /// </summary>
    public async Task ResetRateLimitForKeyAsync(string key)
    {
        await _rateLimitingService.ResetKeyLimitsAsync(key);
    }

    /// <summary>
    /// Resets all rate limits.
    /// </summary>
    public async Task ResetAllRateLimitsAsync()
    {
        await _rateLimitingService.ResetAllLimitsAsync();
    }

    /// <summary>
    /// Get overall gateway metrics including total requests, success rate, and performance stats.
    /// </summary>
    public async Task<object> GetGlobalMetricsAsync()
    {
        return new
        {
            timestamp = DateTime.UtcNow,
            totalRequests = await _metricsService.GetTotalRequestCountAsync(),
            successfulRequests = await _metricsService.GetSuccessfulRequestCountAsync(),
            failedRequests = await _metricsService.GetFailedRequestCountAsync(),
            averageResponseTime = await _metricsService.GetAverageResponseTimeAsync(),
            routes = await _routeRepository.GetCountAsync()
        };
    }
}