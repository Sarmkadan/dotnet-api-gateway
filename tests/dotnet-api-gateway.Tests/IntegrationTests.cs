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

/// <summary>
/// Integration tests for routing and rate limiting functionality in the API gateway.
/// Tests the interaction between route matching, target selection, rate limiting, and circuit breaker patterns.
/// </summary>
public sealed class RoutingAndRateLimitingIntegrationTests
{
	/// <summary>
	/// Creates a route target with default or specified parameters.
	/// </summary>
	/// <param name="name">The name of the target (default: "backend").</param>
	/// <param name="baseUrl">The base URL for the target (default: "http://localhost:8080").</param>
	/// <returns>A new <see cref="RouteTarget"/> instance configured with the specified parameters.</returns>
	private static RouteTarget CreateTarget(string name = "backend", string baseUrl = "http://localhost:8080") => new()
	{
		Name = name,
		BaseUrl = baseUrl,
		Weight = 1,
		IsHealthy = true,
		HealthCheckIntervalSeconds = 60
	};

	/// <summary>
	/// Creates a gateway route with the specified configuration.
	/// </summary>
	/// <param name="name">The name of the route.</param>
	/// <param name="pattern">The path pattern to match against incoming requests.</param>
	/// <param name="targets">The route targets that can handle requests matching the pattern.</param>
	/// <returns>A new <see cref="GatewayRoute"/> instance configured with the specified parameters.</returns>
	private static GatewayRoute CreateRoute(string name, string pattern, params RouteTarget[] targets) => new()
	{
		Name = name,
		PathPattern = pattern,
		AllowedMethods = ["GET", "POST", "PUT", "DELETE"],
		Targets = targets,
		TimeoutSeconds = 30
	};

	/// <summary>
	/// Tests a complete routing workflow where a request matches a route and a healthy target is selected.
	/// </summary>
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

	/// <summary>
	/// Tests rate limiting enforcement when multiple requests are made within the rate limit window.
	/// </summary>
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

	/// <summary>
	/// Tests an end-to-end workflow where a route is created and then queried to verify correct data persistence.
	/// </summary>
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

	/// <summary>
	/// Tests the interaction between circuit breaker and routing decisions to ensure unhealthy targets are avoided.
	/// </summary>
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

	/// <summary>
	/// Tests configuration loading to ensure all properties are correctly set from configuration.
	/// </summary>
	[Fact]
	public async Task ConfigurationLoading_ValidConfig_AllPropertiesSet()
	{
		// Arrange
		var config = new DotnetApiGatewayOptions
		{
			ApplicationName = "TestGateway",
			Version = "1.0.0",
			MaxRequestBodySize = 10 * 1024 * 1024,
			DefaultTimeoutSeconds = 30,
			MaxConcurrentRequests = 100,
			EnableLogging = true,
			EnableMetrics = true
		};

		// Act & Assert
		config.ApplicationName.Should().Be("TestGateway");
		config.Version.Should().Be("1.0.0");
		config.MaxRequestBodySize.Should().Be(10 * 1024 * 1024);
	}
}