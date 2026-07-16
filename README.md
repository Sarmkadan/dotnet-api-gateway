# DotNetApiGateway

An ASP.NET Core API gateway: route matching, load-balanced forwarding, per-route rate limiting, circuit breaking, request/response transformation and aggregation.

## Architecture

How the pieces fit together - middleware order, the fallback forwarding endpoint, design decisions and their trade-offs - is documented in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## CircuitBreakerPolicy

The `CircuitBreakerPolicy` class defines fault-tolerance rules for the API gateway's circuit breaker pattern implementation. It configures thresholds for failure/success detection, timeout behavior, retry logic, and HTTP status codes that trigger circuit breaking. The policy can be enabled/disabled per route or service and is validated before use to ensure configuration integrity.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a circuit breaker policy with default settings
var policy = new CircuitBreakerPolicy
{
    Id = "user-service-policy",
    FailureThreshold = 3,      // Trip circuit after 3 failures
    SuccessThreshold = 2,      // Reset circuit after 2 consecutive successes
    TimeoutSeconds = 30,        // Consider request timed out after 30 seconds
    FailureStatusCodes = [500, 502, 503, 504], // Codes that count as failures
    Enabled = true,             // Enable circuit breaker for this route
    MaxRetries = 2,            // Retry failed requests up to 2 times
    RetryDelayMilliseconds = 200 // Wait 200ms between retries
};

// Validate the policy configuration
policy.Validate();

// Check if a specific HTTP status code should trip the circuit
bool isFailure = policy.IsFailureStatus(503); // Returns true

// Check if the circuit breaker is enabled
bool isEnabled = policy.IsEnabled(); // Returns true
```

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

## RetryPolicy

The `RetryPolicy` class provides configurable retry behavior for transient operations, particularly useful for handling temporary failures in distributed systems. It supports both synchronous and asynchronous retry patterns with configurable retry counts, delays, and backoff strategies.

Example usage:

```csharp
using DotNetApiGateway.Integration;
using Microsoft.Extensions.Logging;

// Create a logger
var logger = new LoggerFactory().CreateLogger<RetryPolicy>();

// Create a retry policy with 3 attempts and exponential backoff
var retryPolicy = new RetryPolicy
{
    MaxAttempts = 3,
    InitialDelayMilliseconds = 100,
    MaxDelayMilliseconds = 5000,
    BackoffFactor = 2.0
};

// Execute an async operation with retry
var response = await retryPolicy.ExecuteAsync(async () =>
{
    var httpClient = new HttpClient();
    return await httpClient.GetAsync("https://api.example.com/data");
});

// Execute a typed async operation with retry
var data = await retryPolicy.ExecuteAsync<WeatherData>(async () =>
{
    var httpClient = new HttpClient();
    var response = await httpClient.GetAsync("https://api.example.com/weather");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return JsonUtility.Deserialize<WeatherData>(content);
});

// Check if retry policy is enabled
if (retryPolicy.IsEnabled)
{
    Console.WriteLine($"Retry enabled with {retryPolicy.MaxAttempts} attempts");
}
```

## RequestCoalescingPolicy

The `RequestCoalescingPolicy` class defines coalescing behavior for duplicate concurrent requests. When multiple identical requests arrive simultaneously, coalescing ensures only one upstream call is made and the result is shared with all waiters. This reduces load on upstream services and improves response times for duplicate requests.

## AggregationPolicy

The `AggregationPolicy` class defines how multiple upstream targets are aggregated when a request is processed. It supports different aggregation strategies (parallel, sequential, or conditional) and allows configuration of conditional targets that determine which upstream services receive the request based on conditions. Aggregation policies are useful for implementing canary deployments, blue-green deployments, A/B testing, or routing requests to different backend services based on request characteristics.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create an aggregation policy for parallel request distribution
var parallelPolicy = new AggregationPolicy
{
    Id = "parallel-distribution-policy",
    Enabled = true,
    Strategy = AggregationStrategy.Parallel,
    Targets = [
        new ConditionalAggregationTarget
        {
            Name = "primary-backend",
            BaseUrl = "https://primary.api.example.com",
            Weight = 70, // 70% of traffic
            Condition = "request.Headers.ContainsKey(\"X-Environment\") && request.Headers[\"X-Environment\"] == \"production\""
        },
        new ConditionalAggregationTarget
        {
            Name = "canary-backend",
            BaseUrl = "https://canary.api.example.com",
            Weight = 30, // 30% of traffic
            Condition = "request.Headers.ContainsKey(\"X-Environment\") && request.Headers[\"X-Environment\"] == \"canary\""
        }
    ]
};

// Create an aggregation policy for sequential request processing
var sequentialPolicy = new AggregationPolicy
{
    Id = "sequential-fallback-policy",
    Enabled = true,
    Strategy = AggregationStrategy.Sequential,
    Targets = [
        new ConditionalAggregationTarget
        {
            Name = "primary-service",
            BaseUrl = "https://primary.api.example.com",
            TimeoutSeconds = 30
        },
        new ConditionalAggregationTarget
        {
            Name = "secondary-service",
            BaseUrl = "https://secondary.api.example.com",
            TimeoutSeconds = 45
        },
        new ConditionalAggregationTarget
        {
            Name = "tertiary-service",
            BaseUrl = "https://backup.api.example.com",
            TimeoutSeconds = 60
        }
    ]
};

// Validate the aggregation policy configuration
parallelPolicy.Validate();
sequentialPolicy.Validate();

// Check if the policy is enabled
bool isEnabled = parallelPolicy.Enabled; // Returns true

// Get the aggregation strategy
var strategy = parallelPolicy.Strategy; // Returns AggregationStrategy.Parallel
```

## ApiVersioningPolicy

