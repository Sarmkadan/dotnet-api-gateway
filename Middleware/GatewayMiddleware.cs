// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Middleware for processing requests through the API gateway
/// </summary>
public class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayMiddleware> _logger;

    public GatewayMiddleware(RequestDelegate next, ILogger<GatewayMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RoutingService routingService)
    {
        var requestContext = new RequestContext
        {
            RequestId = Guid.NewGuid().ToString(),
            Path = context.Request.Path.Value ?? "/",
            Method = context.Request.Method,
            ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ReceivedAt = DateTime.UtcNow
        };

        // Extract headers
        foreach (var header in context.Request.Headers)
        {
            requestContext.Headers[header.Key] = header.Value.ToString();
        }

        // Extract query parameters
        foreach (var query in context.Request.Query)
        {
            requestContext.QueryParameters[query.Key] = query.Value.ToString();
        }

        // Extract auth token
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            requestContext.AuthToken = authHeader.ToString();
        }

        // Add request context to items for downstream middleware
        context.Items["RequestContext"] = requestContext;

        _logger.LogInformation(
            "Gateway received request: {Method} {Path} from {ClientIp} [RequestId: {RequestId}]",
            requestContext.Method,
            requestContext.Path,
            requestContext.ClientIp,
            requestContext.RequestId);

        try
        {
            await _next(context);

            _logger.LogInformation(
                "Request completed: {RequestId} Status: {StatusCode} Duration: {ElapsedMs}ms",
                requestContext.RequestId,
                context.Response.StatusCode,
                requestContext.ElapsedTime().TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing request {RequestId}: {ErrorMessage}",
                requestContext.RequestId,
                ex.Message);

            throw;
        }
    }
}

/// <summary>
/// Extension method for adding gateway middleware to the pipeline
/// </summary>
public static class GatewayMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GatewayMiddleware>();
    }
}
