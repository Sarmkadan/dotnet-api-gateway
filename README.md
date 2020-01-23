## CircuitBreakerRepositoryExtensions

The `CircuitBreakerRepositoryExtensions` class provides a set of extension methods for working with circuit breaker repositories. These extensions enable you to retrieve circuit breaker statuses by service name, state, or other criteria, as well as update and reset circuit breakers.

The following example demonstrates how to use these extensions:

```csharp
var circuitBreakerRepository = new CircuitBreakerRepository();

// Get circuit breaker status by service name or default
var status = await CircuitBreakerRepositoryExtensions.GetByServiceNameOrDefaultAsync(circuitBreakerRepository, "my-service");

// Get all open circuit breakers
var openCircuits = await CircuitBreakerRepositoryExtensions.GetOpenCircuitsAsync(circuitBreakerRepository);

// Update a batch of circuit breakers
await CircuitBreakerRepositoryExtensions.UpdateBatchAsync(circuitBreakerRepository, new[]
{
    new CircuitBreakerStatus { ServiceName = "service1", State = CircuitBreakerState.Open },
    new CircuitBreakerStatus { ServiceName = "service2", State = CircuitBreakerState.Closed },
});

// Reset all circuit breakers to closed
await CircuitBreakerRepositoryExtensions.ResetAllToClosedAsync(circuitBreakerRepository);
```

## JsonUtilityValidation

The `JsonUtilityValidation` class provides static methods for validating JSON data against expected formats and structures. It includes methods for checking validity, parsing, deserialization, and merging JSON, with both validation result and boolean outcome variants. Methods like `Validate<T>`, `ValidateDeserialize`, and `IsValid<T>` help ensure JSON conforms to expected schemas or types.

Example usage:
```csharp
// Validate a JSON string for basic deserialization safety
var json = "{\"Name\": 123}"; // Invalid JSON (number instead of string)
var errors = JsonUtilityValidation.ValidateDeserialize(json);
Console.WriteLine($"Validation errors: {errors.Count}"); // Outputs "Validation errors: 1"
```

## EventBus

The `EventBus` is an in‑memory pub‑sub system used by the gateway to broadcast domain events such as route creation, circuit‑breaker state changes, rate‑limit violations and request failures. It exposes a simple API for subscribing, publishing and inspecting the number of listeners for a given event type.

Example usage:

```csharp
using DotNetApiGateway.Events;
using System;
using System.Threading.Tasks;

// Create a logger (replace with a real logger in production)
var logger = new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<EventBus>();

// Instantiate the event bus
var bus = new EventBus(logger);

// Define a handler for RouteCreatedEvent
Func<RouteCreatedEvent, Task> routeCreatedHandler = async ev =>
{
    Console.WriteLine($"Route created: {ev.RouteId} – {ev.RouteName}");
    await Task.CompletedTask;
};

// Subscribe to the event
bus.Subscribe(routeCreatedHandler);

// Publish a RouteCreatedEvent
await bus.PublishAsync(new RouteCreatedEvent
{
    RouteId = "route-123",
    RouteName = "My API Route"
});

// Check how many handlers are listening
int count = bus.GetSubscriberCount<RouteCreatedEvent>();
Console.WriteLine($"Handlers registered: {count}");

// Unsubscribe if needed
bus.Unsubscribe(routeCreatedHandler);

// Clear all subscriptions
bus.Clear();
```

Make sure to replace the logger with a real implementation when integrating into the gateway.

## JsonUtilityBenchmarks

The `JsonUtilityBenchmarks` class measures the performance of JSON serialization and deserialization operations using the `JsonUtility` class. It benchmarks the time required to serialize objects to JSON and deserialize JSON back to objects, focusing on typical usage patterns.

Example usage:
```csharp
var benchmarks = new JsonUtilityBenchmarks();
benchmarks.Setup(); // Initializes test data

// Serialize test object to JSON
string json = benchmarks.Serialize();

// Deserialize JSON back to object
TestClass? obj = benchmarks.Deserialize();

Console.WriteLine($"Serialized: {json}");
Console.WriteLine($"Deserialized: {obj?.Name}, {obj?.Value}");
```

## RoutingAndRateLimitingIntegrationTests

The `RoutingAndRateLimitingIntegrationTests` class provides comprehensive integration tests for the API gateway's routing and rate limiting functionality. It tests the complete workflow from route creation and matching through target selection, rate limiting enforcement, and circuit breaker integration. These tests verify that the gateway correctly handles concurrent requests, maintains proper state across operations, and enforces configuration settings.

Example usage:
```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using Xunit;

// Create repositories and services
var routeRepository = new GatewayRouteRepository();
var routingService = new RoutingService(routeRepository);
var rateLimitStoreFactory = new RateLimitStoreFactory();
var rateLimitService = new RateLimitingService(rateLimitStoreFactory, logger);

// Test full routing workflow
var route = new GatewayRoute
{
    Name = "api-route",
    PathPattern = "/api/users",
    AllowedMethods = ["GET", "POST"],
    Targets = [
        new RouteTarget { Name = "backend-1", BaseUrl = "http://backend1:8080", IsHealthy = true },
        new RouteTarget { Name = "backend-2", BaseUrl = "http://backend2:8080", IsHealthy = true }
    ],
    TimeoutSeconds = 30
};

await routingService.CreateRouteAsync(route);
var foundRoute = await routingService.FindRouteAsync("/api/users", "GET");
var selectedTarget = routingService.SelectTarget(foundRoute);

Assert.NotNull(foundRoute);
Assert.Equal("api-route", foundRoute.Name);
Assert.NotNull(selectedTarget);

// Test rate limiting
var policy = new RateLimitPolicy { Enabled = true, RequestsPerMinute = 100 };
var isAllowed = await rateLimitService.IsAllowedAsync("client-1", policy);
Assert.True(isAllowed);

// Test circuit breaker integration
var cbRepository = new CircuitBreakerRepository();
var cbService = new CircuitBreakerService(cbRepository);
var canAttempt = await cbService.CanAttemptAsync("backend-1", new CircuitBreakerPolicy { Enabled = true });
Assert.True(canAttempt);
```
