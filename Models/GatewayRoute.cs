// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents a route configuration in the API gateway
/// </summary>
public class GatewayRoute
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string PathPattern { get; set; } = string.Empty;
    public string[] AllowedMethods { get; set; } = [];
    public RouteTarget[] Targets { get; set; } = [];
    public RateLimitPolicy? RateLimitPolicy { get; set; }
    public CircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }
    public CachePolicy? CachePolicy { get; set; }
    public AuthenticationPolicy? AuthenticationPolicy { get; set; }
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

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

    public bool SupportsMethod(string method)
    {
        return AllowedMethods.Any(m => m.Equals(method, StringComparison.OrdinalIgnoreCase));
    }
}