The `ApiVersioningPolicy` class configures API versioning behavior for gateway routes. It supports multiple versioning strategies (URL path, header, query parameter, and media type) that can be combined, with the first match determining the version. The policy allows setting a default version, requiring version headers, specifying supported versions, and controlling whether version segments are stripped from the path before forwarding to backend services.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create an API versioning policy with URL path and header strategies
var versioningPolicy = new ApiVersioningPolicy
{
  Enabled = true,
  DefaultVersion = "1",
  RequireVersion = false,
  Strategies = [
    VersioningStrategy.UrlPath,
    VersioningStrategy.Header,
    VersioningStrategy.QueryParameter
  ],
  SupportedVersions = ["1", "2", "3"],
  HeaderName = "X-API-Version",
  QueryParameterName = "api-version",
  StripVersionFromPath = true
};

// Validate the versioning policy configuration
versioningPolicy.Validate();

// Check if versioning is enabled
bool isEnabled = versioningPolicy.Enabled; // Returns true

// Check if a specific version is supported
bool isSupported = versioningPolicy.SupportedVersions.Contains("2"); // Returns true

// Get the configured header name
string headerName = versioningPolicy.HeaderName; // Returns "X-API-Version"
```

// Create a request coalescing policy with default settings
var policy = new RequestCoalescingPolicy
{
    Id = "user-profile-policy",
    Enabled = true,
    TimeoutMs = 5000, // Wait up to 5 seconds for a coalesced response
    MaxQueuedRequests = 200, // Allow up to 200 followers to queue
    CoalescibleMethods = ["GET", "HEAD"], // Only coalesce GET and HEAD requests
    IncludeQueryString = true // Include query parameters in coalescing key
};

// Validate the policy configuration
policy.Validate();

// Check if a specific HTTP method can be coalesced
bool canCoalesce = policy.IsCoalescible("GET"); // Returns true

// Generate a coalescing key for a request
var queryParams = new Dictionary<string, string> { ["userId"] = "123", ["fields"] = "name,email" };
string coalescingKey = policy.GenerateCoalescingKey(
    "/api/users/123",
    "GET",
    queryParams
);
// Returns: "GET:/api/users/123?fields=name,email&userId=123"
```

## WebhookRegistry

The `WebhookRegistry` class manages webhook subscriptions and provides functionality to asynchronously publish domain events to subscribed endpoints. It allows for registering and unregistering subscriptions, filtering by event type, and configuring delivery retry policies with exponential backoff.

Example usage:

```csharp
using DotNetApiGateway.Integration;
using Microsoft.Extensions.Logging;

// Create a logger and instantiate the registry
var logger = new LoggerFactory().CreateLogger<WebhookRegistry>();
var registry = new WebhookRegistry(logger);

// Register a webhook subscription
var subscription = new WebhookSubscription
{
    Id = "sub_001",
    CallbackUrl = "https://hooks.example.com/receive",
    EventTypes = new[] { "order.created" },
    RetryPolicy = new WebhookRetryPolicy { MaxRetries = 3, InitialDelayMs = 500, MaxDelayMs = 5000 }
};
registry.Register(subscription);

// Publish an event to subscribers
var webhookEvent = new WebhookEvent
{
    EventType = "order.created",
    Data = new { OrderId = 12345, Status = "Created" }
};
await registry.PublishEventAsync(webhookEvent);

// Retrieve and manage subscriptions
var allSubscriptions = registry.GetAllSubscriptions();
var orderSubscriptions = registry.GetSubscriptionsForEvent("order.created");
registry.Unregister("sub_001");
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

## HttpClientFactory

The `HttpClientFactory` class provides a factory for creating and managing pooled HTTP client instances. It reuses HTTP clients for better performance and proper connection pooling, reducing the overhead of creating new HTTP clients for each request. The factory is thread-safe and supports timeout configuration, client removal, and cleanup operations.

Example usage:

```csharp
using DotNetApiGateway.Integration;
using Microsoft.Extensions.Logging;

// Create a logger (replace with real logger in production)
var logger = new LoggerFactory().CreateLogger<HttpClientFactory>();

// Create the HTTP client factory
var clientFactory = new HttpClientFactory(logger);

// Get or create a pooled HTTP client for a specific base URL
var client = clientFactory.GetClient("https://api.example.com");

// Make requests using the pooled client
var response = await client.GetAsync("/data");
var content = await response.Content.ReadAsStringAsync();

// Create a transient client for one-off requests (no pooling)
var transientClient = clientFactory.CreateTransientClient();
var transientResponse = await transientClient.GetAsync("https://api.example.com/temp");

// Update client timeout configuration
clientFactory.SetClientTimeout("https://api.example.com", TimeSpan.FromSeconds(60));

// Get current client count
int clientCount = clientFactory.GetClientCount();
Console.WriteLine($"Active clients: {clientCount}");

// Remove a specific client
clientFactory.RemoveClient("https://api.example.com");

// Clear all cached clients
clientFactory.Clear();
```

## ExternalApiClient

The `ExternalApiClient` class provides a generic HTTP client wrapper for calling external APIs with built-in error handling, retry logic, and logging. It simplifies making HTTP requests to external services while providing resilience against transient failures.

Example usage:

```csharp
using DotNetApiGateway.Integration;
using Microsoft.Extensions.Logging;

// Create an HTTP client (typically injected via DI in production)
var httpClient = new HttpClient();

// Create a logger (replace with real logger in production)
var logger = new LoggerFactory().CreateLogger<ExternalApiClient>();

// Create the external API client with optional retry policy
var apiClient = new ExternalApiClient(httpClient, logger: logger);

// Make a GET request to retrieve data
var userData = await apiClient.GetAsync<UserData>("https://api.example.com/users/123");

// Make a POST request to create a resource
var newUser = new CreateUserRequest { Name = "John Doe", Email = "john@example.com" };
var createdUser = await apiClient.PostAsync<CreateUserRequest, UserResponse>(
    "https://api.example.com/users", 
    newUser
);

// Make a PUT request to update a resource
var updatedUser = new UpdateUserRequest { Name = "John Updated", Email = "john.updated@example.com" };
var result = await apiClient.PutAsync<UpdateUserRequest, UserResponse>(
    "https://api.example.com/users/123", 
    updatedUser
);

