#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Middleware for comprehensive request and response logging.
/// Logs all incoming requests with headers, body, and response details for audit trails.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _enableBodyLogging;
    private readonly string[] _excludedPaths = new[] { "/health", "/metrics" };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, bool enableBodyLogging = false)
    {
        _next = next;
        _logger = logger;
        _enableBodyLogging = enableBodyLogging;
    }

    /// <summary>
    /// Intercept request, log details, and track response.
    /// Captures request/response timing and status for observability.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health check and metrics endpoints
        if (_excludedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid().ToString("N");
        context.Items["RequestId"] = requestId;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Log incoming request
        _logger.LogInformation(
            "Request received [ID: {RequestId}] {Method} {Path} from {RemoteIp}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        // Log request headers (excluding sensitive ones)
        var sensitiveHeaders = new[] { "Authorization", "X-API-Key", "Cookie" };
        var safeHeaders = context.Request.Headers
            .Where(h => !sensitiveHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => (object)h.Value.ToString());

        _logger.LogDebug("Request headers: {@Headers}", safeHeaders);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Log response
            _logger.LogInformation(
                "Response sent [ID: {RequestId}] {StatusCode} completed in {ElapsedMs}ms",
                requestId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            // Copy response body back
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request failed [ID: {RequestId}] after {ElapsedMs}ms",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
