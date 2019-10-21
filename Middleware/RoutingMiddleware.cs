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

    public async Task InvokeAsync(HttpContext context, RoutingService routingService, ApiVersioningService apiVersioningService)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        try
        {
            var route = await routingService.FindRouteAsync(path, method);

            if (route is not null)
            {
                // Validate API versioning policy when configured
                if (route.VersioningPolicy?.Enabled == true)
                {
                    if (!apiVersioningService.TryResolveVersion(context, route.VersioningPolicy, out var version))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsJsonAsync(
                            ApiVersioningService.BuildVersionErrorResponse(route.VersioningPolicy, null));
                        return;
                    }

                    context.Items["ApiVersion"] = version;

                    // Strip version segment from path so backends don't see it
                    if (route.VersioningPolicy.StripVersionFromPath && version is not null)
                    {
                        var strippedPath = apiVersioningService.StripVersionFromPath(path, route.VersioningPolicy);
                        context.Items["StrippedPath"] = strippedPath;
                        _logger.LogDebug("Version {Version} resolved; path stripped to {StrippedPath}", version, strippedPath);
                    }
                }

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
