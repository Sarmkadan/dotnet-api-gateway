#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using DotNetApiGateway.Configuration;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class RoutingAndRateLimitingIntegrationTests
{
    private static RouteTarget CreateTarget(string name = "backend", string baseUrl = "http://localhost:8080") => new()
    {
        Name = name,
        BaseUrl = baseUrl,
        Weight = 1,
        IsHealthy = true,
        HealthCheckIntervalSeconds = 60
    };

    private static GatewayRoute CreateRoute(string name, string pattern, params RouteTarget[] targets) => new()
    {
        Name = name,
        PathPattern = pattern,
        AllowedMethods = ["GET", "POST", "PUT", "DELETE"],
        Targets = targets.ToList(),
        TimeoutSeconds = 30
    };

    [Fact]
    public async Task FullRoutingWorkflow_RequestMatchesRoute_SelectsHealthyTarget()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route = CreateRoute(
            "api-route",
            "/api/users",
            CreateTarget("backend-1", "http://backend1:8080"),
            CreateTarget("backend-2", "http://backend2:8080")
        );
        await repository.AddAsync(route);

        var routingService = new RoutingService(repository);

        // Act
        var foundRoute = await routingService.FindRouteAsync("/api/users", "GET");
        var selectedTarget = routingService.SelectTarget(foundRoute);

        // Assert
        foundRoute.Should().NotBeNull();
        foundRoute.Name.Should().Be("api-route");
        selectedTarget.Should().NotBeNull();
        selectedTarget.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitingWithRouting_MultipleRequests_EnforcesLimit()
    {
        // Arrange
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        var mockStore = new Mock<IRateLimitStore>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var logger = new Mock<ILogger<RateLimitingService>>();
        var rateLimitService = new RateLimitingService(mockFactory.Object, logger.Object);

        var allowedCount = 0;
        mockStore.Setup(s => s.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .Returns(async () =>
            {
                await Task.Delay(0);
                return allowedCount++ < 3; // Only allow first 3 requests
            });

        var policy = new RateLimitPolicy { Enabled = true, RequestsPerMinute = 3 };
        const string clientKey = "client-1";

        // Act
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(await rateLimitService.IsAllowedAsync(clientKey, policy));
        }

        // Assert
        results.Should().Equal(true, true, true, false, false);
    }

    [Fact]
    public async Task EndToEndWorkflow_RouteCreatedAndQueried_CorrectData()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var routingService = new RoutingService(repository);

        var route = CreateRoute(
            "users-api",
            "/api/v1/users/{id}",
            CreateTarget("users-backend", "http://users-service:8080")
        );

        // Act
        var created = await routingService.CreateRouteAsync(route);
        var allRoutes = await routingService.GetAllActiveRoutesAsync();

        // Assert
        created.Id.Should().NotBeEmpty();
        allRoutes.Should().HaveCount(1);
        allRoutes.First().Name.Should().Be("users-api");
    }

    [Fact]
    public async Task CircuitBreakerAndRouting_ChainedDecisions_MakesCorrectChoices()
    {
        // Arrange
        var cbRepository = new CircuitBreakerRepository();
        var routeRepository = new GatewayRouteRepository();
        var cbService = new CircuitBreakerService(cbRepository);
        var routingService = new RoutingService(routeRepository);

        var healthyTarget = CreateTarget("service-a");
        var unhealthyTarget = CreateTarget("service-b");
        unhealthyTarget.IsHealthy = false;

        var route = CreateRoute("multi-target", "/api/endpoint", healthyTarget, unhealthyTarget);
        await routeRepository.AddAsync(route);

        var foundRoute = await routingService.FindRouteAsync("/api/endpoint", "GET");
        var policy = new CircuitBreakerPolicy { Enabled = true, FailureThreshold = 3, TimeoutSeconds = 60 };

        // Act
        var canAttempt = await cbService.CanAttemptAsync("service-a", policy);
        var selectedTarget = routingService.SelectTarget(foundRoute);

        // Assert
        canAttempt.Should().BeTrue();
        selectedTarget.Name.Should().Be("service-a");
    }

    [Fact]
    public async Task ConfigurationLoading_ValidConfig_AllPropertiesSet()
    {
        // Arrange
        var config = new GatewayConfiguration
        {
            ApplicationName = "TestGateway",
            Version = "1.0.0",
            Port = 5000,
            MaxRequestBodySize = 10 * 1024 * 1024,
            DefaultTimeoutSeconds = 30,
            MaxConcurrentRequests = 100,
            EnableLogging = true,
            EnableMetrics = true
        };

        // Act & Assert
        config.ApplicationName.Should().Be("TestGateway");
        config.Version.Should().Be("1.0.0");
        config.Port.Should().Be(5000);
        config.MaxRequestBodySize.Should().Be(10 * 1024 * 1024);
    }
}

