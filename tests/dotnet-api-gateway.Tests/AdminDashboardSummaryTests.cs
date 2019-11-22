#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace dotnet_api_gateway.Tests;

/// <summary>
/// Represents a test fixture for admin dashboard summary data used in JSON serialization tests.
/// </summary>
public sealed class AdminDashboardSummaryTests
{
    /// <summary>
    /// Gets or sets the gateway information.
    /// </summary>
    public required GatewayInfo Gateway { get; set; }

    /// <summary>
    /// Gets or sets the request metrics.
    /// </summary>
    public required RequestMetrics Requests { get; set; }

    /// <summary>
    /// Gets or sets the route statistics.
    /// </summary>
    public required RouteStats Routes { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker statistics.
    /// </summary>
    public required CircuitBreakerStats CircuitBreakers { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code distribution.
    /// </summary>
    public required Dictionary<int, long> StatusCodeDistribution { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the summary was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Represents gateway metadata.
    /// </summary>
    public sealed class GatewayInfo
    {
        /// <summary>
        /// Gets or sets the gateway name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the gateway version.
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Gets or sets the gateway uptime.
        /// </summary>
        public required string Uptime { get; set; }

        /// <summary>
        /// Gets or sets when the gateway was started.
        /// </summary>
        public required DateTime StartedAt { get; set; }
    }

    /// <summary>
    /// Represents request metrics.
    /// </summary>
    public sealed class RequestMetrics
    {
        /// <summary>
        /// Gets or sets the total number of requests.
        /// </summary>
        public required long Total { get; set; }

        /// <summary>
        /// Gets or sets the number of successful requests.
        /// </summary>
        public required long Successful { get; set; }

        /// <summary>
        /// Gets or sets the number of failed requests.
        /// </summary>
        public required long Failed { get; set; }

        /// <summary>
        /// Gets or sets the success rate percentage.
        /// </summary>
        public required double SuccessRatePercent { get; set; }

        /// <summary>
        /// Gets or sets the average response time in milliseconds.
        /// </summary>
        public required double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the requests per second.
        /// </summary>
        public required double RequestsPerSecond { get; set; }
    }

    /// <summary>
    /// Represents route statistics.
    /// </summary>
    public sealed class RouteStats
    {
        /// <summary>
        /// Gets or sets the total number of routes.
        /// </summary>
        public required int Total { get; set; }

        /// <summary>
        /// Gets or sets the number of active routes.
        /// </summary>
        public required int Active { get; set; }

        /// <summary>
        /// Gets or sets the number of inactive routes.
        /// </summary>
        public required int Inactive { get; set; }
    }

    /// <summary>
    /// Represents circuit breaker statistics.
    /// </summary>
    public sealed class CircuitBreakerStats
    {
        /// <summary>
        /// Gets or sets the total number of circuit breakers.
        /// </summary>
        public required int Total { get; set; }

        /// <summary>
        /// Gets or sets the number of open circuit breakers.
        /// </summary>
        public required int Open { get; set; }

        /// <summary>
        /// Gets or sets the number of half-open circuit breakers.
        /// </summary>
        public required int HalfOpen { get; set; }

        /// <summary>
        /// Gets or sets the number of closed circuit breakers.
        /// </summary>
        public required int Closed { get; set; }
    }
}
