#nullable enable

namespace DotNetApiGateway.Models;

/// <summary>
/// Extension methods for <see cref="RouteTarget"/> providing additional functionality
/// </summary>
public static class RouteTargetExtensions
{
    /// <summary>
    /// Gets the effective base URL including port if specified
    /// </summary>
    /// <param name="target">The route target</param>
    /// <returns>Base URL with port if port is set</returns>
    public static string GetEffectiveBaseUrl(this RouteTarget target)
    {
        if (target.Port == null || target.Port == 80 || target.Port == 443)
        {
            return target.BaseUrl;
        }

        var uri = new Uri(target.BaseUrl);
        var builder = new UriBuilder(uri)
        {
            Port = target.Port.Value
        };
        return builder.Uri.ToString();
    }

    /// <summary>
    /// Calculates the effective timeout in milliseconds for this target
    /// </summary>
    /// <param name="target">The route target</param>
    /// <returns>Timeout in milliseconds, or null if not set</returns>
    public static int? GetTimeoutMilliseconds(this RouteTarget target)
    {
        return target.TimeoutSeconds.HasValue
            ? target.TimeoutSeconds.Value * 1000
            : null;
    }

    /// <summary>
    /// Determines if this target should be considered for traffic based on health status
    /// </summary>
    /// <param name="target">The route target</param>
    /// <param name="currentTime">Current UTC time to check against health check interval</param>
    /// <returns>True if target should receive traffic, false otherwise</returns>
    public static bool ShouldReceiveTraffic(this RouteTarget target, DateTime currentTime)
    {
        if (target.IsHealthy)
        {
            return true;
        }

        // If unhealthy, check if enough time has passed since last health check
        if (target.LastHealthCheckAt == default)
        {
            return false; // Never checked
        }

        var timeSinceLastCheck = currentTime - target.LastHealthCheckAt;
        return timeSinceLastCheck.TotalSeconds >= target.HealthCheckIntervalSeconds;
    }

    /// <summary>
    /// Gets a normalized weight value for consistent load balancing calculations
    /// </summary>
    /// <param name="target">The route target</param>
    /// <returns>Normalized weight value</returns>
    public static double GetNormalizedWeight(this RouteTarget target)
    {
        return target.Weight / 100.0;
    }
}