// Make a DELETE request to remove a resource
var isDeleted = await apiClient.DeleteAsync("https://api.example.com/users/123");

// Send a pre-built HTTP request
var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
request.Headers.Add("Authorization", "Bearer token123");
var response = await apiClient.SendRequestAsync(request);

// Make a raw HTTP request with custom configuration
var customResponse = await apiClient.SendAsync(
    "https://api.example.com/custom-endpoint",
    HttpMethod.Post,
    contentType: "application/json",
    content: "{\"key\": \"value\"}",
    headers: new Dictionary<string, string> { ["X-Custom-Header"] = "custom-value" }
);
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

## JwtValidationServiceTests

The `JwtValidationServiceTests` class contains comprehensive unit tests for the `JwtValidationService` class. These tests cover scenarios such as validating valid and invalid JWT tokens, handling different authentication policies, and testing various edge cases like token expiration, invalid signatures, and more. The test suite ensures the JWT validation service behaves as expected under various conditions.

Example usage:

```csharp
var service = new JwtValidationService();
var token = JwtValidationServiceTests.GenerateTestToken(JwtValidationServiceTests.TestSecret);

var policy = new AuthenticationPolicy
{
    Enabled = true,
    Type = AuthenticationType.Bearer,
    ValidateSignature = true,
    ValidateExpiration = true,
    JwtSecret = JwtValidationServiceTests.TestSecret,
    JwtIssuer = JwtValidationServiceTests.TestIssuer,
    JwtAudience = JwtValidationServiceTests.TestAudience,
};

var identity = await service.ValidateTokenAsync(token, policy);
Assert.NotNull(identity);
Assert.Equal("user-123", identity.Id);
```

## AuthenticationPolicy

The `AuthenticationPolicy` class defines authentication and authorization requirements for API gateway routes. It configures JWT validation settings, allowed scopes, and role-based access control for securing gateway endpoints. The policy supports Bearer token authentication with configurable signature validation, expiration checking, and algorithm support.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create an authentication policy for JWT Bearer tokens
var authPolicy = new AuthenticationPolicy
{
    Id = "user-auth-policy",
    Enabled = true,
    Type = AuthenticationType.Bearer,
    ValidateSignature = true,
    ValidateExpiration = true,
    ClockSkewSeconds = 30,
    JwtIssuer = "https://auth.example.com",
    JwtAudience = "api.example.com",
    JwtSecret = "your-256-bit-secret-key-here",
    JwtAlgorithms = ["HS256", "HS384"],
    AllowedScopes = ["read:users", "write:users"],
    AllowedRoles = ["admin", "user"],
    RequiresAuthentication() // Returns true
};

// Validate the policy configuration
authPolicy.Validate();

// Check if the policy requires authentication
bool requiresAuth = authPolicy.RequiresAuthentication(); // Returns true

// Check if the policy has scope requirements
bool hasScopes = authPolicy.HasScopeRequirements(); // Returns true

// Check if the policy has role requirements
bool hasRoles = authPolicy.HasRoleRequirements(); // Returns true
```

## RouteTarget

The `RouteTarget` class represents a backend service target for a route. It defines the upstream service configuration including connection details, health monitoring, load balancing weight, and request transformation settings. Route targets are used within `GatewayRoute` configurations to specify where requests should be forwarded.

Each target has a unique identifier, display name, base URL, optional port and timeout settings, health check configuration, header transformation rules, and load balancing weight. The `RouteTarget` class provides methods for URL construction, health status updates, and configuration validation.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a primary backend target with health monitoring
var primaryTarget = new RouteTarget
{
    Id = "user-service-primary",
    Name = "User Service - Primary",
    BaseUrl = "https://user-service.internal",
    Port = 8080,
    Weight = 70,
    IsHealthy = true,
    HealthCheckPath = "/health",
    HealthCheckIntervalSeconds = 30,
    TimeoutSeconds = 30,
    StripPathPrefix = true,
    TransformHeaders = new Dictionary<string, string>
    {
        ["X-Service-Version"] = "v2",
        ["X-Environment"] = "production"
    }
};

// Create a secondary backup target with lower weight
var backupTarget = new RouteTarget
{
    Id = "user-service-backup",
    Name = "User Service - Backup",
    BaseUrl = "https://backup-service.internal",
    Port = 8080,
    Weight = 30,
    IsHealthy = true,
    HealthCheckIntervalSeconds = 60,
    TimeoutSeconds = 45
};

// Validate the target configuration
primaryTarget.Validate();
backupTarget.Validate();

// Build the forward URL for a request
string requestPath = "/api/v1/users/123";
string forwardUrl = primaryTarget.GetForwardUrl(requestPath);
// Returns: "https://user-service.internal:8080/api/v1/users/123"

// Update health status based on health check results
primaryTarget.UpdateHealthStatus(isHealthy: true, error: null);
```

## GatewayRoute

The `GatewayRoute` class represents a route configuration in the API gateway. It defines how incoming requests are matched, processed, and forwarded to upstream services with support for rate limiting, circuit breaking, caching, authentication, request coalescing, aggregation, API versioning, and request/response transformations.

