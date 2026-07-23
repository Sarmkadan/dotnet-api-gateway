#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents a backend service target for a route
/// </summary>
public sealed class RouteTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int? Port { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int Weight { get; set; } = 1;
    public bool IsHealthy { get; set; } = true;
    public string? HealthCheckPath { get; set; }
    public int HealthCheckIntervalSeconds { get; set; } = 60;
    public Dictionary<string, string> TransformHeaders { get; set; } = [];
    public bool StripPathPrefix { get; set; } = false;
    public DateTime LastHealthCheckAt { get; set; }
    public string? LastHealthCheckError { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker state for this specific endpoint.
    /// When null, no circuit breaker is configured for this endpoint.
    /// </summary>
    public CircuitBreakerState? CircuitState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this endpoint's circuit was last opened.
    /// </summary>
    public DateTime? CircuitOpenedAt { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker timeout in seconds for this endpoint.
    /// </summary>
    public int CircuitTimeoutSeconds { get; set; } = 60;

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

    /// <summary>
    /// Builds the absolute backend URL for the given request path.
    /// When <see cref="StripPathPrefix"/> is set, the first path segment
    /// (the gateway route prefix) is removed before forwarding.
    /// </summary>
    public string GetForwardUrl(string requestPath)
    {
        ArgumentNullException.ThrowIfNull(requestPath);

        var targetPath = StripPathPrefix ? RemoveFirstPathSegment(requestPath) : requestPath;
        var baseUri = new Uri(BaseUrl);
        var fullUrl = new Uri(baseUri, targetPath);
        return fullUrl.ToString();
    }

    private static string RemoveFirstPathSegment(string path)
    {
        var queryIndex = path.IndexOfAny(['?', '#']);
        var pathPart = queryIndex < 0 ? path : path[..queryIndex];
        var suffix = queryIndex < 0 ? string.Empty : path[queryIndex..];

        var trimmed = pathPart.TrimStart('/');
        var separatorIndex = trimmed.IndexOf('/');
        var remainder = separatorIndex < 0 ? "/" : trimmed[separatorIndex..];
        return remainder + suffix;
    }

    public void UpdateHealthStatus(bool isHealthy, string? error = null)
    {
        IsHealthy = isHealthy;
        LastHealthCheckAt = DateTime.UtcNow;
        LastHealthCheckError = error;
    }

    /// <summary>
    /// Checks if this endpoint's circuit breaker is currently open.
    /// </summary>
    /// <returns>True if the circuit is open and requests should be blocked.</returns>
    public bool IsCircuitOpen(int circuitTimeoutSeconds)
    {
        if (CircuitState == CircuitBreakerState.Open)
        {
            var timeSinceOpen = DateTime.UtcNow - CircuitOpenedAt.GetValueOrDefault();
            return timeSinceOpen.TotalSeconds < circuitTimeoutSeconds;
        }
        return false;
    }

    /// <summary>
    /// Records a successful request for this endpoint.
    /// </summary>
    public void RecordCircuitSuccess()
    {
        if (CircuitState == CircuitBreakerState.HalfOpen)
        {
            CircuitState = CircuitBreakerState.Closed;
            CircuitOpenedAt = null;
        }
        else if (CircuitState == CircuitBreakerState.Closed)
        {
            // In closed state, we can reset failure count
            // Note: We don't track failure count per endpoint in this simple model
        }
    }

    /// <summary>
    /// Records a failed request for this endpoint.
    /// </summary>
    /// <param name="circuitTimeoutSeconds">The circuit breaker timeout in seconds.</param>
    public void RecordCircuitFailure(int circuitTimeoutSeconds)
    {
        if (CircuitState == CircuitBreakerState.HalfOpen)
        {
            // Single failure reopens the circuit
            CircuitState = CircuitBreakerState.Open;
            CircuitOpenedAt = DateTime.UtcNow;
        }
        else if (CircuitState == CircuitBreakerState.Closed)
        {
            // For now, we'll just trip the circuit immediately on first failure
            // In a more sophisticated implementation, we'd track failure count
            CircuitState = CircuitBreakerState.Open;
            CircuitOpenedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Changes the circuit state for this endpoint.
    /// </summary>
    public void ChangeCircuitState(CircuitBreakerState newState)
    {
        if (CircuitState != newState)
        {
            CircuitState = newState;
            if (newState == CircuitBreakerState.Open)
            {
                CircuitOpenedAt = DateTime.UtcNow;
            }
            else if (newState == CircuitBreakerState.Closed)
            {
                CircuitOpenedAt = null;
            }
        }
    }
}
