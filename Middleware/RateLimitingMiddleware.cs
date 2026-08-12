#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.RateLimiting;
using DotNetApiGateway.Constants;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Middleware for enforcing rate limiting policies on incoming requests using System.Threading.RateLimiting.
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        System.ArgumentNullException.ThrowIfNull(next);
        System.ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RoutingService routingService, RateLimitingService rateLimitingService)
    {
        System.ArgumentNullException.ThrowIfNull(context);
        System.ArgumentNullException.ThrowIfNull(routingService);
        System.ArgumentNullException.ThrowIfNull(rateLimitingService);

        _logger.LogInformation("Processing rate limiting for request {Path}", context.Request.Path);

        try
        {
            if (!context.Items.TryGetValue("GatewayRoute", out var routeObj) || routeObj is not GatewayRoute route)
            {
                _logger.LogInformation("Bypassing rate limiting: no route found for {Path}", context.Request.Path);
                await _next(context); // No route found, bypass rate limiting
                return;
            }

            if (route.RateLimitPolicy is null || !route.RateLimitPolicy.Enabled)
            {
                _logger.LogInformation("Bypassing rate limiting: policy not enabled for route {RouteId}", route.Id);
                await _next(context); // Rate limiting not enabled for this route
                return;
            }

            var policy = route.RateLimitPolicy;
            var key = GenerateRateLimitKey(context, policy);

            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Could not generate rate limit key for request to {Path}. Bypassing rate limit.", context.Request.Path);
                await _next(context);
                return;
            }

            if (policy.BypassForAuthenticatedUsers && context.User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Bypassing rate limiting: authenticated user {User}", context.User.Identity?.Name ?? "unknown");
                await _next(context); // Bypass for authenticated users
                return;
            }

            var isAllowed = await rateLimitingService.IsAllowedAsync(key, policy);

            if (!isAllowed)
            {
                var info = await rateLimitingService.GetRateLimitInfoAsync(key, policy);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Standard headers (RFC 6585)
                context.Response.Headers["Retry-After"] = info.Reset.ToString();
                context.Response.Headers["RateLimit-Limit"] = info.Limit.ToString();
                context.Response.Headers["RateLimit-Remaining"] = info.Remaining.ToString();

                // Existing custom gateway headers (preserved for backward compatibility)
                context.Response.Headers[GatewayConstants.RateLimitHeader] = info.Limit.ToString();
                context.Response.Headers[GatewayConstants.RateLimitRemainingHeader] = info.Remaining.ToString();
                context.Response.Headers[GatewayConstants.RateLimitResetHeader] = info.Reset.ToString();

                await context.Response.WriteAsync("Too Many Requests");
                _logger.LogWarning("Rate limit exceeded for key {Key} on route {RouteId}. Limit: {Limit}, Remaining: {Remaining}",
                    key, route.Id, info.Limit, info.Remaining);
                return;
            }

            // Set response headers for allowed requests
            var allowedInfo = await rateLimitingService.GetRateLimitInfoAsync(key, policy);

            // Standard headers (RFC 6585)
            context.Response.Headers["RateLimit-Limit"] = allowedInfo.Limit.ToString();
            context.Response.Headers["RateLimit-Remaining"] = allowedInfo.Remaining.ToString();
            context.Response.Headers["Retry-After"] = allowedInfo.Reset.ToString();

            // Existing custom gateway headers (preserved for backward compatibility)
            context.Response.Headers[GatewayConstants.RateLimitHeader] = allowedInfo.Limit.ToString();
            context.Response.Headers[GatewayConstants.RateLimitRemainingHeader] = allowedInfo.Remaining.ToString();
            context.Response.Headers[GatewayConstants.RateLimitResetHeader] = allowedInfo.Reset.ToString();

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing rate limiting for request {Path}", context.Request.Path);
            throw;
        }
    }

    private string GenerateRateLimitKey(HttpContext context, RateLimitPolicy policy)
    {
        return policy.KeyGenerator.ToLowerInvariant() switch
        {
            "clientip" => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "authenticateduser" => context.User.Identity?.Name ?? "anonymous",
            "routeid" => ((GatewayRoute)context.Items["GatewayRoute"]!).Id,
            var s when s.StartsWith("customheader:") => context.Request.Headers[s.Substring(13)].ToString(),
            _ => "global" // Default fallback key
        };
    }
}

/// <summary>
/// Extension method for adding rate limiting middleware to the pipeline.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder builder)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