Each route has a unique identifier, name, path pattern with support for wildcards and parameters, allowed HTTP methods, and one or more target endpoints. Routes can be configured with various policies for resilience, security, and performance optimization.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a gateway route for user management API
var userRoute = new GatewayRoute
{
    Id = "user-management-route",
    Name = "User Management API",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET", "PUT", "DELETE"],
    Targets = [
        new RouteTarget
        {
            Name = "user-service-primary",
            BaseUrl = "https://user-service.internal:8080",
            Weight = 70,
            IsHealthy = true,
            HealthCheckIntervalSeconds = 30
        },
        new RouteTarget
        {
            Name = "user-service-secondary",
            BaseUrl = "https://user-service-backup.internal:8080",
            Weight = 30,
            IsHealthy = true,
            HealthCheckIntervalSeconds = 60
        }
    ],
    RateLimitPolicy = new RateLimitPolicy
    {
        Enabled = true,
        RequestsPerMinute = 1000,
        Strategy = RateLimitStrategy.SlidingWindow
    },
    CircuitBreakerPolicy = new CircuitBreakerPolicy
    {
        Enabled = true,
        FailureThreshold = 5,
        SuccessThreshold = 3,
        TimeoutSeconds = 30,
        FailureStatusCodes = [500, 502, 503, 504],
        MaxRetries = 2,
        RetryDelayMilliseconds = 100
    },
    CachePolicy = new CachePolicy
    {
        Enabled = true,
        DurationSeconds = 300,
        VaryByHeaders = ["Accept-Language"]
    },
    AuthenticationPolicy = new AuthenticationPolicy
    {
        Enabled = true,
        Type = AuthenticationType.Bearer,
        ValidateSignature = true,
        ValidateExpiration = true,
        JwtSecret = "your-secret-key",
        JwtIssuer = "your-issuer",
        JwtAudience = "your-audience"
    },
    RequestCoalescingPolicy = new RequestCoalescingPolicy
    {
        Enabled = true,
        TimeoutMs = 5000,
        MaxQueuedRequests = 100,
        CoalescibleMethods = ["GET", "HEAD"],
        IncludeQueryString = true
    },
    AggregationPolicy = new AggregationPolicy
    {
        Enabled = true,
        Strategy = AggregationStrategy.Parallel,
        Targets = [
            new ConditionalAggregationTarget
            {
                Name = "primary-backend",
                BaseUrl = "https://api.example.com",
                Weight = 80,
                Condition = "request.Headers.ContainsKey(\"X-Environment\") && request.Headers[\"X-Environment\"] == \"production\""
            },
            new ConditionalAggregationTarget
            {
                Name = "canary-backend",
                BaseUrl = "https://canary.api.example.com",
                Weight = 20,
                Condition = "request.Headers.ContainsKey(\"X-Environment\") && request.Headers[\"X-Environment\"] == \"canary\""
            }
        ]
    },
    VersioningPolicy = new ApiVersioningPolicy
    {
        Enabled = true,
        Strategies = [
            VersioningStrategy.UrlPath,
            VersioningStrategy.Header,
            VersioningStrategy.QueryParameter
        ],
        SupportedVersions = ["1", "2"],
        DefaultVersion = "1",
        HeaderName = "X-API-Version"
    },
    TransformationRules = [
        new TransformationRule
        {
            Id = "add-tenant-header",
            Phase = TransformationPhase.Request,
            Operation = TransformationOperation.AddHeader,
            Key = "X-Tenant-Id",
            Value = "acme-corp",
            Order = 1,
            IsEnabled = true
        },
        new TransformationRule
        {
            Id = "set-environment-param",
            Phase = TransformationPhase.Request,
            Operation = TransformationOperation.SetQueryParam,
            Key = "env",
            Value = "production",
            Order = 2,
            IsEnabled = true
        }
    ],
    IsActive = true,
    TimeoutSeconds = 60,
    CustomHeaders = new Dictionary<string, string>
    {
        ["X-Gateway-Version"] = "2.0",
        ["X-Built-At"] = DateTime.UtcNow.ToString("o")
    },
    CreatedAt = DateTime.UtcNow
};

// Validate the route configuration
userRoute.Validate();

// Check if the route matches a request path
bool matchesPath = userRoute.MatchesPath("/api/users/123"); // Returns true
bool matchesWildcard = userRoute.MatchesPath("/api/users/details"); // Returns true

// Check if the route supports a specific HTTP method
bool supportsGet = userRoute.SupportsMethod("GET"); // Returns true
bool supportsPost = userRoute.SupportsMethod("POST"); // Returns false
```

## RoutingServiceTests

The `RoutingServiceTests` class provides a comprehensive test suite for the `RoutingService` class. It ensures the correctness of routing logic, target selection strategies, URL construction, and gateway operations. The tests cover route discovery, target selection algorithms, and CRUD operations on gateway routes.

Example usage:

```csharp
var repository = new GatewayRouteRepository();
var service = new RoutingService(repository);

var healthyTarget = new RouteTarget { BaseUrl = "http://backend:8080" };
var route = new GatewayRoute
{
    Name = "TestRoute",
    PathPattern = "/api/test",
    AllowedMethods = ["GET"],
    Targets = [healthyTarget],
    TimeoutSeconds = 30
};

// Test finding an existing route
await repository.AddAsync(route);
var foundRoute = await service.FindRouteAsync("/api/test", "GET");
Assert.NotNull(foundRoute);
Assert.Equal("TestRoute", foundRoute.Name);

// Test selecting a target using round-robin strategy
var target1 = service.SelectTarget(route);
var target2 = service.SelectTarget(route);
Assert.NotNull(target1);
Assert.NotNull(target2);

// Test building a forward URL
var forwardUrl = service.BuildForwardUrl(target1, "/api/test/123");
Assert.Equal("http://backend:8080/api/test/123", forwardUrl);

