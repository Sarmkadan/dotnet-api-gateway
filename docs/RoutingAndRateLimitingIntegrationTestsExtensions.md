# RoutingAndRateLimitingIntegrationTestsExtensions

The `RoutingAndRateLimitingIntegrationTestsExtensions` class provides a set of static helper methods designed to streamline the setup of integration tests within the `dotnet-api-gateway` project. It encapsulates the instantiation logic for core gateway services—specifically routing, rate limiting, circuit breaking, and metrics—allowing test suites to generate configured service instances with minimal boilerplate. This utility ensures consistent service configuration across test scenarios while isolating dependencies required for verifying gateway behavior under various load and failure conditions.

## API

### CreateConfiguredRoutingService
Initializes and returns a new instance of the `RoutingService` pre-configured for integration testing scenarios.
*   **Parameters**: None.
*   **Return Value**: A fully instantiated `RoutingService` object ready to process route definitions.
*   **Exceptions**: Throws an exception if the underlying configuration required to bootstrap the routing engine is missing or malformed in the test context.

### CreateRateLimitingService
Constructs a `RateLimitingService` instance with default policies suitable for validating throughput constraints and request throttling logic.
*   **Parameters**: None.
*   **Return Value**: A new `RateLimitingService` object.
*   **Exceptions**: May throw if the internal rate limit store fails to initialize or if required policy definitions are absent.

### CreateCircuitBreakerService
Generates a `CircuitBreakerService` configured with thresholds optimized for rapid state transitions during fault injection tests.
*   **Parameters**: None.
*   **Return Value**: A configured `CircuitBreakerService` instance.
*   **Exceptions**: Throws if the circuit breaker state manager cannot be allocated or if dependency injection for failure counters fails.

### CreateMetricsService
Creates a `MetricsService` instance designed to capture and expose telemetry data without external backend dependencies, typically using in-memory storage for test verification.
*   **Parameters**: None.
*   **Return Value**: A `MetricsService` object ready to record metrics.
*   **Exceptions**: Throws if the metrics collector fails to start or if the in-memory store initialization encounters an error.

## Usage

### Example 1: Setting Up a Full Gateway Test Context
This example demonstrates how to initialize all core services required for a comprehensive integration test that verifies routing logic alongside rate limiting and circuit breaker states.

```csharp
using dotnet_api_gateway.Tests.Extensions;

public class GatewayIntegrationTests
{
    [Fact]
    public void Gateway_Should_Route_And_Enforce_Limits()
    {
        // Initialize services using the extension helpers
        var routingService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateConfiguredRoutingService();
        var rateLimitingService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateRateLimitingService();
        var circuitBreakerService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateCircuitBreakerService();
        var metricsService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateMetricsService();

        // Simulate a request flow
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        
        // Verify routing resolution
        var route = routingService.ResolveRoute(request);
        Assert.NotNull(route);

        // Verify rate limit check
        var isAllowed = rateLimitingService.CheckLimit("client-ip-123");
        Assert.True(isAllowed);
        
        // Record metrics for assertion later
        metricsService.RecordRequest(route.Path, 200, TimeSpan.FromMilliseconds(50));
    }
}
```

### Example 2: Isolating Circuit Breaker Behavior
This example focuses specifically on testing the circuit breaker logic in isolation, ensuring that the service correctly transitions to an open state after simulated failures.

```csharp
using dotnet_api_gateway.Tests.Extensions;
using dotnet_api_gateway.Services;

public class CircuitBreakerTests
{
    [Fact]
    public void CircuitBreaker_Should_Open_After_Threshold()
    {
        // Create only the necessary service for this specific test case
        var circuitBreakerService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateCircuitBreakerService();
        var metricsService = RoutingAndRateLimitingIntegrationTestsExtensions.CreateMetricsService();

        const string serviceName = "downstream-payment-service";

        // Simulate consecutive failures
        for (int i = 0; i < 5; i++)
        {
            circuitBreakerService.RecordFailure(serviceName);
        }

        // Assert the circuit is now open
        var state = circuitBreakerService.GetState(serviceName);
        Assert.Equal(CircuitState.Open, state);

        // Ensure metrics captured the state change
        var metrics = metricsService.GetRecentEvents();
        Assert.Contains(metrics, m => m.EventType == "CircuitOpened");
    }
}
```

## Notes

*   **Instance Isolation**: Each method call returns a new instance of the respective service. There is no shared state between calls to `CreateConfiguredRoutingService`, `CreateRateLimitingService`, etc. Test writers must ensure that if services need to share state (e.g., a circuit breaker reading metrics), such dependencies are explicitly wired in the test setup, as these factory methods do not automatically link the returned instances.
*   **Thread Safety**: The methods themselves are static and thread-safe for invocation. However, the returned service instances are not guaranteed to be thread-safe unless the underlying implementation specifically dictates otherwise. In multi-threaded test scenarios involving concurrent requests against a single service instance, external synchronization or per-thread service instantiation may be required.
*   **Configuration Dependencies**: These methods rely on the test environment having access to necessary configuration sections (e.g., `appsettings.json` or environment variables) expected by the gateway core. If running in a minimal test harness without a host builder, ensure that default configuration providers are registered to prevent initialization exceptions.
*   **Resource Disposal**: The created services may hold unmanaged resources or in-memory caches. While intended for short-lived test scopes, it is best practice to dispose of the returned objects if they implement `IDisposable`, particularly the `MetricsService` which may buffer data before flushing.
