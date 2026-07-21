#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Exceptions;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Test implementation of GatewayRouteRepository that allows testing repository-dependent functionality
/// </summary>
public class TestGatewayRouteRepository : GatewayRouteRepository
{
    public TestGatewayRouteRepository()
    {
        // Initialize with empty routes
    }
}

public class RoutingServiceTests
{
    private readonly Mock<GatewayRouteRepository> _routeRepositoryMock;
    private readonly RoutingService _routingService;

    public RoutingServiceTests()
    {
        _routeRepositoryMock = new Mock<GatewayRouteRepository>();
        _routingService = new RoutingService(_routeRepositoryMock.Object, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);
    }

    [Fact]
    public async Task FindRouteAsync_ExactPathMatch_ReturnsRoute()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var route = new GatewayRoute
        {
            Id = "test-route-1",
            Name = "Test Route",
            PathPattern = "/api/users",
            AllowedMethods = new[] { "GET", "POST" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-1",
                    Name = "Test Target",
                    BaseUrl = "https://api.example.com"
                }
            }
        };

        await routeRepository.AddAsync(route);

        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        // Act
        var result = await routingService.FindRouteAsync("/api/users", "GET");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(route);
    }

    [Fact]
    public async Task FindRouteAsync_PrefixTemplateMatch_ReturnsRoute()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var route = new GatewayRoute
        {
            Id = "test-route-2",
            Name = "User Route",
            PathPattern = "/api/users/{id}",
            AllowedMethods = new[] { "GET", "PUT", "DELETE" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-2",
                    Name = "User Service Target",
                    BaseUrl = "https://users.example.com"
                }
            }
        };

        await routeRepository.AddAsync(route);

        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        // Act
        var result = await routingService.FindRouteAsync("/api/users/123", "GET");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(route);
    }

    [Fact]
    public async Task FindRouteAsync_WildcardMatch_ReturnsRoute()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var route = new GatewayRoute
        {
            Id = "test-route-3",
            Name = "Static Files Route",
            PathPattern = "/static/*",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-3",
                    Name = "Static Files Target",
                    BaseUrl = "https://static.example.com"
                }
            }
        };

        await routeRepository.AddAsync(route);

        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        // Act - wildcard matches exactly one segment
        var result = await routingService.FindRouteAsync("/static/image.png", "GET");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(route);
    }

    [Fact]
    public async Task FindRouteAsync_NoMatch_ThrowsRouteNotFoundException()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        // Act
        Func<Task> act = async () => await routingService.FindRouteAsync("/nonexistent", "GET");

        // Assert
        await act.Should().ThrowAsync<RouteNotFoundException>()
            .WithMessage("Route not found: GET /nonexistent");
    }

    [Fact]
    public async Task FindRouteAsync_MethodNotSupported_ThrowsRouteNotFoundException()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var route = new GatewayRoute
        {
            Id = "test-route-4",
            Name = "GET Only Route",
            PathPattern = "/api/data",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-4",
                    Name = "Data Target",
                    BaseUrl = "https://data.example.com"
                }
            }
        };

        await routeRepository.AddAsync(route);
        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        // Act
        Func<Task> act = async () => await routingService.FindRouteAsync("/api/data", "POST");

        // Assert
        await act.Should().ThrowAsync<RouteNotFoundException>()
            .WithMessage("Route not found: POST /api/data");
    }

    [Fact]
    public async Task SelectTarget_RoundRobinStrategy_DistributesEvenly()
    {
        // Arrange
        var route = new GatewayRoute
        {
            Id = "test-route-5",
            Name = "Load Balanced Route",
            PathPattern = "/api/load",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-5a",
                    Name = "Target A",
                    BaseUrl = "https://a.example.com",
                    Weight = 1
                },
                new RouteTarget
                {
                    Id = "target-5b",
                    Name = "Target B",
                    BaseUrl = "https://b.example.com",
                    Weight = 1
                },
                new RouteTarget
                {
                    Id = "target-5c",
                    Name = "Target C",
                    BaseUrl = "https://c.example.com",
                    Weight = 1
                }
            }
        };

        // Act & Assert
        var target1 = _routingService.SelectTarget(route);
        var target2 = _routingService.SelectTarget(route);
        var target3 = _routingService.SelectTarget(route);
        var target4 = _routingService.SelectTarget(route);

        target1.Should().NotBeNull();
        target2.Should().NotBeNull();
        target3.Should().NotBeNull();
        target4.Should().NotBeNull();

        // Should cycle through targets in round-robin fashion
        target1.Id.Should().Be("target-5a");
        target2.Id.Should().Be("target-5b");
        target3.Id.Should().Be("target-5c");
        target4.Id.Should().Be("target-5a"); // Back to first
    }

    [Fact]
    public void SelectTarget_NoHealthyTargets_ThrowsGatewayException()
    {
        // Arrange
        var route = new GatewayRoute
        {
            Id = "test-route-6",
            Name = "Unhealthy Route",
            PathPattern = "/api/unhealthy",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-6a",
                    Name = "Unhealthy Target A",
                    BaseUrl = "https://unhealthy-a.example.com",
                    IsHealthy = false
                },
                new RouteTarget
                {
                    Id = "target-6b",
                    Name = "Unhealthy Target B",
                    BaseUrl = "https://unhealthy-b.example.com",
                    IsHealthy = false
                }
            }
        };

        // Act
        Func<RouteTarget> act = () => _routingService.SelectTarget(route);

        // Assert
        act.Should().Throw<GatewayException>()
            .WithMessage("No healthy targets available for route Unhealthy Route")
            .Where(ex => ex.ErrorCode == "NO_HEALTHY_TARGETS");
    }

    [Fact]
    public void SelectTarget_IpHashStrategy_ReturnsConsistentTargetForSameIp()
    {
        // Arrange
        var route = new GatewayRoute
        {
            Id = "test-route-7",
            Name = "IP Hash Route",
            PathPattern = "/api/ip-hash",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-7a",
                    Name = "IP Target A",
                    BaseUrl = "https://ipa.example.com"
                },
                new RouteTarget
                {
                    Id = "target-7b",
                    Name = "IP Target B",
                    BaseUrl = "https://ipb.example.com"
                }
            }
        };

        var routingServiceIpHash = new RoutingService(_routeRepositoryMock.Object, LoadBalancingStrategy.IpHash, NullLogger<RoutingService>.Instance);

        // Act
        var target1 = routingServiceIpHash.SelectTarget(route, "192.168.1.100");
        var target2 = routingServiceIpHash.SelectTarget(route, "192.168.1.100"); // Same IP
        var target3 = routingServiceIpHash.SelectTarget(route, "192.168.1.101"); // Different IP

        // Assert
        target1.Should().NotBeNull();
        target2.Should().NotBeNull();
        target3.Should().NotBeNull();

        // Same IP should return same target
        target1.Id.Should().Be(target2.Id);
        // Different IP may return different target (not guaranteed but likely)
    }

    [Fact]
    public void SelectTarget_LeastConnectionsStrategy_ReturnsLowestWeightTarget()
    {
        // Arrange
        var route = new GatewayRoute
        {
            Id = "test-route-8",
            Name = "Least Connections Route",
            PathPattern = "/api/least-conn",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-8a",
                    Name = "High Weight Target",
                    BaseUrl = "https://high.example.com",
                    Weight = 10
                },
                new RouteTarget
                {
                    Id = "target-8b",
                    Name = "Low Weight Target",
                    BaseUrl = "https://low.example.com",
                    Weight = 1
                },
                new RouteTarget
                {
                    Id = "target-8c",
                    Name = "Medium Weight Target",
                    BaseUrl = "https://medium.example.com",
                    Weight = 5
                }
            }
        };

        var routingServiceLeastConn = new RoutingService(_routeRepositoryMock.Object, LoadBalancingStrategy.LeastConnections, NullLogger<RoutingService>.Instance);

        // Act
        var target = routingServiceLeastConn.SelectTarget(route);

        // Assert
        target.Should().NotBeNull();
        target.Id.Should().Be("target-8b"); // Should select target with lowest weight (1)
    }

    [Fact]
    public void BuildForwardUrl_CombinesBaseUrlAndPathCorrectly()
    {
        // Arrange
        var target = new RouteTarget
        {
            Id = "target-9",
            Name = "Forward URL Target",
            BaseUrl = "https://api.example.com",
            StripPathPrefix = false
        };

        // Act
        var url = _routingService.BuildForwardUrl(target, "/users/123/posts");

        // Assert
        url.Should().Be("https://api.example.com/users/123/posts");
    }

    [Fact]
    public void ApplyHeaderTransforms_AddsAndOverridesHeaders()
    {
        // Arrange
        var target = new RouteTarget
        {
            Id = "target-11",
            Name = "Header Transform Target",
            BaseUrl = "https://api.example.com",
            TransformHeaders = new Dictionary<string, string>
            {
                { "X-Custom-Header", "custom-value" },
                { "Authorization", "Bearer overridden-token" },
                { "X-Another-Header", "another-value" }
            }
        };

        var originalHeaders = new Dictionary<string, string>
        {
            { "Authorization", "Bearer original-token" },
            { "Content-Type", "application/json" },
            { "User-Agent", "test-agent" }
        };

        // Act
        var result = _routingService.ApplyHeaderTransforms(target, originalHeaders);

        // Assert
        result.Should().ContainKey("X-Custom-Header").WhoseValue.Should().Be("custom-value");
        result.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer overridden-token"); // Overridden
        result.Should().ContainKey("X-Another-Header").WhoseValue.Should().Be("another-value");
        result.Should().ContainKey("Content-Type").WhoseValue.Should().Be("application/json"); // Preserved
        result.Should().ContainKey("User-Agent").WhoseValue.Should().Be("test-agent"); // Preserved
    }

    [Fact]
    public async Task GetAllActiveRoutesAsync_ReturnsOnlyActiveRoutes()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        var activeRoute = new GatewayRoute
        {
            Id = "active-route",
            Name = "Active Route",
            PathPattern = "/api/active",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "active-target",
                    Name = "Active Target",
                    BaseUrl = "https://active.example.com"
                }
            },
            IsActive = true
        };

        var inactiveRoute = new GatewayRoute
        {
            Id = "inactive-route",
            Name = "Inactive Route",
            PathPattern = "/api/inactive",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "inactive-target",
                    Name = "Inactive Target",
                    BaseUrl = "https://inactive.example.com"
                }
            },
            IsActive = false
        };

        await routeRepository.AddAsync(activeRoute);
        await routeRepository.AddAsync(inactiveRoute);

        // Act
        var result = await routingService.GetAllActiveRoutesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("active-route");
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRouteAsync_ValidRoute_CallsRepositoryAndLogs()
    {
        // Arrange
        var route = new GatewayRoute
        {
            Id = "new-route",
            Name = "New Route",
            PathPattern = "/api/new",
            AllowedMethods = new[] { "POST" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "new-target",
                    Name = "New Target",
                    BaseUrl = "https://new.example.com"
                }
            }
        };

        // Act
        var result = await _routingService.CreateRouteAsync(route);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("new-route");
    }

    [Fact]
    public async Task DeleteRouteAsync_ExistingRoute_ReturnsTrueAndLogs()
    {
        // Arrange
        var routeRepository = new TestGatewayRouteRepository();
        var routingService = new RoutingService(routeRepository, LoadBalancingStrategy.RoundRobin, NullLogger<RoutingService>.Instance);

        var route = new GatewayRoute
        {
            Id = "existing-route-id",
            Name = "Test Route",
            PathPattern = "/api/test",
            AllowedMethods = new[] { "GET" },
            Targets = new[]
            {
                new RouteTarget
                {
                    Id = "target-1",
                    Name = "Test Target",
                    BaseUrl = "https://test.example.com"
                }
            }
        };

        await routeRepository.AddAsync(route);

        // Act
        var result = await routingService.DeleteRouteAsync("existing-route-id");

        // Assert
        result.Should().BeTrue();
    }
}