// Apply header transformations
var originalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };
var transformedHeaders = service.ApplyHeaderTransforms(healthyTarget, originalHeaders);
Assert.Contains("Authorization", transformedHeaders);
Assert.Contains("X-Gateway-Version", transformedHeaders);
```

## DateTimeUtilityTests

The `DateTimeUtilityTests` class provides a comprehensive test suite for the `DateTimeUtility` static class, which offers various date and time manipulation utilities. These utilities include Unix timestamp conversion, date formatting, relative time formatting, and business day calculations. The class is useful for API responses, logging, scheduling, and any time-sensitive operations within the gateway.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using System;

// Convert current time to Unix timestamp
long currentTimestamp = DateTimeUtility.ToUnixTimestamp(DateTime.UtcNow);
Console.WriteLine($"Current Unix timestamp: {currentTimestamp}");

// Convert Unix timestamp back to DateTime
DateTime dateFromTimestamp = DateTimeUtility.FromUnixTimestamp(1672531200L);
Console.WriteLine($"Date from timestamp: {dateFromTimestamp}");

// Format a DateTime to a standardized string
string formattedDate = DateTimeUtility.FormatDateTime(DateTime.Now);
Console.WriteLine($"Formatted date: {formattedDate}");

// Get relative time description (e.g., "just now", "5 minutes ago")
string relativeTime = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddMinutes(-2));
Console.WriteLine($"Relative time: {relativeTime}");

// Check if a date is in the past
bool isPast = DateTimeUtility.IsPast(DateTime.UtcNow.AddDays(-1));
Console.WriteLine($"Is past: {isPast}");

// Check if a date is in the future
bool isFuture = DateTimeUtility.IsFuture(DateTime.UtcNow.AddDays(1));
Console.WriteLine($"Is future: {isFuture}");

// Check if two dates are on the same day
bool isSameDay = DateTimeUtility.IsSameDay(
    new DateTime(2023, 1, 1, 10, 0, 0),
    new DateTime(2023, 1, 1, 20, 0, 0)
);
Console.WriteLine($"Is same day: {isSameDay}");

// Calculate business days between two dates
int businessDays = DateTimeUtility.GetBusinessDaysBetween(
    new DateTime(2023, 1, 2),  // Monday
    new DateTime(2023, 1, 6)   // Friday
);
Console.WriteLine($"Business days between: {businessDays}");
```

## UrlUtilityTests

The `UrlUtilityTests` class provides a comprehensive test suite for the `UrlUtility` class, which offers various URL manipulation and parsing utilities. The tests cover URL combination, query string parsing and building, URL sanitization, and URL validation utilities.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// Combine URLs with proper slash handling
string combinedUrl = UrlUtility.CombineUrl("https://api.example.com/", "/v1/users");
// Result: "https://api.example.com/v1/users"

// Parse query strings
var queryParams = UrlUtility.ParseQueryString("?name=John%20Doe&city=New%20York&color=red&color=blue");
// queryParams["name"] == "John Doe"
// queryParams["city"] == "New York"
// queryParams["color"] == "red" (first value kept)

// Build query strings
string queryString = UrlUtility.BuildQueryString(new Dictionary<string, string> 
{
    ["page"] = "1",
    ["limit"] = "20"
});
// Result: "?page=1&limit=20"

// Sanitize URLs (mask sensitive parameters)
string sanitizedUrl = UrlUtility.SanitizeUrl(
    "https://api.example.com/data?token=secret123&page=1&api_key=my-key");
// Result: "https://api.example.com/data?token=***&page=1&api_key=***"

// Validate URLs
bool isValid = UrlUtility.IsValidUrl("https://api.example.com");
// Result: true

// Extract URL components
string hostname = UrlUtility.GetHostname("https://api.example.com/v1/users?page=1");
// Result: "api.example.com"

int port = UrlUtility.GetPort("https://api.example.com:8080/api");
// Result: 8080

// Check query parameters
bool hasParam = UrlUtility.HasQueryParameter("https://api.example.com/data?page=1", "page");
// Result: true
```

## TransformationRule

The `TransformationRule` class defines transformation operations that can be applied to HTTP requests and responses during the request processing pipeline. It supports various transformation operations such as adding/setting headers, managing query parameters, rewriting paths, and modifying request/response content. Each rule can be configured with a specific phase (request or response), operation type, target key/value, execution order, and enabled/disabled state.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a transformation rule for adding a custom header to requests
var addHeaderRule = new TransformationRule
{
    Id = "add-tenant-header",
    Description = "Add tenant identifier to all requests",
    Phase = TransformationPhase.Request,
    Operation = TransformationOperation.AddHeader,
    Key = "X-Tenant-Id",
    Value = "acme-corp",
    Order = 1,
    IsEnabled = true
};

// Create a transformation rule for setting a query parameter
var setQueryParamRule = new TransformationRule
{
    Id = "set-environment-param",
    Description = "Set environment parameter for backend routing",
    Phase = TransformationPhase.Request,
    Operation = TransformationOperation.SetQueryParam,
    Key = "env",
    Value = "production",
    Order = 2,
    IsEnabled = true
};

// Create a transformation rule for rewriting path prefixes
var rewritePathRule = new TransformationRule
{
    Id = "rewrite-path-prefix",
    Description = "Rewrite API version prefix in path",
    Phase = TransformationPhase.Request,
    Operation = TransformationOperation.RewritePathPrefix,
    Key = "/v1",
    Value = "/api/v1",
    Order = 3,
    IsEnabled = true
};

// Create a transformation rule for response header manipulation
var responseHeaderRule = new TransformationRule
{
    Id = "add-gateway-version",
    Description = "Add gateway version header to responses",
    Phase = TransformationPhase.Response,
    Operation = TransformationOperation.SetHeader,
    Key = "X-Gateway-Version",
    Value = "2.0",
    Order = 1,
    IsEnabled = true
};

// Validate the rule configuration
addHeaderRule.Validate();
setQueryParamRule.Validate();

// Check if a rule is enabled
bool isEnabled = addHeaderRule.IsEnabled; // Returns true

// Get rule priority/order
int executionOrder = addHeaderRule.Order; // Returns 1
```

## RequestTransformationServiceTests

The `RequestTransformationServiceTests` class provides a comprehensive test suite for the `RequestTransformationService` class. It tests the request and response transformation rules that can be applied to HTTP requests and responses, including header manipulation, query parameter management, path rewriting, and rule validation. The tests cover both request-phase and response-phase operations, ensuring that transformations are applied correctly according to the specified rules.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using HttpMethod = System.Net.Http.HttpMethod;

// Create the transformation service
var service = new RequestTransformationService(NullLogger<RequestTransformationService>.Instance);

