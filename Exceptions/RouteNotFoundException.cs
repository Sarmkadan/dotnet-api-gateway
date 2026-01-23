// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Thrown when a requested route cannot be found or matched
/// </summary>
public class RouteNotFoundException : GatewayException
{
    public string RequestPath { get; set; }
    public string? HttpMethod { get; set; }

    public RouteNotFoundException(
        string requestPath,
        string? httpMethod = null)
        : base(
            $"Route not found: {httpMethod ?? "ANY"} {requestPath}",
            "INVALID_ROUTE",
            404)
    {
        RequestPath = requestPath;
        HttpMethod = httpMethod;
    }
}
