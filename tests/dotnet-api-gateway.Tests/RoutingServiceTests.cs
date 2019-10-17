#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class RoutingServiceTests
{
    private static RouteTarget HealthyTarget(string name = "backend") => new()
    {
        Name = name,
        BaseUrl = "http://backend:8080",
        Weight = 1,
        IsHealthy = true,
        HealthCheckIntervalSeconds = 60
    };

    private static RouteTarget UnhealthyTarget(string name = "backend") => new()
    {
        Name = name,
        BaseUrl = "http://backend:8080",
        Weight = 1,
        IsHealthy = false,
        HealthCheckIntervalSeconds = 60
    };

    private static GatewayRoute ValidRoute(string path = "/api/users") => new()
    {
        Name = "UserRoute",
        PathPattern = path,
        AllowedMethods = ["GET"],
        Targets = [HealthyTarget()],
        TimeoutSeconds = 30
    };

    [Fact]
    public async Task FindRouteAsync_ExistingRoute_ReturnsRoute()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route = ValidRoute();
        await repository.AddAsync(route);

        var service = new RoutingService(repository);

        // Act
        var result = await service.FindRouteAsync("/api/users", "GET");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("UserRoute");
    }

    [Fact]
    public async Task FindRouteAsync_NonExistentRoute_ThrowsRouteNotFoundException()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);

        // Act
        var act = () => service.FindRouteAsync("/api/nonexistent", "GET");

        // Assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    [Fact]
    public void SelectTarget_RoundRobin_DistributesEvenly()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var target1 = new RouteTarget { Name = "backend-1", BaseUrl = "http://backend1:8080", IsHealthy = true };
        var target2 = new RouteTarget { Name = "backend-2", BaseUrl = "http://backend2:8080", IsHealthy = true };
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [target1, target2],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository, LoadBalancingStrategy.RoundRobin);

        // Act
        var selected1 = service.SelectTarget(route);
        var selected2 = service.SelectTarget(route);
        var selected3 = service.SelectTarget(route);

        // Assert
        selected1.Should().Be(target1);
        selected2.Should().Be(target2);
        selected3.Should().Be(target1); // Cycles back
    }

    [Fact]
    public void SelectTarget_IpHash_SameIpSameTarget()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var target1 = new RouteTarget { Name = "backend-1", BaseUrl = "http://backend1:8080", IsHealthy = true };
        var target2 = new RouteTarget { Name = "backend-2", BaseUrl = "http://backend2:8080", IsHealthy = true };
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [target1, target2],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository, LoadBalancingStrategy.IpHash);
        const string clientIp = "192.168.1.100";

        // Act
        var selected1 = service.SelectTarget(route, clientIp);
        var selected2 = service.SelectTarget(route, clientIp);

        // Assert
        selected1.Should().Be(selected2);
    }

    [Fact]
    public void SelectTarget_IpHash_NoIpFallsBackToRoundRobin()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var target1 = new RouteTarget { Name = "backend-1", BaseUrl = "http://backend1:8080", IsHealthy = true };
        var target2 = new RouteTarget { Name = "backend-2", BaseUrl = "http://backend2:8080", IsHealthy = true };
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [target1, target2],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository, LoadBalancingStrategy.IpHash);

        // Act
        var selected1 = service.SelectTarget(route, null);
        var selected2 = service.SelectTarget(route, null);

        // Assert
        selected1.Should().Be(target1);
        selected2.Should().Be(target2);
    }

    [Fact]
    public void SelectTarget_LeastConnections_SelectsByWeight()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var target1 = new RouteTarget { Name = "backend-1", BaseUrl = "http://backend1:8080", IsHealthy = true, Weight = 5 };
        var target2 = new RouteTarget { Name = "backend-2", BaseUrl = "http://backend2:8080", IsHealthy = true, Weight = 1 };
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [target1, target2],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository, LoadBalancingStrategy.LeastConnections);

        // Act
        var selected = service.SelectTarget(route);

        // Assert
        selected.Should().Be(target2); // Lower weight selected
    }

    [Fact]
    public void SelectTarget_NoHealthyTargets_ThrowsGatewayException()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [UnhealthyTarget()],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository);

        // Act
        var act = () => service.SelectTarget(route);

        // Assert
        act.Should().Throw<GatewayException>().WithMessage("*No healthy targets*");
    }

    [Fact]
    public void SelectTarget_FiltersOnlyHealthyTargets()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var healthyTarget = HealthyTarget("healthy");
        var unhealthyTarget = UnhealthyTarget("unhealthy");
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = [unhealthyTarget, healthyTarget],
            TimeoutSeconds = 30
        };

        var service = new RoutingService(repository);

        // Act
        var selected = service.SelectTarget(route);

        // Assert
        selected.Should().Be(healthyTarget);
    }

    [Fact]
    public void BuildForwardUrl_CombinesTargetAndPath()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var target = new RouteTarget { BaseUrl = "http://backend:8080" };

        // Act
        var url = service.BuildForwardUrl(target, "/api/users/123");

        // Assert
        url.Should().Be("http://backend:8080/api/users/123");
    }

    [Fact]
    public void ApplyHeaderTransforms_AddsTransformedHeaders()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var target = new RouteTarget
        {
            TransformHeaders = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "custom-value",
                ["X-Gateway-Version"] = "1.0"
            }
        };

        var originalHeaders = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token",
            ["User-Agent"] = "MyClient/1.0"
        };

        // Act
        var transformed = service.ApplyHeaderTransforms(target, originalHeaders);

        // Assert
        transformed.Should().Contain("Authorization", "Bearer token");
        transformed.Should().Contain("X-Custom-Header", "custom-value");
        transformed.Should().Contain("X-Gateway-Version", "1.0");
    }

    [Fact]
    public void ApplyHeaderTransforms_OverridesExistingHeaders()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var target = new RouteTarget
        {
            TransformHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };

        var originalHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = "text/plain"
        };

        // Act
        var transformed = service.ApplyHeaderTransforms(target, originalHeaders);

        // Assert
        transformed["Content-Type"].Should().Be("application/json");
    }

    [Fact]
    public async Task GetAllActiveRoutesAsync_ReturnsActiveRoutes()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route1 = ValidRoute("/api/users");
        var route2 = ValidRoute("/api/products");
        await repository.AddAsync(route1);
        await repository.AddAsync(route2);

        var service = new RoutingService(repository);

        // Act
        var routes = await service.GetAllActiveRoutesAsync();

        // Assert
        routes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateRouteAsync_ValidRoute_AddsRoute()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var route = ValidRoute();

        // Act
        var created = await service.CreateRouteAsync(route);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateRouteAsync_InvalidRoute_ThrowsArgumentException()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var invalidRoute = new GatewayRoute { Name = "" }; // Missing required fields

        // Act
        var act = () => service.CreateRouteAsync(invalidRoute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateRouteAsync_ValidRoute_UpdatesRoute()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var route = ValidRoute();
        var created = await repository.AddAsync(route);

        created.TimeoutSeconds = 60;

        // Act
        var updated = await service.UpdateRouteAsync(created);

        // Assert
        updated.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public async Task DeleteRouteAsync_ExistingRoute_DeletesRoute()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);
        var route = ValidRoute();
        var created = await repository.AddAsync(route);

        // Act
        var deleted = await service.DeleteRouteAsync(created.Id);

        // Assert
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRouteAsync_NonExistentRoute_ReturnsFalse()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var service = new RoutingService(repository);

        // Act
        var deleted = await service.DeleteRouteAsync("nonexistent-id");

        // Assert
        deleted.Should().BeFalse();
    }
}