// Create a sample HTTP request
var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/users");

// Define transformation rules for the request phase
var requestRules = new List<TransformationRule>
{
    new()
    {
        Phase = TransformationPhase.Request,
        Operation = TransformationOperation.AddHeader,
        Key = "X-Tenant-Id",
        Value = "acme"
    },
    new()
    {
        Phase = TransformationPhase.Request,
        Operation = TransformationOperation.SetQueryParam,
        Key = "env",
        Value = "production"
    },
    new()
    {
        Phase = TransformationPhase.Request,
        Operation = TransformationOperation.RewritePathPrefix,
        Key = "/v1",
        Value = "/api/v1"
    }
};

// Apply the transformation rules to the request
service.ApplyRequestRules(request, requestRules);

// Verify the transformations were applied
request.Headers.TryGetValues("X-Tenant-Id", out var tenantValues).Should().BeTrue();
tenantValues!.First().Should().Be("acme");

request.RequestUri!.Query.Should().Contain("env=production");
request.RequestUri.AbsolutePath.Should().Be("/api/v1/users");

// Create a sample HTTP response
var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

// Define transformation rules for the response phase
var responseRules = new List<TransformationRule>
{
    new()
    {
        Phase = TransformationPhase.Response,
        Operation = TransformationOperation.SetHeader,
        Key = "X-Gateway-Version",
        Value = "2.0"
    },
    new()
    {
        Phase = TransformationPhase.Response,
        Operation = TransformationOperation.RemoveHeader,
        Key = "Server"
    }
};

// Apply the transformation rules to the response
service.ApplyResponseRules(response, responseRules);

// Verify the transformations were applied
response.Headers.TryGetValues("X-Gateway-Version", out var versionValues).Should().BeTrue();
versionValues!.First().Should().Be("2.0");
response.Headers.Contains("Server").Should().BeFalse();

// Test rule validation
var invalidRule = new TransformationRule
{
    Operation = TransformationOperation.AddHeader,
    Key = "",
    Value = "value"
};

var act = () => invalidRule.Validate();
act.Should().Throw<ArgumentException>();
```

## ValidationUtilityTests

The `ValidationUtilityTests` class provides a comprehensive test suite for the `ValidationUtility` class, which offers various validation utilities for common data types and formats. These tests cover email validation, URL validation, IP address validation, UUID validation, string validation, collection validation, type checking, and HTTP-specific validations.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using System.Collections.Generic;

// Validate email addresses
bool isValidEmail = ValidationUtility.IsValidEmail("user@example.com");
// Result: true

bool isInvalidEmail = ValidationUtility.IsValidEmail("invalid.email");
// Result: false

// Validate URLs
bool isValidUrl = ValidationUtility.IsValidUrl("https://api.example.com/v1/users");
// Result: true

bool isInvalidUrl = ValidationUtility.IsValidUrl("ftp://files.example.com");
// Result: false

// Validate IP addresses
bool isValidIp = ValidationUtility.IsValidIpAddress("192.168.1.1");
// Result: true

bool isInvalidIp = ValidationUtility.IsValidIpAddress("256.1.1.1");
// Result: true (basic regex validation)

// Validate UUIDs
bool isValidUuid = ValidationUtility.IsValidUuid("550e8400-e29b-41d4-a716-446655440000");
// Result: true

bool isInvalidUuid = ValidationUtility.IsValidUuid("invalid-uuid-format");
// Result: false

// Validate strings
bool isNullOrEmpty = ValidationUtility.IsNullOrEmpty("");
// Result: true

bool isAlphanumeric = ValidationUtility.IsAlphanumeric("abc123");
// Result: true

bool isAsciiOnly = ValidationUtility.IsAsciiOnly("Hello World");
// Result: true

bool isValidLength = ValidationUtility.IsValidLength("hello", 0, 10);
// Result: true

// Validate ports
bool isValidPort = ValidationUtility.IsValidPort(8080);
// Result: true

bool isInvalidPort = ValidationUtility.IsValidPort(65536);
// Result: false

// Validate HTTP methods
bool isValidHttpMethod = ValidationUtility.IsValidHttpMethod("GET");
// Result: true

bool isInvalidHttpMethod = ValidationUtility.IsValidHttpMethod("INVALID");
// Result: false

// Validate HTTP status codes
bool isValidStatusCode = ValidationUtility.IsValidHttpStatusCode(200);
// Result: true

bool isInvalidStatusCode = ValidationUtility.IsValidHttpStatusCode(99);
// Result: false

// Validate null values
bool isNull = ValidationUtility.IsNull(null);
// Result: true

bool isNotNull = ValidationUtility.IsNull(new object());
// Result: false

// Validate types
bool isCorrectType = ValidationUtility.IsValidType<string>("string");
// Result: true

bool isWrongType = ValidationUtility.IsValidType<string>(123);
// Result: false

// Validate collections
bool isNullOrEmptyCollection = ValidationUtility.IsNullOrEmpty((List<int>)null);
// Result: true

bool isEmptyCollection = ValidationUtility.IsNullOrEmpty(new List<int>());
// Result: true

bool isPopulatedCollection = ValidationUtility.IsNullOrEmpty(new List<int> { 1, 2, 3 });
// Result: false

// Validate dictionary keys
bool hasRequiredKeys = ValidationUtility.HasRequiredKeys(
    new Dictionary<string, string> { ["name"] = "John", ["email"] = "john@example.com" },
    "name", "email"
);
// Result: true

bool missingRequiredKey = ValidationUtility.HasRequiredKeys(
    new Dictionary<string, string> { ["name"] = "John" },
    "name", "email"
);
// Result: false
```

## CircuitBreakerStatusTests

The `CircuitBreakerStatusTests` class provides a comprehensive test suite for the `CircuitBreakerStatus` class, which tracks the state and metrics of circuit breaker instances. These tests verify the correct behavior of success/failure recording, state transitions, rate calculations, and counter resets, ensuring the circuit breaker accurately reflects system health and recovery patterns.

