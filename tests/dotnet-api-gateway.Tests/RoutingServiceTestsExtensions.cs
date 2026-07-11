#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using System;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Extension methods for creating test routes and targets in routing service tests
/// </summary>
public static class RoutingServiceTestsExtensions
{
    /// <summary>
    /// Creates a test route with healthy targets for testing routing scenarios
    /// </summary>
    /// <param name="tests">The test class instance</param>
    /// <param name="name">Route name</param>
    /// <param name="pathPattern">URL path pattern</param>
    /// <param name="targetCount">Number of targets to create</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace</exception>
    /// <exception cref="ArgumentException"><paramref name="pathPattern"/> is null or whitespace</exception>
    /// <returns>Created GatewayRoute</returns>
    public static GatewayRoute CreateTestRoute(
        this RoutingServiceTests tests,
        string name = "TestRoute",
        string pathPattern = "/api/test",
        int targetCount = 1)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathPattern);

        if (targetCount < 1)
        {
            throw new ArgumentException("Target count must be at least 1", nameof(targetCount));
        }

        var repository = new GatewayRouteRepository();
        var targets = new List<RouteTarget>(targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            targets.Add(new RouteTarget
            {
                Name = $"backend-{i + 1}",
                BaseUrl = $"http://backend-{i + 1}:8080",
                IsHealthy = true,
                Weight = i + 1,
                HealthCheckIntervalSeconds = 60
            });
        }

        var route = new GatewayRoute
        {
            Name = name,
            PathPattern = pathPattern,
            AllowedMethods = ["GET", "POST"],
            Targets = targets.ToArray(),
            TimeoutSeconds = 30
        };

        repository.AddAsync(route).GetAwaiter().GetResult();
        return route;
    }

    /// <summary>
    /// Creates multiple routes with different configurations for comprehensive testing
    /// </summary>
    /// <param name="tests">The test class instance</param>
    /// <param name="routeCount">Number of routes to create</param>
    /// <param name="targetsPerRoute">Number of targets per route</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="routeCount"/> must be positive</exception>
    /// <exception cref="ArgumentException"><paramref name="targetsPerRoute"/> must be positive</exception>
    /// <returns>List of created GatewayRoute objects</returns>
    public static List<GatewayRoute> CreateMultipleTestRoutes(
        this RoutingServiceTests tests,
        int routeCount = 3,
        int targetsPerRoute = 2)
    {
        ArgumentNullException.ThrowIfNull(tests);

        if (routeCount < 1)
        {
            throw new ArgumentException("Route count must be at least 1", nameof(routeCount));
        }

        if (targetsPerRoute < 1)
        {
            throw new ArgumentException("Targets per route must be at least 1", nameof(targetsPerRoute));
        }

        var repository = new GatewayRouteRepository();
        var routes = new List<GatewayRoute>(routeCount);

        for (int r = 0; r < routeCount; r++)
        {
            var targets = new List<RouteTarget>(targetsPerRoute);
            for (int t = 0; t < targetsPerRoute; t++)
            {
                targets.Add(new RouteTarget
                {
                    Name = $"backend-{r + 1}-{t + 1}",
                    BaseUrl = $"http://backend-{r + 1}-{t + 1}:8080",
                    IsHealthy = true,
                    Weight = t + 1,
                    HealthCheckIntervalSeconds = 60
                });
            }

            var route = new GatewayRoute
            {
                Name = $"Route-{r + 1}",
                PathPattern = $"/api/service{r + 1}/**",
                AllowedMethods = ["GET", "POST", "PUT", "DELETE"],
                Targets = targets.ToArray(),
                TimeoutSeconds = 30
            };

            repository.AddAsync(route).GetAwaiter().GetResult();
            routes.Add(route);
        }

        return routes;
    }

    /// <summary>
    /// Creates a route with header transformation rules for testing header handling
    /// </summary>
    /// <param name="tests">The test class instance</param>
    /// <param name="routeName">Name of the route</param>
    /// <param name="headerTransforms">Header transformation rules</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="routeName"/> is null or whitespace</exception>
    /// <returns>Created GatewayRoute with header transforms</returns>
    public static GatewayRoute CreateRouteWithHeaderTransforms(
        this RoutingServiceTests tests,
        string routeName = "HeaderTransformRoute",
        Dictionary<string, string>? headerTransforms = null)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        var repository = new GatewayRouteRepository();

        var route = new GatewayRoute
        {
            Name = routeName,
            PathPattern = "/api/transform",
            AllowedMethods = ["POST"],
            Targets = [
                new RouteTarget
                {
                    Name = "transform-backend",
                    BaseUrl = "http://transform-backend:8080",
                    IsHealthy = true,
                    TransformHeaders = headerTransforms ?? new Dictionary<string, string>
                    {
                        ["X-Request-Id"] = "${requestId}",
                        ["X-Custom-Header"] = "custom-value",
                        ["X-Forwarded-For"] = "${clientIp}",
                        ["Authorization"] = "Bearer ${authToken}"
                    }
                }
            ],
            TimeoutSeconds = 30
        };

        repository.AddAsync(route).GetAwaiter().GetResult();
        return route;
    }

    /// <summary>
    /// Creates unhealthy targets for testing failure scenarios
    /// </summary>
    /// <param name="tests">The test class instance</param>
    /// <param name="count">Number of unhealthy targets to create</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="count"/> must be positive</exception>
    /// <returns>List of unhealthy RouteTarget objects</returns>
    public static List<RouteTarget> CreateUnhealthyTargets(
        this RoutingServiceTests tests,
        int count = 3)
    {
        ArgumentNullException.ThrowIfNull(tests);

        if (count < 1)
        {
            throw new ArgumentException("Count must be at least 1", nameof(count));
        }

        var targets = new List<RouteTarget>(count);

        for (int i = 0; i < count; i++)
        {
            targets.Add(new RouteTarget
            {
                Name = $"unhealthy-{i + 1}",
                BaseUrl = $"http://unhealthy-{i + 1}:8080",
                IsHealthy = false,
                Weight = 1,
                HealthCheckIntervalSeconds = 60
            });
        }

        return targets;
    }
}