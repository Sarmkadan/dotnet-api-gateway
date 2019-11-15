#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Contains unit tests for the AdminDashboardSummary class.
/// </summary>
public sealed class AdminDashboardSummaryTests
{
    /// <summary>
    /// Creates a new instance of the MetricsService class with the specified number of requests, successes, and failures.
    /// </summary>
    /// <param name="requests">The total number of requests.</param>
    /// <param name="successes">The number of successful requests.</param>
    /// <param name="failures">The number of failed requests.</param>
    /// <returns>A new instance of the MetricsService class.</returns>
    private static MetricsService CreateMetricsService(int requests = 0, int successes = 0, int failures = 0)
    {
        var svc = new MetricsService();
        for (int i = 0; i < successes; i++)
            svc.RecordRequest("r1", 200, TimeSpan.FromMilliseconds(50));
        for (int i = 0; i < failures; i++)
            svc.RecordRequest("r1", 500, TimeSpan.FromMilliseconds(200));
        return svc;
    }

    /// <summary>
    /// Verifies that the GetMetrics method returns the correct counts after recording requests.
    /// </summary>
    [Fact]
    public void GetMetrics_AfterRequests_ReportsCorrectCounts()
    {
        var svc = CreateMetricsService(successes: 8, failures: 2);
        var metrics = svc.GetMetrics();

        metrics.TotalRequests.Should().Be(10);
        metrics.SuccessfulRequests.Should().Be(8);
        metrics.FailedRequests.Should().Be(2);
        metrics.SuccessRate.Should().BeApproximately(80.0, 0.01);
    }

    /// <summary>
    /// Verifies that the GetMetrics method returns zero success rate when no requests have been recorded.
    /// </summary>
    [Fact]
    public void GetMetrics_NoRequests_ReturnsZeroSuccessRate()
    {
        var svc = new MetricsService();
        var metrics = svc.GetMetrics();

        metrics.TotalRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(0);
        metrics.AverageResponseTimeMs.Should().Be(0);
    }

    /// <summary>
    /// Verifies that the GetMetrics method tracks the status code distribution correctly.
    /// </summary>
    [Fact]
    public void GetMetrics_StatusCodeDistribution_TracksEachCode()
    {
        var svc = new MetricsService();
        svc.RecordRequest("r1", 200, TimeSpan.FromMilliseconds(10));
        svc.RecordRequest("r1", 200, TimeSpan.FromMilliseconds(10));
        svc.RecordRequest("r1", 404, TimeSpan.FromMilliseconds(5));

        var metrics = svc.GetMetrics();

        metrics.StatusCodeDistribution.Should().ContainKey(200).WhoseValue.Should().Be(2);
        metrics.StatusCodeDistribution.Should().ContainKey(404).WhoseValue.Should().Be(1);
    }

    /// <summary>
    /// Verifies that the GetMetrics method calculates the per-route average response time correctly.
    /// </summary>
    [Fact]
    public void GetMetrics_RouteMetrics_CalculatesPerRouteAverage()
    {
        var svc = new MetricsService();
        svc.RecordRequest("route-a", 200, TimeSpan.FromMilliseconds(100));
        svc.RecordRequest("route-a", 200, TimeSpan.FromMilliseconds(300));

        var metrics = svc.GetMetrics();
        var routeMetric = metrics.RouteMetrics.Single(r => r.RouteId == "route-a");

        routeMetric.GetAverageResponseTime().Should().BeApproximately(200.0, 0.01);
    }
}