Example usage:

```csharp
using DotNetApiGateway.Models;
using Xunit;

// Create a new circuit breaker status in initial Closed state
var status = new CircuitBreakerStatus();

// Record successful operations
status.RecordSuccess();
Assert.Equal(1, status.SuccessCount);
Assert.Equal(1, status.TotalSuccesses);
Assert.Equal(1, status.TotalRequests);
Assert.Null(status.LastError);

// Record failures
status.RecordFailure("Connection timeout");
Assert.Equal(1, status.FailureCount);
Assert.Equal(1, status.TotalFailures);
Assert.Equal("Connection timeout", status.LastError);

// Calculate success/failure rates
var successRate = status.GetSuccessRate(); // Returns 0.5 for 1 success, 1 failure
var failureRate = status.GetFailureRate(); // Returns 0.5
Assert.Equal(1.0, successRate + failureRate);

// Change circuit state and verify counter resets
status.ChangeState(CircuitBreakerState.Closed);
Assert.Equal(0, status.FailureCount);
Assert.Equal(0, status.SuccessCount);

// Reset circuit breaker to initial state
status.Reset();
Assert.Equal(CircuitBreakerState.Closed, status.State);
Assert.Equal(0, status.FailureCount);
Assert.Equal(0, status.SuccessCount);
Assert.Null(status.LastError);
```

## GatewayRouteTests

The `GatewayRouteTests` class provides a comprehensive test suite for the `GatewayRoute` class, which represents a route configuration in the API gateway. These tests verify path matching logic, HTTP method support, and route validation rules. The test suite covers exact path matching, wildcard segments, parameter segments, case-insensitive matching, and various validation scenarios for route configuration.

Example usage:

```csharp
using DotNetApiGateway.Models;
using System;
using Xunit;

// Create a route with a parameter segment
var route = new GatewayRoute
{
    Name = "UserRoute",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET", "PUT"],
    Targets = [
        new RouteTarget
        {
            Name = "backend",
            BaseUrl = "http://backend:8080",
            Weight = 1,
            HealthCheckIntervalSeconds = 60
        }
    ],
    TimeoutSeconds = 30
};

// Test path matching with exact static path
bool matchesExact = route.MatchesPath("/api/health");
// Result: true

// Test path matching with wildcard segment
bool matchesWildcard = route.MatchesPath("/api/users/details");
// Result: true (wildcard "*" matches "users")

// Test path matching with parameter segment
bool matchesParameter = route.MatchesPath("/api/users/123");
// Result: true (parameter {id} matches "123")

// Test path matching with too many segments
bool tooManySegments = route.MatchesPath("/api/users/123/orders");
// Result: false

// Test path matching with too few segments
bool tooFewSegments = route.MatchesPath("/api/users");
// Result: false

// Test case-insensitive path matching
bool caseInsensitive = route.MatchesPath("/API/USERS/123");
// Result: true (case-insensitive matching)

// Test HTTP method support
bool supportsGet = route.SupportsMethod("GET");
// Result: true

bool supportsDelete = route.SupportsMethod("DELETE");
// Result: false

bool supportsCaseInsensitiveMethod = route.SupportsMethod("put");
// Result: true (case-insensitive method matching)

// Test route validation
try
{
    var invalidRoute = new GatewayRoute
    {
        Name = "",
        PathPattern = "/api/test",
        AllowedMethods = ["GET"],
        Targets = [new RouteTarget { Name = "target", BaseUrl = "http://test" }],
        TimeoutSeconds = 30
    };
    invalidRoute.Validate();
    // Should not reach here
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    // Expected: Validation failed: ...
}

// Test timeout validation
var routeWithInvalidTimeout = new GatewayRoute
{
    Name = "TestRoute",
    PathPattern = "/api/test",
    AllowedMethods = ["GET"],
    Targets = [new RouteTarget { Name = "target", BaseUrl = "http://test" }],
    TimeoutSeconds = 0
};

try
{
    routeWithInvalidTimeout.Validate();
    // Should not reach here
}

catch (ArgumentException ex)
{
    Console.WriteLine($"Timeout validation failed: {ex.Message}");
    // Expected: Timeout validation failed: ...
}

## RequestContextTests

The `RequestContextTests` class provides a comprehensive test suite for the `RequestContext` class, which holds request-scoped metadata such as identifiers, client identity, authentication tokens, headers, query parameters, custom data, and elapsed time. These tests verify the correct initialization, identifier generation, and property manipulation logic, ensuring that the request context reliably maintains state throughout the request lifecycle.

Example usage (using the `RequestContext` class):

```csharp
using DotNetApiGateway.Models;
using System;
using System.Collections.Generic;

// Instantiate the RequestContext
var context = new RequestContext();

// Verify default values and ID generation
string requestId = context.RequestId;

// Client identification
context.ClientIdentity = new ClientIdentity { Id = "client-123" };
string clientId = context.GetClientIdentifier(); // Returns "client-123"

context.ClientIdentity = null;
context.ClientIp = "192.168.1.1";
string ip = context.GetClientIdentifier(); // Returns "192.168.1.1"

// Authentication token handling
context.AuthToken = "Bearer my-token";
bool hasToken = context.HasAuthToken(); // Returns true
string token = context.ExtractBearerToken(); // Returns "my-token"

// Elapsed time tracking
context.ReceivedAt = DateTime.UtcNow.AddSeconds(-2);
TimeSpan elapsed = context.ElapsedTime(); // Returns ~2 seconds

