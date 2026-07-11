#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using DotNetApiGateway.Repositories;

/// <summary>
/// Gateway management endpoints for route configuration, monitoring, and control.
/// Provides operational endpoints for managing routes, policies, and gateway state.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GatewayManagementController : ControllerBase
{
    private readonly RoutingService _routingService;
    private readonly CircuitBreakerService _circuitBreakerService;
    private readonly RateLimitingService _rateLimitingService;
    private readonly MetricsService _metricsService;
    private readonly GatewayRouteRepository _routeRepository;
    private readonly ILogger<GatewayManagementController> _logger;

    public GatewayManagementController(
        RoutingService routingService,
        CircuitBreakerService circuitBreakerService,
        RateLimitingService rateLimitingService,
        MetricsService metricsService,
        GatewayRouteRepository routeRepository,
        ILogger<GatewayManagementController> logger)
    {
        _routingService = routingService;
        _circuitBreakerService = circuitBreakerService;
        _rateLimitingService = rateLimitingService;
        _metricsService = metricsService;
        _routeRepository = routeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new gateway route with policies and targets.
    /// </summary>
    [HttpPost("routes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoute([FromBody] GatewayRoute route)
    {
        if (route is null || string.IsNullOrWhiteSpace(route.Id))
        {
            _logger.LogWarning("Invalid route creation attempt");
            return BadRequest(new { error = "Route ID and configuration required" });
        }

        try
        {
            await _routeRepository.AddAsync(route);
            _logger.LogInformation("Route created: {RouteId}", route.Id);
            return CreatedAtAction(nameof(GetRouteById), new { id = route.Id }, route);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Route creation failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve all active routes with their configuration and metrics.
    /// </summary>
    [HttpGet("routes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoutes()
    {
        var routes = await _routeRepository.GetAllAsync();
        var routeMetrics = new List<object>();

        foreach (var route in routes)
        {
            var metrics = await _metricsService.GetRouteMetricsAsync(route.Id);
            routeMetrics.Add(new { route, metrics });
        }

        return Ok(routeMetrics);
    }

    /// <summary>
    /// Get a specific route by ID with detailed configuration and stats.
    /// </summary>
    [HttpGet("routes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRouteById(string id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route is null)
            return NotFound(new { error = "Route not found", id });

        var metrics = await _metricsService.GetRouteMetricsAsync(id);
        return Ok(new { route, metrics });
    }

    /// <summary>
    /// Update an existing route configuration with new policies and targets.
    /// </summary>
    [HttpPut("routes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoute(string id, [FromBody] GatewayRoute updatedRoute)
    {
        var existing = await _routeRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound(new { error = "Route not found", id });

        updatedRoute.Id = id;
        await _routeRepository.UpdateAsync(updatedRoute);
        _logger.LogInformation("Route updated: {RouteId}", id);
        return Ok(updatedRoute);
    }

    /// <summary>
    /// Delete a route and clean up associated metrics and state.
    /// </summary>
    [HttpDelete("routes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoute(string id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route is null)
            return NotFound(new { error = "Route not found", id });

        await _routeRepository.DeleteAsync(id);
        _logger.LogInformation("Route deleted: {RouteId}", id);
        return NoContent();
    }

    /// <summary>
    /// Get detailed metrics for a specific route including response times and error rates.
    /// </summary>
    [HttpGet("metrics/routes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRouteMetrics(string id)
    {
        var metrics = await _metricsService.GetRouteMetricsAsync(id);
        return Ok(metrics);
    }

    /// <summary>
    /// Get circuit breaker statuses for all configured targets.
    /// </summary>
    [HttpGet("circuit-breakers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCircuitBreakerStatuses()
    {
        var statuses = await _circuitBreakerService.GetAllStatusesAsync();
        return Ok(statuses);
    }

    /// <summary>
    /// Reset a circuit breaker to Closed state, allowing traffic to resume.
    /// </summary>
    [HttpPost("circuit-breakers/{targetId}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetCircuitBreaker(string targetId)
    {
        var status = await _circuitBreakerService.GetStatusAsync(targetId);
        if (status is null)
            return NotFound(new { error = "Circuit breaker not found", targetId });

        await _circuitBreakerService.ResetCircuitAsync(targetId);
        _logger.LogInformation("Circuit breaker reset: {TargetId}", targetId);
        var updatedStatus = await _circuitBreakerService.GetStatusAsync(targetId);
        return Ok(updatedStatus);
    }

    /// <summary>
    /// Get the current rate limit status for a specific key.
    /// </summary>
    [HttpGet("rate-limits/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateLimitStatus(string key)
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

        var info = await _rateLimitingService.GetRateLimitInfoAsync(key, policy);
        return Ok(info);
    }

    /// <summary>
    /// Resets the rate limit for a specific key.
    /// </summary>
    [HttpPost("rate-limits/{key}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetRateLimitForKey(string key)
    {
        await _rateLimitingService.ResetKeyLimitsAsync(key);
        return Ok(new { message = $"Rate limit for key '{key}' reset." });
    }

    /// <summary>
    /// Resets all rate limits.
    /// </summary>
    [HttpPost("rate-limits/reset-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllRateLimits()
    {
        await _rateLimitingService.ResetAllLimitsAsync();
        return Ok(new { message = "All rate limits reset." });
    }

    /// <summary>
    /// Get overall gateway metrics including total requests, success rate, and performance stats.
    /// </summary>
    [HttpGet("metrics/global")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalMetrics()
    {
        var metrics = new
        {
            timestamp = DateTime.UtcNow,
            totalRequests = await _metricsService.GetTotalRequestCountAsync(),
            successfulRequests = await _metricsService.GetSuccessfulRequestCountAsync(),
            failedRequests = await _metricsService.GetFailedRequestCountAsync(),
            averageResponseTime = await _metricsService.GetAverageResponseTimeAsync(),
            routes = await _routeRepository.GetCountAsync()
        };

        return Ok(metrics);
    }
}