public sealed class ConcurrencyIntegrationTests
{
    [Fact]
    public async Task ConcurrentRequests_MultipleClients_EachTrackedIndependently()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route = new GatewayRoute
        {
            Name = "test-route",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets =
            [
                new RouteTarget
                {
                    Name = "backend",
                    BaseUrl = "http://localhost:8080",
                    IsHealthy = true
                }
            ],
            TimeoutSeconds = 30
        };
        await repository.AddAsync(route);

        var routingService = new RoutingService(repository);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(async i =>
            {
                var foundRoute = await routingService.FindRouteAsync("/api/test", "GET");
                var target = routingService.SelectTarget(foundRoute);
                return target.Name;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe("backend");
        results.Should().HaveCount(10);
    }

    [Fact]
    public async Task ConcurrentCircuitBreakerOperations_NoRaceConditions_AllOperationsSucceed()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        const string serviceName = "test-service";

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(async i =>
            {
                var status = await service.GetOrCreateStatusAsync(serviceName);
                return status.Id;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe(results[0]); // All should get the same ID
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task ConcurrentRateLimitingChecks_MultipleStores_NoInterference()
    {
        // Arrange
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        var mockStore = new Mock<IRateLimitStore>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var requestCount = 0;
        var lockObj = new object();

        mockStore.Setup(s => s.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .Returns(async () =>
            {
                await Task.Delay(5); // Simulate some work
                lock (lockObj)
                {
                    requestCount++;
                    return requestCount <= 10; // Allow first 10
                }
            });

        var logger = new Mock<ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = new RateLimitPolicy { Enabled = true, RequestsPerMinute = 10 };

        // Act
        var tasks = Enumerable.Range(0, 15)
            .Select(i => service.IsAllowedAsync($"client-{i % 3}", policy))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Count(r => r).Should().BeLessThanOrEqualTo(10);
    }
}

public sealed class RequestContextIntegrationTests
{
    [Fact]
    public void RequestContext_WithMultipleOperations_MaintainsState()
    {
        // Arrange
        var context = new RequestContext
        {
            Path = "/api/users/123",
            Method = "GET",
            ClientIp = "192.168.1.1",
            AuthToken = "Bearer token123"
        };

        context.Headers["X-Request-ID"] = "req-001";
        context.QueryParameters["include"] = "profile";
        context.CustomData["user_type"] = "admin";

        // Act
        context.Headers["X-Custom"] = "custom-value";
        var bearer = context.ExtractBearerToken();
        var clientId = context.GetClientIdentifier();
        var hasAuth = context.HasAuthToken();

        // Assert
        context.Path.Should().Be("/api/users/123");
        context.Headers.Should().HaveCount(2);
        context.QueryParameters.Should().HaveCount(1);
        context.CustomData.Should().HaveCount(1);
        bearer.Should().Be("token123");
        clientId.Should().Be("192.168.1.1");
        hasAuth.Should().BeTrue();
    }

    [Fact]
    public void RequestContext_MatchingRouteAndTarget_PreservesReferences()
    {
        // Arrange
        var context = new RequestContext();
        var route = new GatewayRoute
        {
            Name = "test-route",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"],
            Targets = []
        };
        var target = new RouteTarget { Name = "backend", BaseUrl = "http://localhost" };

        // Act
        context.MatchedRoute = route;
        context.SelectedTarget = target;

        // Assert
        context.MatchedRoute.Should().Be(route);
        context.SelectedTarget.Should().Be(target);
        context.MatchedRoute.Name.Should().Be("test-route");
        context.SelectedTarget.Name.Should().Be("backend");
    }
}

public sealed class ValidationAndUtilityIntegrationTests
{
    [Fact]
    public void ValidationUtility_EmailAndUrl_CorrectlyValidates()
    {
        // Arrange & Act
        var emailValid = DotNetApiGateway.Utilities.ValidationUtility.IsValidEmail("user@example.com");
        var emailInvalid = DotNetApiGateway.Utilities.ValidationUtility.IsValidEmail("invalid");
        var urlValid = DotNetApiGateway.Utilities.ValidationUtility.IsValidUrl("https://example.com");
        var urlInvalid = DotNetApiGateway.Utilities.ValidationUtility.IsValidUrl("not-a-url");

        // Assert
        emailValid.Should().BeTrue();
        emailInvalid.Should().BeFalse();
        urlValid.Should().BeTrue();
        urlInvalid.Should().BeFalse();
    }

    [Fact]
    public void JsonUtility_SerializeDeserialize_RoundTrip()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            Enabled = true,
            RequestsPerMinute = 100,
            Strategy = RateLimitStrategy.SlidingWindow
        };

        // Act
        var json = DotNetApiGateway.Utilities.JsonUtility.Serialize(policy);
        var deserialized = DotNetApiGateway.Utilities.JsonUtility.Deserialize<RateLimitPolicy>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Enabled.Should().Be(true);
        deserialized.RequestsPerMinute.Should().Be(100);
    }

    [Fact]
    public void UrlUtility_ParseAndBuild_ConsistentResults()
    {
        // Arrange
        var url = "https://api.example.com/v1/users?page=1&limit=10";

        // Act
        var hostname = DotNetApiGateway.Utilities.UrlUtility.GetHostname(url);
        var port = DotNetApiGateway.Utilities.UrlUtility.GetPort(url);
        var hasParam = DotNetApiGateway.Utilities.UrlUtility.HasQueryParameter(url, "page");

        // Assert
        hostname.Should().Be("api.example.com");
        port.Should().Be(443); // HTTPS default
        hasParam.Should().BeTrue();
    }
}

public sealed class GatewayRouteRepositoryIntegrationTests
{
    [Fact]
    public async Task Repository_CreateUpdateDelete_FullLifecycle()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route = new GatewayRoute
        {
            Name = "lifecycle-test",
            PathPattern = "/api/test",
            AllowedMethods = ["GET", "POST"],
            Targets = [new RouteTarget { Name = "backend", BaseUrl = "http://localhost" }],
            TimeoutSeconds = 30
        };

        // Act - Create
        var created = await repository.AddAsync(route);
        created.Id.Should().NotBeEmpty();

        // Act - Read
        var retrieved = await repository.GetByIdAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("lifecycle-test");

        // Act - Update
        created.TimeoutSeconds = 60;
        var updated = await repository.UpdateAsync(created);
        updated.TimeoutSeconds.Should().Be(60);

        // Act - Delete
        var deleted = await repository.DeleteAsync(created.Id);
        deleted.Should().BeTrue();

        // Act - Verify Deleted
        var notFound = await repository.GetByIdAsync(created.Id);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task Repository_FindByPath_MatchesCorrectRoute()
    {
        // Arrange
        var repository = new GatewayRouteRepository();
        var route1 = new GatewayRoute
        {
            Name = "users-route",
            PathPattern = "/api/users",
            AllowedMethods = ["GET"],
            Targets = [new RouteTarget { Name = "backend", BaseUrl = "http://localhost" }]
        };
        var route2 = new GatewayRoute
        {
            Name = "products-route",
            PathPattern = "/api/products",
            AllowedMethods = ["GET"],
            Targets = [new RouteTarget { Name = "backend", BaseUrl = "http://localhost" }]
        };

        await repository.AddAsync(route1);
        await repository.AddAsync(route2);

        // Act
        var found = await repository.FindRouteByPathAsync("/api/users", "GET");

        // Assert
        found.Should().NotBeNull();
        found!.Name.Should().Be("users-route");
    }
}

public sealed class MetricsServiceIntegrationTests
{
    [Fact]
    public void MetricsService_RecordRequest_CalculatesCorrectly()
    {
        // Arrange
        var metricsService = new MetricsService();

        // Act
        metricsService.RecordRequest("route1", 200, 100);
        metricsService.RecordRequest("route1", 200, 150);
        metricsService.RecordRequest("route1", 404, 50);
        metricsService.RecordRequest("route2", 200, 200);

        var metrics = metricsService.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(4);
        metrics.SuccessfulRequests.Should().Be(3);
        metrics.FailedRequests.Should().Be(1);
        metrics.StatusCodeDistribution[200].Should().Be(3);
        metrics.StatusCodeDistribution[404].Should().Be(1);
    }
}