// Modifying headers, query parameters, and custom data
context.Headers["X-Custom-Header"] = "value";
context.QueryParameters["page"] = "1";
context.CustomData["key"] = "value";
```

## ApiVersioningServiceTests

The `ApiVersioningServiceTests` class provides a comprehensive test suite for the `ApiVersioningService` class, covering various versioning strategies (URL-path, header, query-parameter, media-type), default version handling, required version validation, supported version filtering, policy disabling, path stripping, and strategy priority behavior. The tests verify that the API versioning service correctly resolves versions from different sources and handles edge cases according to the configured policy.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

// Create the API versioning service
var versioningService = new ApiVersioningService(NullLogger<ApiVersioningService>.Instance);

// Configure a versioning policy with multiple strategies
var policy = new ApiVersioningPolicy
{
    Enabled = true,
    Strategies = [
        VersioningStrategy.UrlPath,
        VersioningStrategy.Header,
        VersioningStrategy.QueryParameter,
        VersioningStrategy.MediaType
    ],
    SupportedVersions = ["1", "2", "3"],
    DefaultVersion = "1",
    StripVersionFromPath = true,
    HeaderName = "X-API-Version"
};

// Create a mock HTTP context
var context = new DefaultHttpContext();

// Example 1: Resolve version from URL path (e.g., /v2/users)
context.Request.Path = "/v2/users";
if (versioningService.TryResolveVersion(context, policy, out var version))
{
    Console.WriteLine($"Resolved version: {version}"); // Outputs: Resolved version: 2
    Console.WriteLine($"Stripped path: {versioningService.StripVersionFromPath(context.Request.Path, policy)}"); // Outputs: Stripped path: /users
}

// Example 2: Resolve version from custom header
context.Request.Path = "/api/data";
context.Request.Headers["X-API-Version"] = "3";
if (versioningService.TryResolveVersion(context, policy, out version))
{
    Console.WriteLine($"Resolved version from header: {version}"); // Outputs: Resolved version from header: 3
}

// Example 3: Resolve version from query parameter
context.Request.Path = "/api/items";
context.Request.QueryString = new QueryString("?api-version=2");
if (versioningService.TryResolveVersion(context, policy, out version))
{
    Console.WriteLine($"Resolved version from query: {version}"); // Outputs: Resolved version from query: 2
}

// Example 4: Resolve version from Accept header (media type)
context.Request.Path = "/api/catalog";
context.Request.Headers.Accept = "application/vnd.myapi.v1+json";
if (versioningService.TryResolveVersion(context, policy, out version))
{
    Console.WriteLine($"Resolved version from media type: {version}"); // Outputs: Resolved version from media type: 1
}

// Example 5: Use default version when no version is specified
context.Request.Path = "/api/users";
context.Request.Headers.Clear();
context.Request.QueryString = QueryString.Empty;
if (versioningService.TryResolveVersion(context, policy, out version))
{
    Console.WriteLine($"Using default version: {version}"); // Outputs: Using default version: 1
}

// Example 6: Check if a version is supported
var supportedVersions = new[] { "1", "2", "3" };
if (supportedVersions.Contains(version))
{
    Console.WriteLine("Version is supported");
}
```

## RateLimitingServiceTests

The `RateLimitingServiceTests` class provides a comprehensive test suite for the `RateLimitingService` class, which handles rate limiting enforcement for API gateway clients. These tests cover various rate limiting scenarios including disabled policies, valid requests, exceeded limits, different rate limiting strategies (sliding window and token bucket), and cleanup operations. The test suite verifies that the rate limiting service correctly tracks request counts, calculates remaining capacity, and enforces policy limits across multiple clients.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Create mock dependencies
var mockFactory = new Mock<IRateLimitStoreFactory>();
var mockStore = new Mock<IRateLimitStore>();
var logger = new Mock<ILogger<RateLimitingService>>();

// Setup the factory to return our mock store
mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

// Create the rate limiting service
var rateLimitingService = new RateLimitingService(mockFactory.Object, logger.Object);

// Define a rate limit policy (e.g., 100 requests per minute using sliding window)
var policy = new RateLimitPolicy
{
    Enabled = true,
    RequestsPerMinute = 100,
    Strategy = RateLimitStrategy.SlidingWindow
};

// Test if a request is allowed (returns true if within limit)
bool isAllowed = await rateLimitingService.IsAllowedAsync("client-123", policy);
Console.WriteLine($"Request allowed: {isAllowed}");

// Get rate limit information (remaining requests, reset time, etc.)
var rateLimitInfo = await rateLimitingService.GetRateLimitInfoAsync("client-123", policy);
Console.WriteLine($"Limit: {rateLimitInfo.Limit}, Remaining: {rateLimitInfo.Remaining}, Reset in: {rateLimitInfo.Reset} seconds");

// Reset limits for a specific client
await rateLimitingService.ResetKeyLimitsAsync("client-123");

// Reset all limits across all clients
await rateLimitingService.ResetAllLimitsAsync();

// Test with token bucket strategy
var tokenBucketPolicy = new RateLimitPolicy
{
    Enabled = true,
    BurstSize = 50,
    Strategy = RateLimitStrategy.TokenBucket
};

var tokenBucketInfo = await rateLimitingService.GetRateLimitInfoAsync("client-456", tokenBucketPolicy);
Console.WriteLine($"Token bucket - Tokens: {tokenBucketInfo.Remaining}/{tokenBucketInfo.Limit}");
```

## JsonUtilityTests

The `JsonUtilityTests` class provides a comprehensive unit testing suite for the `JsonUtility` class, validating JSON serialization, deserialization, parsing, and merging operations. These tests ensure that JSON processing in the API gateway robustly handles various data structures, edge cases, and type conversions.

Example usage demonstrating the test class properties and structure:

```csharp
using DotNetApiGateway.Tests;

// Instantiate the test class
var tests = new JsonUtilityTests();

// Set properties used in testing scenarios
tests.Name = "John";
tests.Age = 30;
tests.OptionalField = "OptionalValue";

// The class contains various [Fact] methods to test JsonUtility, such as:
// tests.Serialize_ValidObject_ReturnsJsonString();
// tests.Deserialize_ValidJson_ReturnsObject();
// tests.MergeJson_BothValidJson_ReturnsMergedJson();
```


```
