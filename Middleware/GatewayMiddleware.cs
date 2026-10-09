#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Middleware for processing requests through the API gateway.
/// Builds a <see cref="RequestContext"/> from the incoming request, stores it in
/// <see cref="HttpContext.Items"/> for downstream middleware, logs the request lifecycle,
/// and records request metrics once the response has been produced.
/// </summary>
public sealed class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the ASP.NET Core request pipeline.</param>
    /// <param name="logger">Logger used to record request received, completed, and failed events.</param>
    public GatewayMiddleware(RequestDelegate next, ILogger<GatewayMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming HTTP request. Creates a <see cref="RequestContext"/> containing the
    /// request ID, path, method, client IP, headers, query parameters, and auth token, then invokes
    /// the rest of the pipeline. After the pipeline completes, the matched route ID (or
    /// <see cref="GatewayConstants.UnknownValue"/> when no route was matched) and the response status
    /// are recorded via <paramref name="metricsService"/>.
    /// </summary>
    /// <param name="context">The current HTTP context for the request.</param>
    /// <param name="routingService">Routing service injected per request. Validated for null; not used directly by this method.</param>
    /// <param name="metricsService">Metrics service used to record the request outcome and duration.</param>
    /// <returns>A task that completes when the downstream pipeline has finished processing the request.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/>, <paramref name="routingService"/>, or <paramref name="metricsService"/> is null.
    /// </exception>
    /// <remarks>
    /// Exceptions raised by downstream middleware are logged as errors and rethrown unchanged, so that
    /// exception handling middleware can map them to HTTP responses.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, RoutingService routingService, MetricsService metricsService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(routingService);
        ArgumentNullException.ThrowIfNull(metricsService);
        var requestContext = new RequestContext
        {
            RequestId = Guid.NewGuid().ToString(),
            Path = context.Request.Path.Value ?? GatewayConstants.DefaultRequestPath,
            Method = context.Request.Method,
            ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? GatewayConstants.UnknownValue,
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
        if (context.Request.Headers.TryGetValue(GatewayConstants.AuthorizationHeader, out var authHeader))
        {
            requestContext.AuthToken = authHeader.ToString();
        }

        // Add request context to items for downstream middleware
        context.Items[GatewayConstants.RequestContextItemKey] = requestContext;

        _logger.LogInformation(
            "Gateway received request: {Method} {Path} from {ClientIp} [RequestId: {RequestId}]",
            requestContext.Method,
            requestContext.Path,
            requestContext.ClientIp,
            requestContext.RequestId);

        try
        {
            await _next(context);

            var routeId = context.Items.TryGetValue(GatewayConstants.GatewayRouteItemKey, out var routeObj) && routeObj is GatewayRoute r
                ? r.Id
                : GatewayConstants.UnknownValue;
            metricsService.RecordRequest(routeId, context.Response.StatusCode, requestContext.ElapsedTime());

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
/// Extension methods for adding <see cref="GatewayMiddleware"/> to the pipeline
/// </summary>
public static class GatewayMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="GatewayMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance, for chaining.</returns>
    public static IApplicationBuilder UseGatewayMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GatewayMiddleware>();
    }
}
