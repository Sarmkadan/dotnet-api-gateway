#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides extension methods for creating configured services used in routing and rate limiting integration tests.
/// </summary>
public static class RoutingAndRateLimitingIntegrationTestsExtensions
{
    /// <summary>
    /// Creates a pre-configured routing service with test routes for integration testing scenarios.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="routes">Routes to pre-populate.</param>
    /// <returns>Configured routing service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="routes"/> is <see langword="null"/>.</exception>
    public static RoutingService CreateConfiguredRoutingService(this RoutingAndRateLimitingIntegrationTests tests, params GatewayRoute[] routes)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentNullException.ThrowIfNull(routes);

        var repository = new GatewayRouteRepository();

        foreach (var route in routes)
        {
            repository.AddAsync(route).GetAwaiter().GetResult();
        }

        return new RoutingService(repository);
    }

    /// <summary>
    /// Creates a rate limiting service with a mock store for testing rate limit policies.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="requestLimit">Maximum allowed requests.</param>
    /// <returns>Configured rate limiting service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    public static RateLimitingService CreateRateLimitingService(this RoutingAndRateLimitingIntegrationTests tests, int requestLimit = 5)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var mockFactory = new Mock<IRateLimitStoreFactory>();
        var mockStore = new Mock<IRateLimitStore>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var requestCount = 0;
        var lockObj = new object();

        mockStore.Setup(s => s.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .Returns(async () =>
            {
                await Task.Delay(0);
                lock (lockObj)
                {
                    requestCount++;
                    return requestCount <= requestLimit;
                }
            });

        var logger = new Mock<ILogger<RateLimitingService>>();
        var rateLimitService = new RateLimitingService(mockFactory.Object, logger.Object);

        return rateLimitService;
    }

    /// <summary>
    /// Creates a circuit breaker service with pre-configured repository for testing circuit breaker behavior.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="failureThreshold">Circuit breaker failure threshold.</param>
    /// <returns>Configured circuit breaker service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    public static CircuitBreakerService CreateCircuitBreakerService(this RoutingAndRateLimitingIntegrationTests tests, int failureThreshold = 3)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);

        return service;
    }

    /// <summary>
    /// Creates a metrics service and records sample request data for testing metrics functionality.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="sampleData">Whether to populate with sample metrics data.</param>
    /// <returns>Configured metrics service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    public static MetricsService CreateMetricsService(this RoutingAndRateLimitingIntegrationTests tests, bool sampleData = true)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var metricsService = new MetricsService();

        if (sampleData)
        {
            metricsService.RecordRequest("test-route", 200, TimeSpan.FromMilliseconds(100));
            metricsService.RecordRequest("test-route", 200, TimeSpan.FromMilliseconds(150));
            metricsService.RecordRequest("test-route", 500, TimeSpan.FromMilliseconds(200));
            metricsService.RecordRequest("another-route", 200, TimeSpan.FromMilliseconds(80));
        }

        return metricsService;
    }
}