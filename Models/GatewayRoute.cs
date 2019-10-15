#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines a route configuration in the API gateway, mapping incoming request paths
/// to one or more downstream <see cref="RouteTarget"/> services. Each route can be
/// individually configured with rate limiting, circuit breaking, caching, and authentication policies.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PathPattern"/> supports simple wildcard matching:
/// <list type="bullet">
///   <item><c>*</c> matches any single path segment</item>
///   <item><c>{paramName}</c> matches any segment and captures it as a named parameter</item>
///   <item>Literal segments are matched case-insensitively</item>
/// </list>
/// </para>
/// <para>
/// A route must pass <see cref="Validate"/> before it can be used for request routing.
/// Required: non-empty <see cref="Name"/>, <see cref="PathPattern"/>, at least one
/// <see cref="AllowedMethods">allowed method</see>, and at least one <see cref="Targets">target</see>.
/// </para>
/// </remarks>
public sealed class GatewayRoute
{
    /// <summary>Unique identifier for this route (auto-generated GUID by default).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Human-readable name for identification in logs and management APIs.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL path pattern to match against incoming requests.
    /// Supports wildcards (<c>*</c>) and named parameters (<c>{id}</c>).
    /// Example: <c>/api/users/{userId}/orders/*</c>
    /// </summary>
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>HTTP methods allowed on this route (e.g. GET, POST, PUT, DELETE).</summary>
    public string[] AllowedMethods { get; set; } = [];

    /// <summary>Downstream service targets. Multiple targets enable load balancing.</summary>
    public RouteTarget[] Targets { get; set; } = [];

    /// <summary>Optional rate limiting configuration for this route.</summary>
    public RateLimitPolicy? RateLimitPolicy { get; set; }

    /// <summary>Optional circuit breaker configuration to prevent cascading failures.</summary>
    public CircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }

    /// <summary>Optional response caching configuration.</summary>
    public CachePolicy? CachePolicy { get; set; }

    /// <summary>Optional authentication requirements (JWT validation, API key, etc.).</summary>
    public AuthenticationPolicy? AuthenticationPolicy { get; set; }

    /// <summary>Whether this route is active and should be considered during request matching.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Maximum time in seconds to wait for a downstream response (1-300).</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Headers to inject into proxied requests before forwarding to targets.</summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];

    /// <summary>UTC timestamp of when this route was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last modification, or <c>null</c> if never modified.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Validates this route configuration. Throws <see cref="ArgumentException"/>
    /// with a descriptive message if any required property is missing or invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Route name cannot be empty");

        if (string.IsNullOrWhiteSpace(PathPattern))
            throw new ArgumentException("Route path pattern cannot be empty");

        if (AllowedMethods.Length == 0)
            throw new ArgumentException("Route must have at least one allowed HTTP method");

        if (Targets.Length == 0)
            throw new ArgumentException("Route must have at least one target");

        if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
            throw new ArgumentException("Timeout must be between 1 and 300 seconds");

        foreach (var target in Targets)
            target.Validate();
    }

    /// <summary>
    /// Tests whether the given request path matches this route's <see cref="PathPattern"/>.
    /// Matching is case-insensitive for literal segments. Wildcards (<c>*</c>) and
    /// named parameters (<c>{param}</c>) match any single segment.
    /// </summary>
    /// <param name="requestPath">The incoming request path (e.g. <c>/api/users/42</c>).</param>
    /// <returns><c>true</c> if the path matches the pattern.</returns>
    public bool MatchesPath(string requestPath)
    {
        // Simple pattern matching - supports wildcards
        var patternParts = PathPattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var requestParts = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (patternParts.Length != requestParts.Length)
            return false;

        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "*")
                continue;

            if (patternParts[i].StartsWith("{") && patternParts[i].EndsWith("}"))
                continue;

            if (!patternParts[i].Equals(requestParts[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether this route allows the specified HTTP method (case-insensitive comparison).
    /// </summary>
    /// <param name="method">The HTTP method to check (e.g. "GET", "POST").</param>
    /// <returns><c>true</c> if the method is in <see cref="AllowedMethods"/>.</returns>
    public bool SupportsMethod(string method)
    {
        return AllowedMethods.Any(m => m.Equals(method, StringComparison.OrdinalIgnoreCase));
    }
}
