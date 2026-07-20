#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;

/// <summary>
/// Circuit breaker management endpoints for monitoring and controlling circuit breaker states.
/// Provides endpoints to inspect circuit breaker statuses across all services.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CircuitBreakerController : ControllerBase
{
    private readonly CircuitBreakerService _circuitBreakerService;
    private readonly ILogger<CircuitBreakerController> _logger;

    public CircuitBreakerController(
        CircuitBreakerService circuitBreakerService,
        ILogger<CircuitBreakerController> logger)
    {
        _circuitBreakerService = circuitBreakerService;
        _logger = logger;
    }

    /// <summary>
    /// Get all circuit breaker statuses across all services.
    /// Returns detailed information including state, failure count, and last transition time.
    /// </summary>
    /// <returns>Collection of all circuit breaker statuses with detailed information.</returns>
    [HttpGet("statuses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAllCircuitBreakerStatuses()
    {
        var statuses = (await _circuitBreakerService.GetAllStatusesAsync()).ToList();

        if (statuses.Count == 0)
        {
            _logger.LogInformation("No circuit breakers found");
            return NoContent();
        }

        _logger.LogInformation("Retrieved {Count} circuit breaker statuses", statuses.Count);
        return Ok(statuses);
    }

    /// <summary>
    /// Get circuit breaker status for a specific service.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <returns>Circuit breaker status for the specified service, or 404 if not found.</returns>
    [HttpGet("statuses/{serviceName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCircuitBreakerStatus(string serviceName)
    {
        var status = await _circuitBreakerService.GetStatusAsync(serviceName);

        if (status is null)
        {
            _logger.LogWarning("Circuit breaker status not found for service: {ServiceName}", serviceName);
            return NotFound(new { error = "Circuit breaker not found", serviceName });
        }

        _logger.LogDebug("Retrieved circuit breaker status for service: {ServiceName}", serviceName);
        return Ok(status);
    }

    /// <summary>
    /// Get all circuit breakers currently in the Open state.
    /// </summary>
    /// <returns>Collection of open circuit breaker statuses.</returns>
    [HttpGet("statuses/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetOpenCircuits()
    {
        var openCircuits = (await _circuitBreakerService.GetOpenCircuitsAsync()).ToList();

        if (openCircuits.Count == 0)
        {
            _logger.LogInformation("No open circuit breakers found");
            return NoContent();
        }

        _logger.LogInformation("Retrieved {Count} open circuit breakers", openCircuits.Count);
        return Ok(openCircuits);
    }

    /// <summary>
    /// Get all circuit breakers currently in the Half-Open state.
    /// </summary>
    /// <returns>Collection of half-open circuit breaker statuses.</returns>
    [HttpGet("statuses/half-open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetHalfOpenCircuits()
    {
        var halfOpenCircuits = (await _circuitBreakerService.GetAllStatusesAsync())
            .Where(cb => cb.State == CircuitBreakerState.HalfOpen)
            .ToList();

        if (halfOpenCircuits.Count == 0)
        {
            _logger.LogInformation("No half-open circuit breakers found");
            return NoContent();
        }

        _logger.LogInformation("Retrieved {Count} half-open circuit breakers", halfOpenCircuits.Count);
        return Ok(halfOpenCircuits);
    }

    /// <summary>
    /// Get all circuit breakers currently in the Closed state.
    /// </summary>
    /// <returns>Collection of closed circuit breaker statuses.</returns>
    [HttpGet("statuses/closed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetClosedCircuits()
    {
        var closedCircuits = (await _circuitBreakerService.GetAllStatusesAsync())
            .Where(cb => cb.State == CircuitBreakerState.Closed)
            .ToList();

        if (closedCircuits.Count == 0)
        {
            _logger.LogInformation("No closed circuit breakers found");
            return NoContent();
        }

        _logger.LogInformation("Retrieved {Count} closed circuit breakers", closedCircuits.Count);
        return Ok(closedCircuits);
    }

    /// <summary>
    /// Reset a specific circuit breaker to the Closed state.
    /// Clears all failure and success counters.
    /// </summary>
    /// <param name="serviceName">The downstream service name.</param>
    /// <returns>Success confirmation or 404 if circuit breaker not found.</returns>
    [HttpPost("statuses/{serviceName}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetCircuit(string serviceName)
    {
        var status = await _circuitBreakerService.GetStatusAsync(serviceName);

        if (status is null)
        {
            _logger.LogWarning("Cannot reset circuit breaker - not found for service: {ServiceName}", serviceName);
            return NotFound(new { error = "Circuit breaker not found", serviceName });
        }

        await _circuitBreakerService.ResetCircuitAsync(serviceName);
        _logger.LogInformation("Circuit breaker for service {ServiceName} manually reset to Closed state", serviceName);
        return Ok(new { message = "Circuit breaker reset successfully", serviceName, previousState = status.State });
    }

    /// <summary>
    /// Reset all circuit breakers to the Closed state.
    /// Clears all failure and success counters across all services.
    /// </summary>
    /// <returns>Success confirmation.</returns>
    [HttpPost("statuses/reset-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllCircuits()
    {
        await _circuitBreakerService.ResetAllCircuitsAsync();
        _logger.LogInformation("All circuit breakers manually reset to Closed state");
        return Ok(new { message = "All circuit breakers reset successfully" });
    }
}
