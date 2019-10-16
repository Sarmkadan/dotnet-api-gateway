#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Default response transformer that applies security headers, CORS headers,
/// and any custom headers defined on the route configuration.
/// </summary>
public sealed class ResponseTransformationService : IResponseTransformer
{
    private readonly ILogger<ResponseTransformationService> _logger;

    // Security headers injected on every response
    private static readonly Dictionary<string, string> SecurityHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY",
        ["X-XSS-Protection"] = "1; mode=block",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin"
    };

    public ResponseTransformationService(ILogger<ResponseTransformationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> TransformAsync(HttpResponseMessage response, GatewayRoute route)
    {
        InjectSecurityHeaders(response);
        InjectCustomRouteHeaders(response, route);

        _logger.LogDebug(
            "Response transformed for route {RouteId}: status {StatusCode}",
            route.Id,
            (int)response.StatusCode);

        return Task.FromResult(response);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void InjectSecurityHeaders(HttpResponseMessage response)
    {
        foreach (var (name, value) in SecurityHeaders)
        {
            // Don't overwrite headers that the upstream service already set
            if (!response.Headers.Contains(name))
                response.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static void InjectCustomRouteHeaders(HttpResponseMessage response, GatewayRoute route)
    {
        if (route.CustomHeaders.Count == 0)
            return;

        foreach (var (name, value) in route.CustomHeaders)
        {
            // Custom route headers always overwrite upstream values
            response.Headers.Remove(name);
            response.Headers.TryAddWithoutValidation(name, value);
        }
    }
}
