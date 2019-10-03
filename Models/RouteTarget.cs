// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents a backend service target for a route
/// </summary>
public class RouteTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
    public bool IsHealthy { get; set; } = true;
    public string? HealthCheckPath { get; set; }
    public int HealthCheckIntervalSeconds { get; set; } = 60;
    public Dictionary<string, string> TransformHeaders { get; set; } = [];
    public bool StripPathPrefix { get; set; } = false;
    public DateTime LastHealthCheckAt { get; set; }
    public string? LastHealthCheckError { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Target name cannot be empty");

        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("Target base URL cannot be empty");

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException($"Invalid URL format: {BaseUrl}");

        if (Weight < 1 || Weight > 100)
            throw new ArgumentException("Target weight must be between 1 and 100");

        if (HealthCheckIntervalSeconds < 10 || HealthCheckIntervalSeconds > 600)
            throw new ArgumentException("Health check interval must be between 10 and 600 seconds");
    }

    public string GetForwardUrl(string requestPath)
    {
        var targetPath = StripPathPrefix ? requestPath : requestPath;
        var baseUri = new Uri(BaseUrl);
        var fullUrl = new Uri(baseUri, targetPath);
        return fullUrl.ToString();
    }

    public void UpdateHealthStatus(bool isHealthy, string? error = null)
    {
        IsHealthy = isHealthy;
        LastHealthCheckAt = DateTime.UtcNow;
        LastHealthCheckError = error;
    }
}
