#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public static class RoutingServiceTestsExtensions
{
    /// <summary>
    /// Creates a test route with healthy targets for testing routing scenarios
    /// </summary>
    /// <param name="tests">The test class instance</param>
    /// <param name="name">Route name</param>
    /// <param name="pathPattern">URL path pattern</param>
    /// <param name="targetCount">Number of targets to create</param>
    /// <returns>Created GatewayRoute</returns>
    public static GatewayRoute CreateTestRoute(
        this RoutingServiceTests tests,
        string name = "TestRoute",
        string pathPattern = "/api/test",
        int targetCount = 1)
    {
        var repository = new GatewayRouteRepository();
        var targets = new List<RouteTarget>();

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
    /// <returns>List of created GatewayRoute objects</returns>
    public static List<GatewayRoute> CreateMultipleTestRoutes(
        this RoutingServiceTests tests,
        int routeCount = 3,
        int targetsPerRoute = 2)
    {
        var repository = new GatewayRouteRepository();
        var routes = new List<GatewayRoute>();

        for (int r = 0; r < routeCount; r++)
        {
            var targets = new List<RouteTarget>();
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
    /// <returns>Created GatewayRoute with header transforms</returns>
    public static GatewayRoute CreateRouteWithHeaderTransforms(
        this RoutingServiceTests tests,
        string routeName = "HeaderTransformRoute",
        Dictionary<string, string>? headerTransforms = null)
    {
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
    /// <returns>List of unhealthy RouteTarget objects</returns>
    public static List<RouteTarget> CreateUnhealthyTargets(
        this RoutingServiceTests tests,
        int count = 3)
    {
        var targets = new List<RouteTarget>();

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