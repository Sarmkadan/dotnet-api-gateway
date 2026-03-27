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

namespace DotNetApiGateway.Tests;

public sealed class AdminDashboardSummaryTests
{
    private static MetricsService CreateMetricsService(int requests = 0, int successes = 0, int failures = 0)
    {
        var svc = new MetricsService();
        for (int i = 0; i < successes; i++)
            svc.RecordRequest("r1", 200, TimeSpan.FromMilliseconds(50));
        for (int i = 0; i < failures; i++)
            svc.RecordRequest("r1", 500, TimeSpan.FromMilliseconds(200));
        return svc;
    }

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

    [Fact]
    public void GetMetrics_NoRequests_ReturnsZeroSuccessRate()
    {
        var svc = new MetricsService();
        var metrics = svc.GetMetrics();

        metrics.TotalRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(0);
        metrics.AverageResponseTimeMs.Should().Be(0);
    }

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
