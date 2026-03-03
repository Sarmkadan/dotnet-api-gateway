#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DotNetApiGateway.Exceptions;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Middleware for resolving the GatewayRoute based on the incoming request path and method.
/// Stores the resolved route in HttpContext.Items for subsequent middleware.
/// </summary>
public sealed class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoutingMiddleware> _logger;

    public RoutingMiddleware(RequestDelegate next, ILogger<RoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RoutingService routingService)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        try
        {
            var route = await routingService.FindRouteAsync(path, method);

            if (route is not null)
            {
                context.Items["GatewayRoute"] = route;
                _logger.LogDebug("Route '{RouteId}' found for {Method} {Path}", route.Id, method, path);
            }
            else
            {
                _logger.LogDebug("No route found for {Method} {Path}", method, path);
            }
        }
        catch (RouteNotFoundException ex)
        {
            _logger.LogWarning(ex, "Route not found exception for {Method} {Path}", method, path);
            context.Items["RouteNotFoundException"] = ex; // Store exception for later handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during route resolution for {Method} {Path}", method, path);
            context.Items["RouteResolutionError"] = ex; // Store error for later handling
        }

        await _next(context); // Pass control to the next middleware
    }
}

/// <summary>
/// Extension method for adding routing middleware to the pipeline.
/// </summary>
public static class RoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseRoutingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoutingMiddleware>();
    }
}
