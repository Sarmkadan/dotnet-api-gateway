#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using DotNetApiGateway.Repositories;

namespace DotNetApiGateway.Controllers;

/// <summary>
/// Gateway management endpoints for route configuration, monitoring, and control.
/// Provides operational endpoints for managing routes, policies, gateway state, and hot-reload configuration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GatewayManagementController : ControllerBase
{
    private readonly GatewayManagementService _gatewayManagementService;
    private readonly ILogger<GatewayManagementController> _logger;

    public GatewayManagementController(
        GatewayManagementService gatewayManagementService,
        ILogger<GatewayManagementController> logger)
    {
        _gatewayManagementService = gatewayManagementService ?? throw new ArgumentNullException(nameof(gatewayManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new gateway route with policies and targets.
    /// </summary>
    [HttpPost("routes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoute([FromBody] GatewayRoute route)
    {
        try
        {
            var createdRoute = await _gatewayManagementService.CreateRouteAsync(route);
            return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.Id }, createdRoute);
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
        var routesWithMetrics = await _gatewayManagementService.GetAllRoutesWithMetricsAsync();
        return Ok(routesWithMetrics);
    }

    /// <summary>
    /// Get a specific route by ID with detailed configuration and stats.
    /// </summary>
    [HttpGet("routes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRouteById(string id)
    {
        var (route, metrics) = await _gatewayManagementService.GetRouteByIdWithMetricsAsync(id);
        if (route is null)
            return NotFound(new { error = "Route not found", id });

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
        try
        {
            var updated = await _gatewayManagementService.UpdateRouteAsync(id, updatedRoute);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Route not found", id });
        }
    }

    /// <summary>
    /// Delete a route and clean up associated metrics and state.
    /// </summary>
    [HttpDelete("routes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoute(string id)
    {
        var deleted = await _gatewayManagementService.DeleteRouteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Route not found", id });

        return NoContent();
    }

    /// <summary>
    /// Get detailed metrics for a specific route including response times and error rates.
    /// </summary>
    [HttpGet("metrics/routes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRouteMetrics(string id)
    {
        var metrics = await _gatewayManagementService.GetRouteMetricsAsync(id);
        return Ok(metrics);
    }

    /// <summary>
    /// Get circuit breaker statuses for all configured targets.
    /// </summary>
    [HttpGet("circuit-breakers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCircuitBreakerStatuses()
    {
        var statuses = await _gatewayManagementService.GetCircuitBreakerStatusesAsync();
        return Ok(statuses);
    }

    /// <summary>
    /// Reset a circuit breaker to Closed state, allowing traffic to resume.
    /// </summary>
    [HttpPost("circuit-breakers/{targetId}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetCircuitBreaker(string targetId)
    {
        try
        {
            var updatedStatus = await _gatewayManagementService.ResetCircuitBreakerAsync(targetId);
            return Ok(updatedStatus);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, targetId });
        }
    }

    /// <summary>
    /// Get the current rate limit status for a specific key.
    /// </summary>
    [HttpGet("rate-limits/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateLimitStatus(string key)
    {
        var info = await _gatewayManagementService.GetRateLimitStatusAsync(key);
        return Ok(info);
    }

    /// <summary>
    /// Resets the rate limit for a specific key.
    /// </summary>
    [HttpPost("rate-limits/{key}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetRateLimitForKey(string key)
    {
        await _gatewayManagementService.ResetRateLimitForKeyAsync(key);
        return Ok(new { message = $"Rate limit for key '{key}' reset." });
    }

    /// <summary>
    /// Resets all rate limits.
    /// </summary>
    [HttpPost("rate-limits/reset-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllRateLimits()
    {
        await _gatewayManagementService.ResetAllRateLimitsAsync();
        return Ok(new { message = "All rate limits reset." });
    }

    /// <summary>
    /// Get overall gateway metrics including total requests, success rate, and performance stats.
    /// </summary>
    [HttpGet("metrics/global")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalMetrics()
    {
        var metrics = await _gatewayManagementService.GetGlobalMetricsAsync();
        return Ok(metrics);
    }

    /// <summary>
    /// Get the current route table version and configuration status.
    /// </summary>
    [HttpGet("hot-reload/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetHotReloadStatus()
    {
        try
        {
            var status = _gatewayManagementService.GetHotReloadStatus();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hot-reload status");
            return StatusCode(500, new { error = "Failed to get hot-reload status", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger a manual reload of the route configuration.
    /// This is useful when automatic change detection fails or for testing purposes.
    /// </summary>
    [HttpPost("hot-reload/reload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerRouteReload()
    {
        try
        {
            var result = await _gatewayManagementService.TriggerRouteReloadAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering route reload");
            return StatusCode(500, new { error = "Failed to trigger route reload", details = ex.Message });
        }
    }

    /// <summary>
    /// Subscribe to route configuration changes using Server-Sent Events (SSE).
    /// Clients can listen for change events to implement real-time route configuration updates.
    /// </summary>
    [HttpGet("hot-reload/events")]
    public async Task SubscribeToRouteEvents()
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var routeRepository = _gatewayManagementService.GetRouteRepository();
        var changeToken = routeRepository.ChangeToken;

        _logger.LogInformation("Client subscribed to route configuration change events");

        // Send initial connection confirmation
        await Response.WriteAsync("event: connected\ndata: {\"status\": \"connected\", \"timestamp\": \"" + DateTime.UtcNow.ToString("o") + "\"}\n\n");
        await Response.Body.FlushAsync();

        // Register callback for changes
        var registration = changeToken.RegisterChangeCallback(async state =>
        {
            var httpContext = (HttpContext)state!;
            try
            {
                var repo = (GatewayRouteRepository)httpContext.RequestServices.GetService(typeof(GatewayRouteRepository))!;
                var status = repo.GetHotReloadStatus();
                await httpContext.Response.WriteAsync($"event: route-changed\ndata: {System.Text.Json.JsonSerializer.Serialize(status)}\n\n");
                await httpContext.Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending route change event");
            }
        }, HttpContext);

        // Keep connection alive
        try
        {
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(1000, HttpContext.RequestAborted); // Keep connection alive
            }
        }
        finally
        {
            registration.Dispose();
            _logger.LogInformation("Client disconnected from route configuration events");
        }
    }
}