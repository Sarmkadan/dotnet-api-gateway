# DotNetApiGateway

An ASP.NET Core API gateway: route matching, load-balanced forwarding, per-route rate limiting, circuit breaking, request/response transformation and aggregation.

## Architecture

How the pieces fit together - middleware order, the fallback forwarding endpoint, design decisions and their trade-offs - is documented in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## WebhookManagementController

The `WebhookManagementController` manages webhook subscriptions and delivery configuration for the API gateway. It handles the creation, retrieval, updating, and deletion of webhook subscriptions, allowing external systems to register callback URLs for specific event types. The controller also provides a test endpoint to validate webhook delivery and verify that callback endpoints are reachable and responding correctly.

Webhook subscriptions include configurable retry policies with exponential backoff, enabling reliable event delivery even when downstream systems experience temporary failures. This controller integrates with the `WebhookRegistry` to register subscriptions for event routing.

Example usage:

```csharp
using DotNetApiGateway.Controllers;
using DotNetApiGateway.Integration;
using Microsoft.AspNetCore.Mvc.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// Create a test server with required services
var hostBuilder = new WebHostBuilder()
    .ConfigureServices(services =>
    {
        services.AddLogging();
        services.AddSingleton<WebhookRegistry>();
        services.AddControllers();
    });

var server = new TestServer(hostBuilder);
var client = server.CreateClient();

// Create a new webhook subscription
var createRequest = new
{
    CallbackUrl = "https://webhook-handler.example.com/api/events",
    EventTypes = new[] { "user.created", "user.updated", "order.placed" },
    MaxRetries = 5,
    InitialDelayMs = 1000,
    MaxDelayMs = 60000
};

var createResponse = await client.PostAsJsonAsync("/api/WebhookManagement/subscriptions", createRequest);
createResponse.EnsureSuccessStatusCode();

var createdSubscription = await createResponse.Content.ReadFromJsonAsync<WebhookSubscription>();
Console.WriteLine($"Created subscription: {createdSubscription.Id}");

// Get all webhook subscriptions
var getAllResponse = await client.GetAsync("/api/WebhookManagement/subscriptions");
getAllResponse.EnsureSuccessStatusCode();

var allSubscriptions = await getAllResponse.Content.ReadFromJsonAsync<List<WebhookSubscription>>();
Console.WriteLine($"Total subscriptions: {allSubscriptions.Count}");

// Get a specific webhook subscription
var getResponse = await client.GetAsync($"/api/WebhookManagement/subscriptions/{createdSubscription.Id}");
getResponse.EnsureSuccessStatusCode();

var subscription = await getResponse.Content.ReadFromJsonAsync<WebhookSubscription>();
Console.WriteLine($"Subscription callback: {subscription.CallbackUrl}");

// Update a webhook subscription
var updateRequest = new
{
    CallbackUrl = "https://webhook-handler.example.com/api/events/v2",
    EventTypes = new[] { "user.*" },
    MaxRetries = 3
};

var updateResponse = await client.PutAsJsonAsync($"/api/WebhookManagement/subscriptions/{createdSubscription.Id}", updateRequest);
updateResponse.EnsureSuccessStatusCode();

// Test webhook delivery
var testResponse = await client.PostAsync($"/api/WebhookManagement/subscriptions/{createdSubscription.Id}/test", null);
testResponse.EnsureSuccessStatusCode();

var testResult = await testResponse.Content.ReadFromJsonAsync<dynamic>();
Console.WriteLine($"Test delivery successful: {testResult.success}");

// Delete a webhook subscription
var deleteResponse = await client.DeleteAsync($"/api/WebhookManagement/subscriptions/{createdSubscription.Id}");
deleteResponse.EnsureSuccessStatusCode();
```

## GatewayManagementController

The `GatewayManagementController` provides operational endpoints for managing and monitoring the API gateway's routes, policies, and overall health. It serves as the central management interface for gateway configuration, allowing administrators to create, retrieve, update, and delete routes, as well as monitor circuit breaker states, rate limits, and gateway metrics.

This controller is essential for gateway operations, providing endpoints for route management (CRUD operations), metrics collection, circuit breaker monitoring and reset, and rate limit inspection and management. It integrates with the `RoutingService`, `CircuitBreakerService`, `RateLimitingService`, `MetricsService`, and `GatewayRouteRepository` to provide comprehensive gateway management capabilities.

Example usage:

```csharp
using DotNetApiGateway.Controllers;
using DotNetApiGateway.Models;
using Microsoft.AspNetCore.Mvc.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// Create a test server with required services
var hostBuilder = new WebHostBuilder()
    .ConfigureServices(services =>
    {
        services.AddLogging();
        services.AddSingleton<RoutingService>();
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<RateLimitingService>();
        services.AddSingleton<MetricsService>();
        services.AddSingleton<GatewayRouteRepository>();
        services.AddControllers();
    });

var server = new TestServer(hostBuilder);
var client = server.CreateClient();

// Create a new gateway route
var newRoute = new GatewayRoute
{
    Id = "user-api-route",
    Name = "User API Route",
    PathPattern = "/api/users/{id}",
    AllowedMethods = new[] { "GET", "PUT", "DELETE" },
    IsActive = true,
    TimeoutSeconds = 30,
    Targets = new List<RouteTarget>
    {
        new RouteTarget
        {
            Name = "user-service-primary",
            BaseUrl = "https://user-service.internal:8080",
            Weight = 70,
            IsHealthy = true
        },
        new RouteTarget
        {
            Name = "user-service-backup",
            BaseUrl = "https://user-service-backup.internal:8080",
            Weight = 30,
            IsHealthy = true
        }
    }
};

var createResponse = await client.PostAsJsonAsync("/api/GatewayManagement/routes", newRoute);
createResponse.EnsureSuccessStatusCode();

var createdRoute = await createResponse.Content.ReadFromJsonAsync<GatewayRoute>();
Console.WriteLine($"Created route: {createdRoute.Id}");

// Get all routes with their metrics
var getAllResponse = await client.GetAsync("/api/GatewayManagement/routes");
getAllResponse.EnsureSuccessStatusCode();

var allRoutesWithMetrics = await getAllResponse.Content.ReadFromJsonAsync<List<object>>();
Console.WriteLine($"Total routes: {allRoutesWithMetrics.Count}");

// Get a specific route with metrics
var getResponse = await client.GetAsync($"/api/GatewayManagement/routes/{createdRoute.Id}");
getResponse.EnsureSuccessStatusCode();

var routeWithMetrics = await getResponse.Content.ReadFromJsonAsync<object>();
Console.WriteLine($"Retrieved route with metrics");

// Update a route
var updatedRoute = new GatewayRoute
{
    Name = "User API Route - Updated",
    PathPattern = "/api/users/{userId}",
    AllowedMethods = new[] { "GET", "PUT", "DELETE", "PATCH" },
    IsActive = true,
    TimeoutSeconds = 45
};

var updateResponse = await client.PutAsJsonAsync($"/api/GatewayManagement/routes/{createdRoute.Id}", updatedRoute);
updateResponse.EnsureSuccessStatusCode();

// Get route-specific metrics
var metricsResponse = await client.GetAsync($"/api/GatewayManagement/metrics/routes/{createdRoute.Id}");
metricsResponse.EnsureSuccessStatusCode();

var routeMetrics = await metricsResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Retrieved metrics for route");

// Get circuit breaker statuses
var circuitBreakersResponse = await client.GetAsync("/api/GatewayManagement/circuit-breakers");
circuitBreakersResponse.EnsureSuccessStatusCode();

var circuitBreakerStatuses = await circuitBreakersResponse.Content.ReadFromJsonAsync<List<object>>();
Console.WriteLine($"Circuit breakers: {circuitBreakerStatuses.Count}");

// Reset a circuit breaker
var resetCircuitResponse = await client.PostAsync("/api/GatewayManagement/circuit-breakers/user-service-primary/reset", null);
resetCircuitResponse.EnsureSuccessStatusCode();

// Get rate limit status for a client
var rateLimitResponse = await client.GetAsync("/api/GatewayManagement/rate-limits/client-123");
rateLimitResponse.EnsureSuccessStatusCode();

var rateLimitInfo = await rateLimitResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Retrieved rate limit info");

// Reset rate limits for a specific key
var resetRateLimitResponse = await client.PostAsync("/api/GatewayManagement/rate-limits/client-123/reset", null);
resetRateLimitResponse.EnsureSuccessStatusCode();

// Reset all rate limits
var resetAllResponse = await client.PostAsync("/api/GatewayManagement/rate-limits/reset-all", null);
resetAllResponse.EnsureSuccessStatusCode();

// Get global gateway metrics
var globalMetricsResponse = await client.GetAsync("/api/GatewayManagement/metrics/global");
globalMetricsResponse.EnsureSuccessStatusCode();

var globalMetrics = await globalMetricsResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Retrieved global metrics");

// Delete a route
var deleteResponse = await client.DeleteAsync($"/api/GatewayManagement/routes/{createdRoute.Id}");
deleteResponse.EnsureSuccessStatusCode();
```

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

## JsonResponseFormatter

The `JsonResponseFormatter` class provides standardized JSON response formatting utilities for consistent API responses across the gateway. It supports success responses with data, error responses with error codes, validation error responses with field-specific details, and paginated responses with metadata. The formatter uses camelCase property naming and handles null values gracefully.

Example usage:

```csharp
using DotNetApiGateway.Formatters;
using System;
using System.Collections.Generic;

// Format a successful response with data
var user = new { Id = 123, Name = "John Doe", Email = "john@example.com" };
string successResponse = JsonResponseFormatter.FormatSuccess(user, "User retrieved successfully");
Console.WriteLine(successResponse);

// Format a success response with only a message (no data)
string messageResponse = JsonResponseFormatter.FormatSuccess("Operation completed successfully");
Console.WriteLine(messageResponse);

// Format an error response with error code and message
string errorResponse = JsonResponseFormatter.FormatError(
    "AUTH_ERROR",
    "Authentication failed: invalid credentials",
    statusCode: 401
);
Console.WriteLine(errorResponse);

// Format a validation error response with field-specific errors
var validationErrors = new Dictionary<string, string>
{
    ["email"] = "Email is required",
    ["password"] = "Password must be at least 8 characters"
};
string validationResponse = JsonResponseFormatter.FormatValidationError(validationErrors);
Console.WriteLine(validationResponse);

// Format a paginated response with data and pagination metadata
var users = new List<object>
{
    new { Id = 1, Name = "User 1" },
    new { Id = 2, Name = "User 2" }
};
string paginatedResponse = JsonResponseFormatter.FormatPaginated(users, 1, 2, 100);
Console.WriteLine(paginatedResponse);

// Format raw JSON bytes response
byte[] jsonBytes = JsonResponseFormatter.FormatBytes(user);
Console.WriteLine(System.Text.Encoding.UTF8.GetString(jsonBytes));
```

## XmlFormatter

The `XmlFormatter` class provides XML serialization and deserialization utilities for the API gateway. It supports converting objects to XML strings or byte arrays, and parsing XML back into objects. The formatter includes XML escaping utilities for safely embedding text in XML documents and handles null values gracefully.

Example usage:

```csharp
using DotNetApiGateway.Formatters;
using System;

// Serialize an object to XML string
var user = new User { Id = 123, Name = "John Doe", Email = "john@example.com" };
string xmlString = XmlFormatter.Serialize(user);
Console.WriteLine(xmlString);

// Serialize an object to XML bytes
byte[] xmlBytes = XmlFormatter.SerializeToBytes(user);
Console.WriteLine($"Serialized to {xmlBytes.Length} bytes");

// Deserialize XML string back to object
string xmlData = "<User><Id>456</Id><Name>Jane Smith</Name><Email>jane@example.com</Email></User>";
User? deserializedUser = XmlFormatter.Deserialize<User>(xmlData);
if (deserializedUser != null)
{
    Console.WriteLine($"Deserialized: {deserializedUser.Name} ({deserializedUser.Email})");
}

// Escape XML special characters
string unsafeText = "<script>alert('XSS')</script>& more text";
string escaped = XmlFormatter.EscapeXml(unsafeText);
Console.WriteLine(escaped); // Output: &lt;script&gt;alert(&apos;XSS&apos;)&lt;/script&gt;&amp; more text

// Unescape XML entities
string escapedText = "&lt;user&gt;John&amp;Jane&lt;/user&gt;";
string unescaped = XmlFormatter.UnescapeXml(escapedText);
Console.WriteLine(unescaped); // Output: <user>John&Jane</user>
```

## ConfigurationValidator

The `ConfigurationValidator` class validates configuration settings for the API gateway.

Example usage:

```csharp
using DotNetApiGateway.Configuration;
using DotNetApiGateway.Models;

// Create a configuration validator
var validator = new ConfigurationValidator();

// Validate gateway configuration
var gatewayResult = validator.ValidateGatewayConfig(new GatewayRoute
{
    Name = "user-api",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET", "PUT", "DELETE"]
});

// Validate a route configuration
var routeResult = validator.ValidateRoute(new GatewayRoute
{
    Name = "user-api",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET"]
});

// Validate a route target
var targetResult = validator.ValidateRouteTarget(new RouteTarget
{
    Name = "user-service-primary",
    BaseUrl = "https://user-service.internal:8080"
});

// Validate rate limit policy
var rateLimitResult = validator.ValidateRateLimitPolicy(new RateLimitPolicy
{
    Enabled = true,
    RequestsPerMinute = 1000
});

// Validate circuit breaker policy
var circuitBreakerResult = validator.ValidateCircuitBreakerPolicy(new CircuitBreakerPolicy
{
    Enabled = true,
    FailureThreshold = 5,
    SuccessThreshold = 3
});

// Validate cache policy
var cacheResult = validator.ValidateCachePolicy(new CachePolicy
{
    Enabled = true,
    DurationSeconds = 300
});

// Access validation errors
if (!gatewayResult.IsValid)
{
    foreach (var error in validator.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}

// Get error summary
string errorSummary = validator.GetErrorSummary();
if (!string.IsNullOrEmpty(errorSummary))
{
    Console.WriteLine(errorSummary);
}
```

## CircuitBreakerRepository

The `CircuitBreakerRepository` class provides data access and persistence operations for circuit breaker statuses in the API gateway. It implements a thread-safe repository pattern using `ReaderWriterLockSlim` for concurrent access, storing circuit breaker states in memory. The repository supports CRUD operations for managing circuit breaker statuses, querying by service name or state, and bulk operations like resetting all circuits or clearing the repository.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using System;
using System.Threading.Tasks;

// Create the circuit breaker repository
var repository = new CircuitBreakerRepository();

// Add a new circuit breaker status for a service
var newStatus = new CircuitBreakerStatus
{
    Id = "user-service-circuit-breaker",
    ServiceName = "user-service",
    State = CircuitBreakerState.Closed,
    FailureCount = 0,
    SuccessCount = 0,
    LastStateChangeAt = DateTime.UtcNow
};
await repository.AddAsync(newStatus);
Console.WriteLine($"Added circuit breaker for service: {newStatus.ServiceName}");

// Get circuit breaker status by ID
var retrievedStatus = await repository.GetByIdAsync("user-service-circuit-breaker");
if (retrievedStatus != null)
{
    Console.WriteLine($"Retrieved status - Service: {retrievedStatus.ServiceName}, State: {retrievedStatus.State}");
}

// Get circuit breaker status by service name
var statusByService = await repository.GetByServiceNameAsync("user-service");
if (statusByService != null)
{
    Console.WriteLine($"Found status for service: {statusByService.ServiceName}");
}

// Get all circuit breaker statuses
var allStatuses = await repository.GetAllAsync();
Console.WriteLine($"Total circuit breakers: {allStatuses.Count()}");

// Get all open circuits
var openCircuits = await repository.GetOpenCircuitsAsync();
Console.WriteLine($"Open circuits: {openCircuits.Count()}");

// Update a circuit breaker status
retrievedStatus!.State = CircuitBreakerState.HalfOpen;
retrievedStatus.FailureCount = 2;
var updatedStatus = await repository.UpdateAsync(retrievedStatus);
Console.WriteLine($"Updated status - New state: {updatedStatus.State}");

// Check if circuit breaker exists
bool exists = await repository.ExistsAsync("user-service-circuit-breaker");
Console.WriteLine($"Circuit breaker exists: {exists}");

// Reset all circuits to Closed state
await repository.ResetAllAsync();
Console.WriteLine("All circuits reset to Closed state");

// Delete a circuit breaker
bool deleted = await repository.DeleteAsync("user-service-circuit-breaker");
Console.WriteLine($"Circuit breaker deleted: {deleted}");

// Clear all circuit breakers
repository.ClearAll();
Console.WriteLine("All circuit breakers cleared");
```

## GatewayRouteRepository

The `GatewayRouteRepository` class provides data access and persistence operations for gateway route configurations in the API gateway. It implements a thread-safe repository pattern using `ReaderWriterLockSlim` for concurrent access, storing route configurations in memory. The repository supports CRUD operations for managing gateway routes, querying routes by ID, name, path, or status, and bulk operations like clearing all routes.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

// Create the gateway route repository
var repository = new GatewayRouteRepository();

// Create a new gateway route
var newRoute = new GatewayRoute
{
  Id = "user-api-route",
  Name = "User API Route",
  PathPattern = "/api/users/{id}",
  AllowedMethods = ["GET", "PUT", "DELETE"],
  IsActive = true,
  TimeoutSeconds = 30
};
await repository.AddAsync(newRoute);
Console.WriteLine($"Added route: {newRoute.Name} ({newRoute.Id})");

// Get a route by ID
var retrievedRoute = await repository.GetByIdAsync("user-api-route");
if (retrievedRoute != null)
{
  Console.WriteLine($"Retrieved route - Name: {retrievedRoute.Name}, Path: {retrievedRoute.PathPattern}");
}

// Get all routes
var allRoutes = await repository.GetAllAsync();
Console.WriteLine($"Total routes: {allRoutes.Count()}");

// Get all active routes
var activeRoutes = await repository.GetActiveRoutesAsync();
Console.WriteLine($"Active routes: {activeRoutes.Count()}");

// Find a route by path and method
var foundRoute = await repository.FindRouteByPathAsync("/api/users/123", "GET");
if (foundRoute != null)
{
  Console.WriteLine($"Found route matching path /api/users/123 with GET method");
}

// Get routes by name (partial match)
var userRoutes = await repository.GetRoutesByNameAsync("user");
Console.WriteLine($"Routes containing 'user' in name: {userRoutes.Count()}");

// Update a route
retrievedRoute!.Name = "User API Route - Updated";
retrievedRoute.PathPattern = "/api/users/{userId}";
var updatedRoute = await repository.UpdateAsync(retrievedRoute);
Console.WriteLine($"Updated route: {updatedRoute.Name}");

// Check if route exists
bool exists = await repository.ExistsAsync("user-api-route");
Console.WriteLine($"Route exists: {exists}");

// Get route count
int routeCount = await repository.GetCountAsync();
Console.WriteLine($"Total routes in repository: {routeCount}");

// Delete a route
bool deleted = await repository.DeleteAsync("user-api-route");
Console.WriteLine($"Route deleted: {deleted}");

// Clear all routes
repository.ClearAll();
Console.WriteLine("All routes cleared");
```

## UrlUtility

The `UrlUtility` class provides utility methods for URL manipulation and parsing operations in the API gateway. It offers functionality for combining URLs, parsing and building query strings, extracting URL components, validating URLs, and sanitizing URLs by removing sensitive parameters. These utilities ensure safe and consistent URL handling throughout the gateway's request processing pipeline.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// Combine base URL with path, handling trailing slashes correctly
string baseUrl = "https://api.example.com";
string path = "/users/123";
string combinedUrl = UrlUtility.CombineUrl(baseUrl, path);
Console.WriteLine(combinedUrl); // Output: https://api.example.com/users/123

// Parse query string into dictionary
string queryString = "?name=John&age=30&city=New+York";
var parameters = UrlUtility.ParseQueryString(queryString);
Console.WriteLine($"Name: {parameters["name"]}, Age: {parameters["age"]}, City: {parameters["city"]}");

// Build query string from dictionary
var queryParams = new Dictionary<string, string>
{
    ["userId"] = "123",
    ["fields"] = "name,email,address",
    ["sort"] = "name"
};
string builtQuery = UrlUtility.BuildQueryString(queryParams);
Console.WriteLine(builtQuery); // Output: ?userId=123&fields=name%2Cemail%2Caddress&sort=name

// Extract hostname from URL
string hostname = UrlUtility.GetHostname("https://api.example.com:8080/users");
Console.WriteLine(hostname); // Output: api.example.com

// Get port number from URL
int port = UrlUtility.GetPort("https://api.example.com:8443/users");
Console.WriteLine(port); // Output: 8443

// Check if URL is valid
bool isValid = UrlUtility.IsValidUrl("https://api.example.com/users");
Console.WriteLine(isValid); // Output: True

// Sanitize URL by removing sensitive parameters
string sensitiveUrl = "https://api.example.com/login?username=admin&password=secret123&token=xyz";
string sanitized = UrlUtility.SanitizeUrl(sensitiveUrl);
Console.WriteLine(sanitized); // Output: https://api.example.com/login?username=admin&password=***&token=***

// Extract path from URL
string pathOnly = UrlUtility.GetPath("https://api.example.com/v1/users/123?include=profile");
Console.WriteLine(pathOnly); // Output: /v1/users/123

// Check if URL has a specific query parameter
bool hasParam = UrlUtility.HasQueryParameter("https://api.example.com/search?q=dotnet&page=1", "q");
Console.WriteLine(hasParam); // Output: True
```

## UrlUtilityTestsValidation

The `UrlUtilityTestsValidation` class provides validation helpers for URL utility test data to ensure test values meet expected constraints. It contains comprehensive validation methods that verify test data against the actual behavior of `UrlUtility` methods, helping maintain data integrity for URL-related tests.

This utility class validates test inputs for URL combination, URL validation, hostname extraction, port extraction, query parameter presence, URL sanitization, query string parsing, and query string building operations. It returns detailed validation problems or boolean results indicating whether test data is valid.

Example usage:

```csharp
using DotNetApiGateway.Tests;
using DotNetApiGateway.Utilities;

// Validate URL combination test data
bool isValidCombine = UrlUtilityTestsValidation.IsValidCombineUrl(
    baseUrl: "https://api.example.com",
    path: "/users/123",
    queryString: "?name=John"
);
Console.WriteLine($"Combine URL test data is valid: {isValidCombine}");

// Validate URL validation test data
bool isValidUrl = UrlUtilityTestsValidation.IsValidIsValidUrl(
    url: "https://api.example.com/users",
    expectedIsValid: true
);
Console.WriteLine($"URL validation test data is valid: {isValidUrl}");

// Validate hostname extraction test data
bool isValidHostname = UrlUtilityTestsValidation.IsValidGetHostname(
    url: "https://api.example.com:8080/users",
    expectedHostname: "api.example.com"
);
Console.WriteLine($"Hostname extraction test data is valid: {isValidHostname}");

// Validate port extraction test data
bool isValidPort = UrlUtilityTestsValidation.IsValidGetPort(
    url: "https://api.example.com:8443/users",
    expectedPort: 8443
);
Console.WriteLine($"Port extraction test data is valid: {isValidPort}");

// Validate query parameter test data
bool isValidParam = UrlUtilityTestsValidation.IsValidHasQueryParameter(
    url: "https://api.example.com/search?q=dotnet&page=1",
    paramName: "q",
    expectedHasParam: true
);
Console.WriteLine($"Query parameter test data is valid: {isValidParam}");

// Validate URL sanitization test data
bool isValidSanitize = UrlUtilityTestsValidation.IsValidSanitizeUrl(
    url: "https://api.example.com/login?username=admin&password=secret",
    expectedSanitized: "https://api.example.com/login?username=admin&password=***"
);
Console.WriteLine($"URL sanitization test data is valid: {isValidSanitize}");

// Validate query string parsing test data
bool isValidParse = UrlUtilityTestsValidation.IsValidParseQueryString(
    queryString: "?name=John&age=30&city=New+York",
    expectedParameters: new Dictionary<string, string>
    {
        ["name"] = "John",
        ["age"] = "30",
        ["city"] = "New York"
    }
);
Console.WriteLine($"Query string parsing test data is valid: {isValidParse}");

// Validate query string building test data
bool isValidBuild = UrlUtilityTestsValidation.IsValidBuildQueryString(
    parameters: new Dictionary<string, string>
    {
        ["userId"] = "123",
        ["fields"] = "name,email"
    },
    expectedQueryString: "?userId=123&fields=name%2Cemail"
);
Console.WriteLine($"Query string building test data is valid: {isValidBuild}");

// Get detailed validation problems for URL combination
var combineProblems = UrlUtilityTestsValidation.ValidateCombineUrl(
    baseUrl: "https://api.example.com",
    path: "/users/123",
    queryString: "?name=John"
);
if (combineProblems.Count > 0)
{
    Console.WriteLine("Combine URL validation problems:");
    foreach (var problem in combineProblems)
    {
        Console.WriteLine($"  - {problem}");
    }
}

// Ensure URL combination test data is valid (throws if invalid)
UrlUtilityTestsValidation.EnsureValidCombineUrl(
    baseUrl: "https://api.example.com",
    path: "/users/123",
    queryString: "?name=John"
);
Console.WriteLine("URL combination test data is valid and passed validation");
```

## CircuitBreakerStatus

The `CircuitBreakerStatus` class tracks the runtime state and metrics of circuit breaker instances in the API gateway. It maintains counters for successes and failures, tracks state transitions, records timestamps of state changes, and provides methods to update the circuit breaker status. The status object is used by the circuit breaker service to make decisions about whether to allow requests through based on the current circuit state.

Example usage:

```csharp
using DotNetApiGateway.Models;
using System;

// Create a circuit breaker status for a service
var status = new CircuitBreakerStatus
{
    Id = "user-service-circuit-breaker",
    ServiceName = "user-service",
    State = CircuitBreakerState.Closed,
    FailureCount = 0,
    SuccessCount = 0,
    LastStateChangeAt = DateTime.UtcNow,
    LastFailureAt = null,
    LastSuccessAt = null,
    TotalFailures = 0,
    TotalSuccesses = 0,
    TotalRequests = 0,
    LastError = null
};

// Record a successful operation
status.RecordSuccess();
Console.WriteLine($"Success count: {status.SuccessCount}, Total successes: {status.TotalSuccesses}");

// Record a failure with error details
status.RecordFailure("Connection timeout to user-service");
Console.WriteLine($"Failure count: {status.FailureCount}, Last error: {status.LastError}");

// Calculate success and failure rates
double successRate = status.GetSuccessRate();
double failureRate = status.GetFailureRate();
Console.WriteLine($"Success rate: {successRate:P}, Failure rate: {failureRate:P}");

// Change circuit state (e.g., trip the circuit after too many failures)
status.ChangeState(CircuitBreakerState.Open);
Console.WriteLine($"Circuit state changed to: {status.State}");

// Reset circuit breaker to initial state
status.Reset();
Console.WriteLine($"Circuit reset - State: {status.State}, Failure count: {status.FailureCount}");

// Access status properties
Console.WriteLine($"Service: {status.ServiceName}");
Console.WriteLine($"Current state: {status.State}");
Console.WriteLine($"Last state change: {status.LastStateChangeAt:O}");
if (status.LastFailureAt.HasValue)
{
    Console.WriteLine($"Last failure at: {status.LastFailureAt.Value:O}");
}
```

## CircuitBreakerService

The `CircuitBreakerService` class manages circuit breaker state and prevents cascading failures in the API gateway. It provides thread-safe operations for checking circuit state, recording successes and failures, and managing circuit breaker status across all downstream services. The service uses per-service locking to prevent race conditions during concurrent state transitions and automatically handles state transitions between Closed, Open, and HalfOpen states based on configurable policies.



Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<CircuitBreakerRepository>();
services.AddSingleton<CircuitBreakerService>();

var serviceProvider = services.BuildServiceProvider();
var circuitBreakerService = serviceProvider.GetRequiredService<CircuitBreakerService>();

// Create a circuit breaker policy with failure thresholds
var policy = new CircuitBreakerPolicy
{
  Enabled = true,
  FailureThreshold = 3, // Trip circuit after 3 failures
  SuccessThreshold = 2, // Reset circuit after 2 consecutive successes
  TimeoutSeconds = 30, // Consider request timed out after 30 seconds
  MaxRetries = 2,
  RetryDelayMilliseconds = 100
};

// Check if a request can be attempted (throws CircuitBreakerException if circuit is open)
bool canAttempt = await circuitBreakerService.CanAttemptAsync("user-service", policy);
Console.WriteLine($"Can attempt request: {canAttempt}");

// Record a successful request
await circuitBreakerService.RecordSuccessAsync("user-service", policy);
Console.WriteLine("Request succeeded - circuit health improved");

// Record a failed request
await circuitBreakerService.RecordFailureAsync(
  "user-service",
  "Connection timeout",
  policy
);
Console.WriteLine("Request failed - circuit health degraded");

// Check if circuit is currently open
bool isOpen = await circuitBreakerService.IsCircuitOpenAsync("user-service");
Console.WriteLine($"Circuit is open: {isOpen}");

// Get circuit breaker status for a specific service
var status = await circuitBreakerService.GetStatusAsync("user-service");
if (status != null)
{
  Console.WriteLine($"Service: {status.ServiceName}");
  Console.WriteLine($"State: {status.State}");
  Console.WriteLine($"Failure count: {status.FailureCount}");
  Console.WriteLine($"Success count: {status.SuccessCount}");
}

// Get all open circuits across the gateway
var openCircuits = await circuitBreakerService.GetOpenCircuitsAsync();
Console.WriteLine($"Open circuits count: {openCircuits.Count()}");

// Reset a specific circuit breaker
await circuitBreakerService.ResetCircuitAsync("user-service");
Console.WriteLine("Circuit manually reset to Closed state");

// Reset all circuit breakers
await circuitBreakerService.ResetAllCircuitsAsync();
Console.WriteLine("All circuits reset to Closed state");

// Get all circuit breaker statuses
var allStatuses = await circuitBreakerService.GetAllStatusesAsync();
foreach (var s in allStatuses)
{
  Console.WriteLine($"{s.ServiceName}: {s.State}");
}
```

## RequestContext

The `RequestContext` class contains request-scoped metadata used throughout the API gateway's request processing pipeline. It holds information about the incoming request including identifiers, client identity, authentication tokens, headers, query parameters, custom data, matched routes, and timing information. This context object is passed through middleware and services to provide consistent access to request state.

Example usage:

```csharp
using DotNetApiGateway.Models;
using System;
using System.Collections.Generic;

// Create a request context for an incoming HTTP request
var context = new RequestContext
{
    RequestId = Guid.NewGuid().ToString(),
    Path = "/api/users/123",
    Method = "GET",
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ["X-Client-Version"] = "2.1",
        ["Accept"] = "application/json"
    },
    QueryParameters = new Dictionary<string, string>
    {
        ["fields"] = "name,email,address",
        ["include"] = "orders"
    },
    Body = "{\"userId\": 123}",
    ClientIp = "192.168.1.100",
    AuthToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ReceivedAt = DateTime.UtcNow.AddSeconds(-0.150),
    CustomData = new Dictionary<string, object>
    {
        ["userAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
        ["requestId"] = "req_abc123"
    },
    MatchedRoute = new GatewayRoute
    {
        Name = "user-api-route",
        PathPattern = "/api/users/{id}",
        AllowedMethods = ["GET", "PUT", "DELETE"]
    },
    SelectedTarget = new RouteTarget
    {
        Name = "user-service-primary",
        BaseUrl = "https://user-service.internal:8080"
    },
    ClientIdentity = new ClientIdentity
    {
        Id = "user-456",
        Name = "John Doe",
        Roles = ["user", "premium"]
    }
};

// Access request context properties
Console.WriteLine($"Request ID: {context.RequestId}");
Console.WriteLine($"Path: {context.Path}");
Console.WriteLine($"Method: {context.Method}");
Console.WriteLine($"Client IP: {context.ClientIp}");
Console.WriteLine($"Has auth token: {context.HasAuthToken()}");

// Extract bearer token from authorization header
string bearerToken = context.ExtractBearerToken();
Console.WriteLine($"Bearer token: {bearerToken}");

// Get client identifier (uses ClientIdentity.Id if available, otherwise ClientIp)
string clientId = context.GetClientIdentifier();
Console.WriteLine($"Client identifier: {clientId}");

// Calculate elapsed time since request was received
TimeSpan elapsed = context.ElapsedTime();
Console.WriteLine($"Processing time: {elapsed.TotalMilliseconds}ms");

// Access custom data for application-specific metadata
if (context.CustomData.TryGetValue("userAgent", out object? userAgent))
{
    Console.WriteLine($"User agent: {userAgent}");
}
```

## RequestInterceptor

The `RequestInterceptor` class provides request interception and transformation capabilities for the API gateway. It allows for modifying HTTP requests before they are forwarded to upstream services by adding/removing headers, transforming request bodies using templates, and managing query parameter mappings. Request interceptors are registered per route and can be enabled/disabled dynamically.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<RequestInterceptor>();

var serviceProvider = services.BuildServiceProvider();
var interceptor = serviceProvider.GetRequiredService<RequestInterceptor>();

// Create a request transformer configuration
var transformer = new RequestTransformer
{
    Enabled = true,
    HeadersToAdd = new Dictionary<string, string>
    {
        ["X-Request-Id"] = Guid.NewGuid().ToString(),
        ["X-Correlation-Id"] = "corr-123",
        ["X-Tenant-Id"] = "acme-corp"
    },
    HeadersToRemove = new List<string> { "X-Old-Header", "User-Agent" },
    BodyTemplate = "{ \"timestamp\": \"{timestamp}\", \"requestId\": \"{requestId}\", \"originalBody\": {body} }",
    QueryParamMappings = new Dictionary<string, string>
    {
        ["userId"] = "id",
        ["fields"] = "projection"
    }
};

// Register the transformer for a specific route
interceptor.RegisterTransformer("user-api-route", transformer);

// Intercept a request
var request = new HttpRequestMessage(HttpMethod.Post, "https://backend/api/users");
request.Content = new StringContent("{\"name\": \"John Doe\", \"email\": \"john@example.com\"}", Encoding.UTF8, "application/json");

var context = new RequestContext
{
    RequestId = Guid.NewGuid().ToString(),
    Path = "/api/users",
    Method = "POST"
};

var interceptedRequest = await interceptor.InterceptAsync("user-api-route", request, context);

// Verify transformations were applied
interceptedRequest.Headers.Contains("X-Request-Id").Should().BeTrue();
interceptedRequest.Headers.Contains("X-Old-Header").Should().BeFalse();
var bodyContent = await interceptedRequest.Content.ReadAsStringAsync();
bodyContent.Should().Contain("timestamp");
bodyContent.Should().Contain("requestId");
```

## RateLimitEntry

The `RateLimitEntry` class represents the current state of a rate limit for a specific key. It tracks request counts, remaining time until window reset, available tokens (for token bucket strategies), and the timestamp of the last request. This type is used by the rate limiting service to provide real-time rate limit information to clients.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a rate limit entry for a client
var rateLimitEntry = new RateLimitEntry
{
  Key = "client-123", // Client identifier (IP, user ID, etc.)
  Count = 42, // Current request count within the window
  RemainingTimeSeconds = 35, // Seconds until rate limit resets
  Tokens = 58.5, // Available tokens in token bucket
  LastRequest = DateTime.UtcNow.AddSeconds(-10) // When last request occurred
};

// Access rate limit information
Console.WriteLine($"Key: {rateLimitEntry.Key}");
Console.WriteLine($"Requests: {rateLimitEntry.Count}/100");
Console.WriteLine($"Remaining time: {rateLimitEntry.RemainingTimeSeconds}s");
Console.WriteLine($"Tokens: {rateLimitEntry.Tokens}");
Console.WriteLine($"Last request: {rateLimitEntry.LastRequest:O}");

// Update the entry with current state
rateLimitEntry.Count = 43;
rateLimitEntry.Tokens = 57.2;
rateLimitEntry.LastRequest = DateTime.UtcNow;
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

## RequestAggregationService

The `RequestAggregationService` class aggregates responses from multiple backend requests based on an aggregation policy. It supports different aggregation strategies (sequential, parallel, or first-success) and conditional fan-out using JSONPath expressions to determine which backend targets should receive requests. This service is useful for implementing canary deployments, blue-green deployments, A/B testing, or consolidating responses from multiple backend services.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddHttpClient();
services.AddSingleton<RequestAggregationService>();

var serviceProvider = services.BuildServiceProvider();
var aggregationService = serviceProvider.GetRequiredService<RequestAggregationService>();

// Create an aggregation policy for parallel request distribution
var parallelPolicy = new AggregationPolicy
{
    Id = "parallel-distribution-policy",
    Enabled = true,
    Strategy = AggregationStrategy.Parallel,
    Targets = [
        new ConditionalAggregationTarget
        {
            Id = "primary-backend",
            UpstreamUrl = "https://api.example.com/v1/users",
            Method = HttpMethod.Get,
            Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
            TimeoutSeconds = 30,
            Optional = false
        },
        new ConditionalAggregationTarget
        {
            Id = "secondary-backend",
            UpstreamUrl = "https://backup.api.example.com/v1/users",
            Method = HttpMethod.Get,
            Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
            TimeoutSeconds = 45,
            Optional = true // Failure won't fail the entire aggregation
        }
    ]
};

// Create an aggregation policy for sequential fallback processing
var sequentialPolicy = new AggregationPolicy
{
    Id = "sequential-fallback-policy",
    Enabled = true,
    Strategy = AggregationStrategy.Sequential,
    Targets = [
        new ConditionalAggregationTarget
        {
            Id = "primary-service",
            UpstreamUrl = "https://primary.api.example.com/users",
            Method = HttpMethod.Get,
            TimeoutSeconds = 30
        },
        new ConditionalAggregationTarget
        {
            Id = "secondary-service",
            UpstreamUrl = "https://secondary.api.example.com/users",
            Method = HttpMethod.Get,
            TimeoutSeconds = 45
        },
        new ConditionalAggregationTarget
        {
            Id = "tertiary-service",
            UpstreamUrl = "https://backup.api.example.com/users",
            Method = HttpMethod.Get,
            TimeoutSeconds = 60
        }
    ]
};

// Create an aggregation policy with conditional fan-out using JSONPath
var conditionalPolicy = new AggregationPolicy
{
    Id = "conditional-fanout-policy",
    Enabled = true,
    Strategy = AggregationStrategy.Parallel,
    Targets = [
        new ConditionalAggregationTarget
        {
            Id = "production-backend",
            UpstreamUrl = "https://api.example.com/production/users",
            Method = HttpMethod.Post,
            Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
            Body = "{\"source\": \"production\"}",
            TimeoutSeconds = 45,
            Optional = false
        },
        new ConditionalAggregationTarget
        {
            Id = "canary-backend",
            UpstreamUrl = "https://canary.api.example.com/users",
            Method = HttpMethod.Post,
            JsonPathCondition = "$.headers['X-Canary-User']", // Only selected users go to canary
            Headers = new Dictionary<string, string> { ["X-Environment"] = "canary" },
            TimeoutSeconds = 30,
            Optional = true
        }
    ]
};

// Execute aggregation with a sample request body
var requestBody = "{\"userId\": 123, \"headers\": {\"X-Canary-User\": \"true\"}}";
var aggregatedResponse = await aggregationService.AggregateAsync(conditionalPolicy, requestBody);

// Access aggregated results
Console.WriteLine($"Total responses: {aggregatedResponse.Responses.Count}");
Console.WriteLine($"Success count: {aggregatedResponse.SuccessCount}");
Console.WriteLine($"Failure count: {aggregatedResponse.FailureCount}");
Console.WriteLine($"Total duration: {aggregatedResponse.TotalDuration.TotalMilliseconds}ms");

// Retrieve individual responses
foreach (var response in aggregatedResponse.Responses)
{
    Console.WriteLine($"Target: {response.Alias}");
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"Duration: {response.Duration.TotalMilliseconds}ms");
    if (response.Body != null)
    {
        Console.WriteLine($"Body: {response.Body}");
    }
}
```

## ExternalApiClientValidation

The `ExternalApiClientValidation` class provides validation utilities for `ExternalApiClient` instances and their HTTP request parameters. It offers methods to validate client instances, endpoints, HTTP methods, request data, content, headers, and cancellation tokens, returning detailed error lists or boolean results. The validation methods follow a consistent pattern with `Validate()`, `IsValid()`, and `EnsureValid()` variants for each validation target.

Example usage:

```csharp
using DotNetApiGateway.Integration;
using System.Net.Http;
using System.Threading;

// Create an ExternalApiClient instance
var apiClient = new ExternalApiClient
{
    BaseUrl = "https://api.example.com",
    Timeout = TimeSpan.FromSeconds(30),
    RetryPolicy = new RetryPolicy { MaxAttempts = 3 }
};

// Validate the client instance
var clientProblems = ExternalApiClientValidation.Validate(apiClient);
if (clientProblems.Count > 0)
{
    Console.WriteLine("Client validation errors:");
    foreach (var problem in clientProblems)
    {
        Console.WriteLine($" - {problem}");
    }
}

// Validate an endpoint URL
var endpoint = "https://api.example.com/users";
var endpointProblems = ExternalApiClientValidation.ValidateEndpoint(endpoint);
if (!ExternalApiClientValidation.IsValidEndpoint(endpoint))
{
    Console.WriteLine("Endpoint is not valid");
}
ExternalApiClientValidation.EnsureValidEndpoint(endpoint);

// Validate HTTP method
var method = HttpMethod.Get;
var methodProblems = ExternalApiClientValidation.ValidateHttpMethod(method);
if (ExternalApiClientValidation.IsValidHttpMethod(method))
{
    Console.WriteLine("HTTP method is valid");
}
ExternalApiClientValidation.EnsureValidHttpMethod(method);

// Validate request data (generic type)
var requestData = new { UserId = 123, Name = "John Doe" };
var dataProblems = ExternalApiClientValidation.ValidateRequestData(requestData);
if (ExternalApiClientValidation.IsValidRequestData(requestData))
{
    Console.WriteLine("Request data is valid");
}
ExternalApiClientValidation.EnsureValidRequestData(requestData);

// Validate request content
var content = "{\"name\": \"John Doe\"}";
var contentProblems = ExternalApiClientValidation.ValidateRequestContent(content, "application/json");
if (ExternalApiClientValidation.IsValidRequestContent(content))
{
    Console.WriteLine("Request content is valid");
}
ExternalApiClientValidation.EnsureValidRequestContent(content, "application/json");

// Validate request headers
dynamic headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer token123",
    ["Content-Type"] = "application/json"
};
var headerProblems = ExternalApiClientValidation.ValidateRequestHeaders(headers);
if (ExternalApiClientValidation.IsValidRequestHeaders(headers))
{
    Console.WriteLine("Headers are valid");
}
ExternalApiClientValidation.EnsureValidRequestHeaders(headers);

// Validate cancellation token
var cts = new CancellationTokenSource();
var tokenProblems = ExternalApiClientValidation.ValidateCancellationToken(cts.Token);
if (ExternalApiClientValidation.IsValidCancellationToken(cts.Token))
{
    Console.WriteLine("Cancellation token is valid");
}
ExternalApiClientValidation.EnsureValidCancellationToken(cts.Token);
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

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddHttpClient();
services.AddSingleton<RequestAggregationService>();

var serviceProvider = services.BuildServiceProvider();
var aggregationService = serviceProvider.GetRequiredService<RequestAggregationService>();

// Create an aggregation policy for parallel request distribution
var parallelPolicy = new AggregationPolicy
{
Id = "parallel-distribution-policy",
Enabled = true,
Strategy = AggregationStrategy.Parallel,
Targets = [
new ConditionalAggregationTarget
{
Id = "primary-backend",
UpstreamUrl = "https://api.example.com/v1/users",
Method = HttpMethod.Get,
Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
TimeoutSeconds = 30,
Optional = false
},
new ConditionalAggregationTarget
{
Id = "secondary-backend",
UpstreamUrl = "https://backup.api.example.com/v1/users",
Method = HttpMethod.Get,
Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
TimeoutSeconds = 45,
Optional = true // Failure won't fail the entire aggregation
}
]
};

// Create an aggregation policy for sequential fallback processing
var sequentialPolicy = new AggregationPolicy
{
Id = "sequential-fallback-policy",
Enabled = true,
Strategy = AggregationStrategy.Sequential,
Targets = [
new ConditionalAggregationTarget
{
Id = "primary-service",
UpstreamUrl = "https://primary.api.example.com/users",
Method = HttpMethod.Get,
TimeoutSeconds = 30
},
new ConditionalAggregationTarget
{
Id = "secondary-service",
UpstreamUrl = "https://secondary.api.example.com/users",
Method = HttpMethod.Get,
TimeoutSeconds = 45
},
new ConditionalAggregationTarget
{
Id = "tertiary-service",
UpstreamUrl = "https://backup.api.example.com/users",
Method = HttpMethod.Get,
TimeoutSeconds = 60
}
]
};

// Create an aggregation policy with conditional fan-out using JSONPath
var conditionalPolicy = new AggregationPolicy
{
Id = "conditional-fanout-policy",
Enabled = true,
Strategy = AggregationStrategy.Parallel,
Targets = [
new ConditionalAggregationTarget
{
Id = "production-backend",
UpstreamUrl = "https://api.example.com/production/users",
Method = HttpMethod.Post,
Headers = new Dictionary<string, string> { ["X-Environment"] = "production" },
Body = "{\"source\": \"production\"}",
TimeoutSeconds = 45,
Optional = false
},
new ConditionalAggregationTarget
{
Id = "canary-backend",
UpstreamUrl = "https://canary.api.example.com/users",
Method = HttpMethod.Post,
JsonPathCondition = "$.headers['X-Canary-User']", // Only selected users go to canary
Headers = new Dictionary<string, string> { ["X-Environment"] = "canary" },
TimeoutSeconds = 30,
Optional = true
}
]
};

// Execute aggregation with a sample request body
var requestBody = "{\"userId\": 123, \"headers\": {\"X-Canary-User\": \"true\"}}";
var aggregatedResponse = await aggregationService.AggregateAsync(conditionalPolicy, requestBody);

// Access aggregated results
Console.WriteLine($"Total responses: {aggregatedResponse.Responses.Count}");
Console.WriteLine($"Success count: {aggregatedResponse.SuccessCount}");
Console.WriteLine($"Failure count: {aggregatedResponse.FailureCount}");
Console.WriteLine($"Total duration: {aggregatedResponse.TotalDuration.TotalMilliseconds}ms");

// Retrieve individual responses
foreach (var response in aggregatedResponse.Responses)
{
Console.WriteLine($"Target: {response.Alias}");
Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine($"Duration: {response.Duration.TotalMilliseconds}ms");
if (response.Body != null)
{
Console.WriteLine($"Body: {response.Body}");
}
}
```

## RequestCoalescingService

The `RequestCoalescingService` class coalesces duplicate concurrent requests so that only one upstream call is made per unique request key, with all concurrent callers receiving the same response. It's designed for singleton registration and is thread-safe, making it ideal for reducing load on upstream services when handling duplicate requests that arrive simultaneously.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<RequestCoalescingService>();

var serviceProvider = services.BuildServiceProvider();
var coalescingService = serviceProvider.GetRequiredService<RequestCoalescingService>();

// Create a request coalescing policy
var policy = new RequestCoalescingPolicy
{
Id = "user-profile-coalescing",
Enabled = true,
TimeoutMs = 5000, // Wait up to 5 seconds for a coalesced response
MaxQueuedRequests = 100, // Allow up to 100 followers to queue
CoalescibleMethods = ["GET", "HEAD"], // Only coalesce GET and HEAD requests
IncludeQueryString = true // Include query parameters in coalescing key
};

// Validate the policy configuration
policy.Validate();

// Simulate concurrent requests arriving for the same user profile
var userId = "123";
var tasks = new List<Task<byte[]?>>();

// Create 5 concurrent requests for the same user profile
for (int i = 0; i < 5; i++)
{
var requestKey = $"/api/users/{userId}";

tasks.Add(Task.Run(async () =>
{
// Each request will either get the coalesced response or execute independently
var response = await coalescingService.GetOrCoalesceAsync(
requestKey,
async (ct) =>
{
// This function only executes once for all concurrent requests with the same key
Console.WriteLine($"Executing upstream call for {requestKey}");
await Task.Delay(100, ct); // Simulate upstream processing
return new byte[] { 0x55, 0x73, 0x65, 0x72, 0x20, 0x50, 0x72, 0x6F, 0x66, 0x69, 0x6C, 0x65 };
},
policy
));

return response;
}));
}

// Wait for all requests to complete
var results = await Task.WhenAll(tasks);

// All results should be identical since they coalesced to the same upstream call
Console.WriteLine($"All {results.Length} requests completed with identical response: {results[0] != null}");

// Dispose the service when done
coalescingService.Dispose();
```

## RoutingService

The `RoutingService` class handles request routing, target selection, and URL construction for the API gateway. It finds matching routes based on request paths and HTTP methods, selects appropriate backend targets using configurable load balancing strategies (round-robin, IP hash, or least connections), applies header transformations, and builds forward URLs for request forwarding. The service also provides CRUD operations for managing gateway routes.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<GatewayRouteRepository>();
services.AddSingleton<RoutingService>();

var serviceProvider = services.BuildServiceProvider();
var routingService = serviceProvider.GetRequiredService<RoutingService>();

// Create a gateway route with multiple targets
var route = new GatewayRoute
{
    Name = "user-api",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET", "PUT", "DELETE"],
    Targets = [
        new RouteTarget
        {
            Name = "user-service-primary",
            BaseUrl = "https://user-service.internal:8080",
            Weight = 70,
            IsHealthy = true,
            TransformHeaders = new Dictionary<string, string>
            {
                ["X-Service-Version"] = "v2",
                ["X-Environment"] = "production"
            }
        },
        new RouteTarget
        {
            Name = "user-service-backup",
            BaseUrl = "https://user-service-backup.internal:8080", 
            Weight = 30,
            IsHealthy = true
        }
    ],
    TimeoutSeconds = 30
};

// Create the route in the gateway
var createdRoute = await routingService.CreateRouteAsync(route);
Console.WriteLine($"Created route: {createdRoute.Name} ({createdRoute.Id})");

// Find a route matching a request
var foundRoute = await routingService.FindRouteAsync("/api/users/123", "GET");
if (foundRoute != null)
{
    Console.WriteLine($"Found route: {foundRoute.Name}");
    
    // Select a target using round-robin strategy
    var selectedTarget = routingService.SelectTarget(foundRoute, "192.168.1.100");
    Console.WriteLine($"Selected target: {selectedTarget.Name} ({selectedTarget.BaseUrl})");
    
    // Build the forward URL for the request
    var forwardUrl = routingService.BuildForwardUrl(selectedTarget, "/api/users/123");
    Console.WriteLine($"Forward URL: {forwardUrl}");
    
    // Apply header transformations
    var originalHeaders = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer token123",
        ["Accept"] = "application/json"
    };
    
    var transformedHeaders = routingService.ApplyHeaderTransforms(selectedTarget, originalHeaders);
    Console.WriteLine("Transformed headers:");
    foreach (var header in transformedHeaders)
    {
        Console.WriteLine($"  {header.Key}: {header.Value}");
    }
}

// Get all active routes
var allRoutes = await routingService.GetAllActiveRoutesAsync();
Console.WriteLine($"Total active routes: {allRoutes.Count()}");

// Update a route
foundRoute.Name = "user-api-updated";
var updatedRoute = await routingService.UpdateRouteAsync(foundRoute);
Console.WriteLine($"Updated route: {updatedRoute.Name}");

// Delete a route
bool deleted = await routingService.DeleteRouteAsync(foundRoute.Id);
Console.WriteLine($"Route deleted: {deleted}");
```

## ConversionUtility

The `ConversionUtility` class provides safe type conversion and casting operations for handling various data types in the API gateway. It offers methods to convert strings to common primitive types (int, long, decimal, double, bool, DateTime, Guid) with configurable default values and exception handling. The utility also includes base64 encoding/decoding methods and a generic `ConvertTo<T>` method for type-safe conversions.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// Convert string to int with default value
int userId = ConversionUtility.ToInt("42", defaultValue: 0);
Console.WriteLine($"User ID: {userId}"); // Output: User ID: 42

// Convert string to decimal with default value
decimal price = ConversionUtility.ToDecimal("19.99", defaultValue: 0.00m);
Console.WriteLine($"Price: {price:C}"); // Output: Price: $19.99

// Convert string to bool with default value
bool isActive = ConversionUtility.ToBoolean("yes", defaultValue: false);
Console.WriteLine($"Is Active: {isActive}"); // Output: Is Active: True

// Convert string to DateTime with default value
DateTime createdAt = ConversionUtility.ToDateTime("2024-01-15T10:30:00Z", defaultValue: DateTime.MinValue);
Console.WriteLine($"Created At: {createdAt:O}"); // Output: Created At: 2024-01-15T10:30:00.0000000Z

// Convert string to Guid
Guid userGuid = ConversionUtility.ToGuid("550e8400-e29b-41d4-a716-446655440000", defaultValue: Guid.Empty);
Console.WriteLine($"User GUID: {userGuid}"); // Output: User GUID: 550e8400-e29b-41d4-a716-446655440000

// Convert byte array to base64 string
byte[] data = { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in ASCII
string base64 = ConversionUtility.ToBase64(data);
Console.WriteLine($"Base64: {base64}"); // Output: Base64: SGVsbG8=

// Convert base64 string back to byte array
byte[]? decodedData = ConversionUtility.FromBase64(base64);
Console.WriteLine($"Decoded: {System.Text.Encoding.ASCII.GetString(decodedData!)}"); // Output: Decoded: Hello

// Generic type conversion
string numberString = "12345";
int? convertedInt = ConversionUtility.ConvertTo<int>(numberString);
Console.WriteLine($"Converted to int: {convertedInt}"); // Output: Converted to int: 12345

// Handle invalid conversions gracefully
int invalidInt = ConversionUtility.ToInt("not-a-number", defaultValue: -1);
Console.WriteLine($"Invalid conversion result: {invalidInt}"); // Output: Invalid conversion result: -1
```

## RateLimitingService

The `RateLimitingService` class enforces rate limiting on API gateway requests using pluggable storage backends. It provides thread-safe rate limiting enforcement across different strategies (fixed window, sliding window, or token bucket) and allows inspection of current rate limit status. The service supports per-route rate limiting policies and can reset limits for specific keys or globally across all configured stores.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<IRateLimitStoreFactory, RateLimitStoreFactory>();
services.AddSingleton<RateLimitingService>();

var serviceProvider = services.BuildServiceProvider();
var rateLimitingService = serviceProvider.GetRequiredService<RateLimitingService>();

// Create a rate limit policy for a specific route
var policy = new RateLimitPolicy
{
    Enabled = true,
    RequestsPerMinute = 100, // Allow 100 requests per minute
    Strategy = RateLimitStrategy.SlidingWindow // Use sliding window algorithm
};

// Check if a request is allowed (returns false when limit is exceeded)
bool isAllowed = await rateLimitingService.IsAllowedAsync("client-123", policy);
Console.WriteLine($"Request allowed: {isAllowed}"); // Returns true if within limit

// Get current rate limit information for display to client
var rateLimitInfo = await rateLimitingService.GetRateLimitInfoAsync("client-123", policy);
Console.WriteLine($"Rate limit: {rateLimitInfo.Limit} requests per minute");
Console.WriteLine($"Remaining: {rateLimitInfo.Remaining} requests");
Console.WriteLine($"Reset in: {rateLimitInfo.Reset} seconds");

// Reset rate limits for a specific client
await rateLimitingService.ResetKeyLimitsAsync("client-123");
Console.WriteLine("Rate limits reset for client-123");

// Reset all rate limits globally
await rateLimitingService.ResetAllLimitsAsync();
Console.WriteLine("All rate limits reset globally");
```

## HeaderUtility

The `HeaderUtility` class provides utility methods for HTTP header manipulation and parsing operations in the API gateway. It offers safe, case-insensitive operations for getting, setting, adding, removing headers, and specialized methods for authentication header parsing. The utility ensures consistent header handling across the request/response pipeline with proper null safety and error handling.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using Microsoft.AspNetCore.Http;

// Create a mock header dictionary for demonstration
var headers = new HeaderDictionary(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
{
    ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0IiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
    ["X-Custom-Header"] = "custom-value",
    ["Content-Type"] = "application/json",
    ["Accept"] = "application/json"
});

// Get a header value (case-insensitive)
string? customHeader = HeaderUtility.GetHeader(headers, "x-custom-header");
Console.WriteLine($"Custom header: {customHeader}"); // Output: Custom header: custom-value

// Set a header value
HeaderUtility.SetHeader(headers, "X-Request-Id", Guid.NewGuid().ToString());

// Add a header (preserves multiple values)
HeaderUtility.AddHeader(headers, "X-Forwarded-For", "192.168.1.100");

// Check if a header exists
bool hasAuth = HeaderUtility.HasHeader(headers, "Authorization");
Console.WriteLine($"Has Authorization: {hasAuth}"); // Output: Has Authorization: True

// Remove a header
HeaderUtility.RemoveHeader(headers, "Accept");

// Extract bearer token from Authorization header
string? bearerToken = HeaderUtility.ExtractBearerToken(headers);
Console.WriteLine($"Bearer token length: {bearerToken?.Length}"); // Output: Bearer token length: 110

// Parse authentication challenge from WWW-Authenticate header
var challengeHeaders = new HeaderDictionary();
challengeHeaders.Append("WWW-Authenticate", "Bearer realm=\"api.example.com\", error=\"invalid_token\"");
var challenge = HeaderUtility.ParseAuthenticationChallenge(challengeHeaders);
Console.WriteLine($"Challenge scheme: {challenge[\"scheme\"]}"); // Output: Challenge scheme: Bearer
Console.WriteLine($"Realm: {challenge[\"realm\"]}"); // Output: Realm: api.example.com

// Copy headers from source to destination
var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
HeaderUtility.CopyHeaders(headers, request);
Console.WriteLine($"Copied {request.Headers.Count()} headers to request");

// Get custom headers (excluding standard HTTP headers)
var customHeaders = HeaderUtility.GetCustomHeaders(headers);
Console.WriteLine($"Custom headers count: {customHeaders.Count}");
foreach (var header in customHeaders)
{
    Console.WriteLine($" {header.Key}: {header.Value}");
}
```

## DateTimeUtility

The `DateTimeUtility` class provides utility methods for date and time operations in the API gateway. It offers functionality for working with Unix timestamps, formatting dates, calculating relative time strings, and determining date boundaries (start/end of day, week, month). The utility handles UTC time conversions and provides helper methods for common date calculations.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using System;

// Get current UTC time in ISO 8601 format
string currentUtcTime = DateTimeUtility.GetCurrentUtcIso8601();
Console.WriteLine($"Current UTC time: {currentUtcTime}");

// Get Unix timestamp for current time
long currentUnixTimestamp = DateTimeUtility.GetCurrentUnixTimestamp();
Console.WriteLine($"Current Unix timestamp: {currentUnixTimestamp}");

// Convert DateTime to Unix timestamp
DateTime now = DateTime.UtcNow;
long unixTimestamp = DateTimeUtility.ToUnixTimestamp(now);
Console.WriteLine($"Unix timestamp for {now:O}: {unixTimestamp}");

// Convert Unix timestamp back to DateTime
DateTime dateTimeFromTimestamp = DateTimeUtility.FromUnixTimestamp(unixTimestamp);
Console.WriteLine($"DateTime from timestamp: {dateTimeFromTimestamp:O}");

// Format DateTime as human-readable string
DateTime exampleDate = new DateTime(2024, 6, 15, 14, 30, 0);
string formattedDate = DateTimeUtility.FormatDateTime(exampleDate);
Console.WriteLine($"Formatted date: {formattedDate}");

// Get relative time string
string relativeTime = DateTimeUtility.GetRelativeTime(exampleDate);
Console.WriteLine($"Relative time: {relativeTime}");

// Check if datetime is in the past or future
bool isPast = DateTimeUtility.IsPast(exampleDate);
bool isFuture = DateTimeUtility.IsFuture(exampleDate.AddDays(1));
Console.WriteLine($"Is past: {isPast}, Is future: {isFuture}");

// Get start and end of day
DateTime startOfDay = DateTimeUtility.GetStartOfDay(exampleDate);
DateTime endOfDay = DateTimeUtility.GetEndOfDay(exampleDate);
Console.WriteLine($"Start of day: {startOfDay:O}");
Console.WriteLine($"End of day: {endOfDay:O}");

// Get start and end of week
DateTime startOfWeek = DateTimeUtility.GetStartOfWeek(exampleDate);
DateTime endOfWeek = DateTimeUtility.GetEndOfWeek(exampleDate);
Console.WriteLine($"Start of week: {startOfWeek:yyyy-MM-dd}");
Console.WriteLine($"End of week: {endOfWeek:yyyy-MM-dd}");

// Get start and end of month
DateTime startOfMonth = DateTimeUtility.GetStartOfMonth(exampleDate);
DateTime endOfMonth = DateTimeUtility.GetEndOfMonth(exampleDate);
Console.WriteLine($"Start of month: {startOfMonth:yyyy-MM-dd}");
Console.WriteLine($"End of month: {endOfMonth:yyyy-MM-dd}");

// Check if two datetimes are on the same day
bool isSameDay = DateTimeUtility.IsSameDay(startOfDay, endOfDay);
Console.WriteLine($"Is same day: {isSameDay}");

// Calculate business days between two dates
int businessDays = DateTimeUtility.GetBusinessDaysBetween(
    new DateTime(2024, 6, 10),
    new DateTime(2024, 6, 20)
);
Console.WriteLine($"Business days between 2024-06-10 and 2024-06-20: {businessDays}");
```

## CryptoUtility

The `CryptoUtility` class provides cryptographic operations including SHA256 hashing and HMAC-SHA256 signature generation for data integrity verification and webhook signature validation. It offers secure methods for generating cryptographic hashes, creating message authentication codes, and verifying signatures using constant-time comparison to prevent timing attacks.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// Generate SHA256 hash of a string
string inputData = "Hello, World!";
string sha256Hash = CryptoUtility.GenerateSha256Hash(inputData);
Console.WriteLine($"SHA256 hash: {sha256Hash}");

// Generate SHA256 hash of byte array
byte[] dataBytes = Encoding.UTF8.GetBytes(inputData);
string sha256HashBytes = CryptoUtility.GenerateSha256Hash(dataBytes);
Console.WriteLine($"SHA256 hash (bytes): {sha256HashBytes}");

// Generate HMAC-SHA256 signature for webhook verification
string webhookData = "{\"event\":\"user.created\",\"userId\":123}";
string webhookSecret = "your-secret-key-12345";
string hmacSignature = CryptoUtility.GenerateHmacSha256(webhookData, webhookSecret);
Console.WriteLine($"HMAC-SHA256 signature: {hmacSignature}");

// Verify HMAC signature (constant-time comparison)
bool isValid = CryptoUtility.VerifyHmacSha256(webhookData, hmacSignature, webhookSecret);
Console.WriteLine($"Signature valid: {isValid}");

// Generate cryptographically secure random string for API keys or tokens
string apiKey = CryptoUtility.GenerateRandomString(64);
Console.WriteLine($"Generated API key: {apiKey}");

// Generate random bytes for cryptographic operations
byte[] randomBytes = CryptoUtility.GenerateRandomBytes(32);
Console.WriteLine($"Generated {randomBytes.Length} random bytes");
```

## EventBusExtensions

The `EventBusExtensions` class provides extension methods for the `EventBus` that enable monitoring and management of event subscribers. These methods allow you to inspect subscriber counts, check for active subscriptions, and publish multiple events as a batch operation.

Example usage:

```csharp
using DotNetApiGateway.Events;
using System;
using System.Threading.Tasks;

// Create an event bus instance (typically injected via DI)
var eventBus = new EventBus();

// Publish some events
await eventBus.PublishAsync(new UserCreatedEvent { UserId = 123, Username = "john_doe" });
await eventBus.PublishAsync(new OrderPlacedEvent { OrderId = 456, Amount = 99.99m });

// Check if there are subscribers for a specific event type
bool hasUserSubscribers = eventBus.HasSubscribers<UserCreatedEvent>();
Console.WriteLine($"Has UserCreatedEvent subscribers: {hasUserSubscribers}");

// Get all event types that currently have subscribers
var eventTypesWithSubscribers = eventBus.GetEventTypesWithSubscribers();
Console.WriteLine("Event types with subscribers:");
foreach (var eventType in eventTypesWithSubscribers)
{
    Console.WriteLine($" - {eventType}");
}

// Get subscriber counts for all event types
var allSubscriberCounts = eventBus.GetAllSubscriberCounts();
Console.WriteLine("Subscriber counts:");
foreach (var kvp in allSubscriberCounts)
{
    Console.WriteLine($" - {kvp.Key}: {kvp.Value} subscribers");
}

// Get total subscriber count across all event types
int totalSubscribers = eventBus.GetTotalSubscriberCount();
Console.WriteLine($"Total subscribers: {totalSubscribers}");

// Publish multiple events as a batch
var eventsToPublish = new object[]
{
    new UserUpdatedEvent { UserId = 123, Email = "john@example.com" },
    new UserDeletedEvent { UserId = 789 },
    new OrderCancelledEvent { OrderId = 101, Reason = "Customer request" }
};

await eventBus.PublishBatchAsync(eventsToPublish);
```

## ExtensionMethods

The `ExtensionMethods` class provides extension methods for common types used throughout the API gateway. It offers a fluent API for string manipulation, collections, and object operations, including null-safe checks, transformations, and formatting utilities.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// String extension methods
string message = "Hello, World!";

// Check if string is empty or whitespace
bool isEmpty = message.IsEmpty(); // Returns false
bool hasContent = message.HasContent(); // Returns true

// Truncate string to maximum length
string truncated = message.Truncate(8); // Returns "Hello..."

// Remove specific characters from string
string cleaned = message.Remove('!', ' '); // Returns "Hello,World"

// Repeat string multiple times
string repeated = message.Repeat(2); // Returns "Hello, World!Hello, World!"

// Convert string to byte array
byte[] bytes = message.ToBytes();

// Convert byte array to hex string
string hex = bytes.ToHexString(); // Returns "48656c6c6f2c20576f726c6421"

// Collection extension methods
List<string> items = new List<string> { "item1", "item2", "item3" };

// Check if collection is empty
bool isEmptyCollection = items.IsEmpty(); // Returns false
bool hasElements = items.HasElements(); // Returns true

// Safely get element at index
string? item = items.GetOrDefault(5, "default"); // Returns "default"

// Dictionary extension methods
Dictionary<string, string> headers = new Dictionary<string, string>
{
    ["Content-Type"] = "application/json",
    ["Authorization"] = "Bearer token123"
};

// Convert dictionary keys to lowercase
auto lowerHeaders = headers.ToLowerKeyDictionary();

// Merge two dictionaries
auto merged = new Dictionary<string, string> { ["X-Custom"] = "value1" }
    .Merge(headers);

// Format milliseconds as human-readable time
long durationMs = 1250;
string formatted = durationMs.FormatMilliseconds(); // Returns "1.25s"

// Check if string matches any pattern
bool matches = "error".MatchesAny("success", "warning", "error"); // Returns true

// Get approximate memory size of string
long size = message.GetMemorySize(); // Returns size in bytes
```

## RateLimitMetrics

The `RateLimitMetrics` class provides tracking and analysis capabilities for rate limit usage patterns and violations in the API gateway. It maintains statistics for individual clients and overall system metrics, enabling monitoring of rate limit compliance, identifying problematic clients, and analyzing usage patterns over time.

Example usage:

```csharp
using DotNetApiGateway.Utilities;

// Create rate limit metrics tracker
var metrics = new RateLimitMetrics();

// Record requests from different clients
metrics.RecordRequest("client-123");
metrics.RecordRequest("client-123");
metrics.RecordRequest("client-456", limited: true); // Limited request
metrics.RecordRequest("client-789");
metrics.RecordRequest("client-123", limited: true);
metrics.RecordRequest("client-456", limited: true);

// Get statistics for a specific client
var clientStats = metrics.GetClientStats("client-123");
if (clientStats != null)
{
    Console.WriteLine($"Client: {clientStats.ClientId}");
    Console.WriteLine($"Total requests: {clientStats.TotalRequests}");
    Console.WriteLine($"Limited requests: {clientStats.LimitedRequests}");
    Console.WriteLine($"Violation rate: {clientStats.ViolationRate:P}");
    Console.WriteLine($"Active duration: {clientStats.ActiveDuration.TotalMinutes:F1} minutes");
}

// Get top 10 clients by request count
var topClients = metrics.GetTopClients(10);
Console.WriteLine($"Top clients: {string.Join(", ", topClients.Select(c => c.ClientId))}");

// Get clients with highest violation rates
var violatingClients = metrics.GetViolatingClients(5);
Console.WriteLine($"Clients with violations: {violatingClients.Count}");

// Get overall system metrics
var overallMetrics = metrics.GetOverallMetrics();
Console.WriteLine($"Total clients: {overallMetrics.TotalClients}");
Console.WriteLine($"Total requests: {overallMetrics.TotalRequests}");
Console.WriteLine($"Total limited: {overallMetrics.TotalLimitedRequests}");
Console.WriteLine($"Avg requests/client: {overallMetrics.AverageRequestsPerClient:F1}");
Console.WriteLine($"Overall violation rate: {overallMetrics.OverallViolationRate:P}");

// Remove old entries (older than 1 hour)
int removedCount = metrics.RemoveOldEntries(TimeSpan.FromHours(1));
Console.WriteLine($"Removed {removedCount} old entries");

// Clear all statistics
metrics.Clear();
```

## RateLimitPolicyExtensions

The `RateLimitPolicyExtensions` class provides extension methods for the `RateLimitPolicy` type, enabling fluent-style operations to create modified copies of rate limit policies. These methods allow you to conveniently adjust rate limiting configuration without mutating the original policy object, making it easier to compose different rate limit strategies for different routes or clients.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a base rate limit policy with default settings
var basePolicy = new RateLimitPolicy
{
    Id = "default-rate-limit",
    RequestsPerMinute = 100,
    Strategy = RateLimitStrategy.FixedWindow,
    Enabled = true,
    StorageType = RateLimitStorageType.Memory
};

// Create a more restrictive policy for sensitive endpoints by chaining extensions
var sensitivePolicy = basePolicy
    .WithRequestsPerMinute(10)  // Only 10 requests per minute
    .WithStrategy(RateLimitStrategy.TokenBucket)  // Use token bucket for smoother limiting
    .WithStorage(RateLimitStorageType.Redis, "localhost:6379")  // Use distributed Redis storage
    .WithDisabled();  // Explicitly disable (overriding base policy)

// Create a policy for authenticated users with higher limits
var authenticatedPolicy = basePolicy
    .WithRequestsPerHour(1000)  // 1000 requests per hour
    .WithEnabled();  // Ensure enabled

// Create a policy for public APIs with burst capability
var publicApiPolicy = basePolicy
    .WithRequestsPerMinute(200)
    .WithBurstSize(50);  // Allow bursts of 50 requests

// Use the configured policies in your gateway routes
var userRoute = new GatewayRoute
{
    Id = "user-api",
    Name = "User API",
    PathPattern = "/api/users/{id}",
    RateLimitPolicyId = sensitivePolicy.Id,  // Apply sensitive policy to user endpoints
    AllowedMethods = ["GET", "PUT", "DELETE"]
};

var publicRoute = new GatewayRoute
{
    Id = "public-api",
    Name = "Public API",
    PathPattern = "/api/public/{resource}",
    RateLimitPolicyId = publicApiPolicy.Id,  // Apply public policy to public endpoints
    AllowedMethods = ["GET"]
};
```

## RequestCoalescingPolicy

The `RequestCoalescingPolicy` class defines coalescing behavior for duplicate concurrent requests. When multiple identical requests arrive simultaneously, coalescing ensures only one upstream call is made and the result is shared with all waiters. This reduces load on upstream services and improves response times for duplicate requests.

## AggregationPolicy

The `AggregationPolicy` class defines how multiple upstream targets are aggregated when a request is processed. It supports different aggregation strategies (parallel, sequential, or conditional) and allows configuration of conditional targets that determine which upstream services receive the request based on conditions. Aggregation policies are useful for implementing canary deployments, blue-green deployments, A/B testing, or routing requests to different backend services based on request characteristics.

## AggregatedResponse

The `AggregatedResponse` class represents the aggregated result of multiple HTTP requests processed in parallel or sequentially. It collects response data from multiple upstream services, tracks success/failure counts, calculates total duration, and provides methods to analyze the aggregated results. This type is particularly useful for implementing request aggregation patterns, consolidating responses from multiple backend services, and implementing fallback strategies.

Example usage:

```csharp
using DotNetApiGateway.Models;
using System;
using System.Collections.Generic;

// Create an aggregated response to collect results from multiple backend services
var aggregatedResponse = new AggregatedResponse
{
    Id = "agg-12345",
    AggregatedAt = DateTime.UtcNow
};

// Simulate adding responses from different services
aggregatedResponse.AddResponse(
    alias: "user-service-primary",
    statusCode: 200,
    body: "{\"userId\": 123, \"name\": \"John Doe\"}",
    headers: new Dictionary<string, string> { ["X-Service"] = "primary" },
    duration: TimeSpan.FromMilliseconds(125),
    errorMessage: null
);

aggregatedResponse.AddResponse(
    alias: "user-service-secondary",
    statusCode: 200,
    body: "{\"userId\": 123, \"name\": \"John Doe\", \"email\": \"john@example.com\"}",
    headers: new Dictionary<string, string> { ["X-Service"] = "secondary" },
    duration: TimeSpan.FromMilliseconds(180),
    errorMessage: null
);

aggregatedResponse.AddResponse(
    alias: "user-service-fallback",
    statusCode: 503,
    body: null,
    headers: new Dictionary<string, string> { ["X-Service"] = "fallback" },
    duration: TimeSpan.FromMilliseconds(50),
    errorMessage: "Service temporarily unavailable"
);

// Access aggregated response statistics
Console.WriteLine($"Total responses: {aggregatedResponse.Responses.Count}");
Console.WriteLine($"Success count: {aggregatedResponse.SuccessCount}");
Console.WriteLine($"Failure count: {aggregatedResponse.FailureCount}");
Console.WriteLine($"Total duration: {aggregatedResponse.TotalDuration.TotalMilliseconds}ms");
Console.WriteLine($"Average response time: {aggregatedResponse.GetAverageResponseTime()}ms");
Console.WriteLine($"Is successful: {aggregatedResponse.IsSuccessful()}");

// Retrieve individual responses
var primaryResponse = aggregatedResponse.GetResponse("user-service-primary");
if (primaryResponse != null)
{
    Console.WriteLine($"Primary service status: {primaryResponse.StatusCode}");
    Console.WriteLine($"Primary service duration: {primaryResponse.Duration.TotalMilliseconds}ms");
    Console.WriteLine($"Primary service body: {primaryResponse.Body}");
}
```

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

## DotnetApiGatewayOptions

The `DotnetApiGatewayOptions` class defines the configuration settings for the API gateway. It controls core gateway behavior including request handling limits, timeout configurations, CORS settings, compression, logging, metrics collection, health checks, JWT validation, and route management. This configuration is typically loaded from application settings and provides centralized control over gateway behavior.

Example usage:

```csharp
using DotNetApiGateway.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Configure gateway options in Program.cs
var builder = WebApplication.CreateBuilder(args);

// Bind configuration to DotnetApiGatewayOptions
builder.Services.Configure<DotnetApiGatewayOptions>(builder.Configuration.GetSection(DotnetApiGatewayOptions.SectionName));

// Access configured options
var gatewayOptions = builder.Services.BuildServiceProvider()
    .GetRequiredService<IOptions<DotnetApiGatewayOptions>>().Value;

// Use the configuration
Console.WriteLine($"Gateway Application: {gatewayOptions.ApplicationName}");
Console.WriteLine($"Gateway Version: {gatewayOptions.Version}");
Console.WriteLine($"Max Request Body Size: {gatewayOptions.MaxRequestBodySize} bytes");
Console.WriteLine($"Default Timeout: {gatewayOptions.DefaultTimeoutSeconds} seconds");
Console.WriteLine($"Max Concurrent Requests: {gatewayOptions.MaxConcurrentRequests}");
Console.WriteLine($"CORS Enabled: {gatewayOptions.EnableCors}");
Console.WriteLine($"Compression Enabled: {gatewayOptions.EnableCompression}");
Console.WriteLine($"Logging Enabled: {gatewayOptions.EnableLogging}");
Console.WriteLine($"Metrics Enabled: {gatewayOptions.EnableMetrics}");
Console.WriteLine($"Health Check Enabled: {gatewayOptions.EnableHealthCheck}");
Console.WriteLine($"Health Check Path: {gatewayOptions.HealthCheckPath}");
Console.WriteLine($"Log Level: {gatewayOptions.LogLevel}");

// Configure JWT validation
if (gatewayOptions.JwtValidation.Enabled)
{
    Console.WriteLine($"JWT Issuer: {gatewayOptions.JwtValidation.Issuer}");
    Console.WriteLine($"JWT Audience: {gatewayOptions.JwtValidation.Audience}");
}

// Access configured routes
gatewayOptions.Routes.ForEach(route => 
{
    Console.WriteLine($"Route: {route.Name} ({route.PathPattern})");
    Console.WriteLine($"  Methods: {string.Join(", ", route.AllowedMethods)}");
    Console.WriteLine($"  Timeout: {route.TimeoutSeconds}s");
});
```

## CachePolicy

The `CachePolicy` class defines caching behavior for API gateway routes, enabling response caching to improve performance and reduce load on upstream services. It supports configurable cache duration, cacheable HTTP methods and status codes, cache key variation by query strings and headers, and limits on cache size. The policy can be enabled/disabled per route and provides validation to ensure configuration integrity.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a cache policy for frequently accessed endpoints
var cachePolicy = new CachePolicy
{
  Id = "user-profile-cache",
  Enabled = true,
  DurationSeconds = 300, // Cache responses for 5 minutes
  Strategy = CacheStrategy.Response,
  CacheableStatusCodes = [200, 201, 204], // Cache successful responses
  CacheableHttpMethods = ["GET", "HEAD"], // Only cache GET and HEAD requests
  VaryByQueryString = true, // Include query parameters in cache key
  VaryByHeaders = ["Accept-Language", "Authorization"], // Vary cache by language and auth
  MaxEntriesInCache = 1000 // Limit cache size to 1000 entries
};

// Validate the cache policy configuration
cachePolicy.Validate();

// Check if caching is enabled
bool isEnabled = cachePolicy.Enabled; // Returns true

// Check if a specific HTTP status code should be cached
bool isCacheableStatus = cachePolicy.IsCacheable(200); // Returns true

// Check if a specific HTTP method can be cached
bool isCacheableMethod = cachePolicy.IsCacheable("GET"); // Returns true

// Generate a cache key for a request
string cacheKey = cachePolicy.GenerateCacheKey(
  "/api/users/123",
  "GET",
  new Dictionary<string, string> { ["lang"] = "en-US", ["fields"] = "name,email" },
  new Dictionary<string, string> { ["Accept-Language"] = "en-US" }
);
// Returns: "GET:/api/users/123?lang=en-US&fields=name,email|Accept-Language:en-US"
```

## HealthCheckService

The `HealthCheckService` class monitors the health of backend targets in the API gateway. It performs health checks on individual targets or all tracked targets, maintains a registry of targets for periodic monitoring, and provides comprehensive gateway health information including uptime, version, and detailed status of all tracked targets.

The service automatically tracks targets that are checked and performs periodic health checks every 30 seconds using a background timer. Health check results are used to update target health status and can be queried to determine overall gateway health.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddHttpClient();
services.AddSingleton<HealthCheckService>();

var serviceProvider = services.BuildServiceProvider();
var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

// Create a route target with health check configuration
var target = new RouteTarget
{
    Id = "user-service-primary",
    Name = "User Service - Primary",
    BaseUrl = "https://user-service.internal:8080",
    IsHealthy = true,
    HealthCheckPath = "/health",
    HealthCheckIntervalSeconds = 30
};

// Check health of a specific target
bool isHealthy = await healthCheckService.CheckTargetHealthAsync(target);
Console.WriteLine($"Target health status: {(isHealthy ? "HEALTHY" : "UNHEALTHY")}");

// Check health of multiple targets
var targets = new List<RouteTarget> { target };
var allHealthResults = await healthCheckService.CheckAllTargetsAsync(targets);
foreach (var result in allHealthResults)
{
    Console.WriteLine($"Target {result.Key}: {(result.Value ? "HEALTHY" : "UNHEALTHY")}");
}

// Get comprehensive gateway health report
gatewayHealth = healthCheckService.GetGatewayHealth();
Console.WriteLine($"Gateway Health: {gatewayHealth.IsHealthy}");
Console.WriteLine($"Uptime: {gatewayHealth.Uptime.TotalHours:F2} hours");
Console.WriteLine($"Version: {gatewayHealth.Version}");
Console.WriteLine($"Tracked targets: {gatewayHealth.Details["trackedTargets"]}");
Console.WriteLine($"Unhealthy targets: {gatewayHealth.Details["unhealthyTargets"]}");

// The service runs periodic health checks automatically in the background
// No manual cleanup needed - Dispose() handles timer and HTTP client cleanup
```

## CacheService

The `CacheService` class provides an in-memory response caching mechanism for the API gateway. It manages cached responses with configurable expiration times, supports cache invalidation by key or prefix, and provides statistics about cache usage. The service is thread-safe using `ReaderWriterLockSlim` for concurrent access and includes automatic cleanup of expired entries via a background timer.

Example usage:

```csharp
using DotNetApiGateway.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create the cache service (typically registered as a singleton in DI)
var cacheService = new CacheService();

// Cache a response with custom headers and expiration
var cacheKey = "user-profile:123";
var responseHeaders = new Dictionary<string, string>
{
    ["Content-Type"] = "application/json",
    ["X-Cache-Status"] = "HIT"
};

cacheService.SetCachedResponse(
    cacheKey,
    statusCode: 200,
    responseBody: "{\"id\": 123, \"name\": \"John Doe\", \"email\": \"john@example.com\"}",
    headers: responseHeaders,
    durationSeconds: 300 // 5 minutes TTL
);

// Try to retrieve a cached response
if (cacheService.TryGetCachedResponse(cacheKey, out var cachedEntry) && cachedEntry != null)
{
    Console.WriteLine($"Cache HIT - Status: {cachedEntry.StatusCode}");
    Console.WriteLine($"Body: {cachedEntry.ResponseBody}");
    Console.WriteLine($"Headers: {string.Join(", ", cachedEntry.Headers.Select(h => $"{h.Key}={h.Value}")}");
    Console.WriteLine($"Cached at: {cachedEntry.CachedAt:O}");
    Console.WriteLine($"Expires at: {cachedEntry.ExpiresAt:O}");
    Console.WriteLine($"Hits: {cachedEntry.HitCount}");
    Console.WriteLine($"Last accessed: {cachedEntry.LastAccessAt:O}");
}
else
{
    Console.WriteLine("Cache MISS - Response not found or expired");
}

// Store and retrieve strongly-typed objects
var userData = new UserProfile { Id = 123, Name = "John Doe", Email = "john@example.com" };
await cacheService.SetAsync(
    "user-profile:123",
    userData,
    TimeSpan.FromMinutes(5)
);

var cachedUser = await cacheService.GetAsync<UserProfile>("user-profile:123");
if (cachedUser != null)
{
    Console.WriteLine($"Retrieved user: {cachedUser.Name} ({cachedUser.Email})");
}

// Invalidate cache by key or prefix
cacheService.InvalidateCache("user-profile:123");

// Invalidate all cache entries matching a prefix
cacheService.InvalidateCacheByPrefix("user-profile:");

// Get cache statistics
var stats = cacheService.GetStatistics();
Console.WriteLine($"Cache entries: {stats.EntriesCount}");
Console.WriteLine($"Total hits: {stats.TotalHits}");
Console.WriteLine($"Total size: {stats.TotalSizeBytes} bytes");
Console.WriteLine($"Hit rate: {stats.GetHitRate():P}");
Console.WriteLine($"Average size per entry: {stats.GetAverageSizePerEntryBytes():N0} bytes");

// Remove expired entries (can also be called via background timer)
int removedCount = await cacheService.RemoveExpiredEntriesAsync();
Console.WriteLine($"Removed {removedCount} expired entries");

// Clear all cache entries
cacheService.ClearAll();

// Dispose the service when done (stops the cleanup timer)
cacheService.Dispose();
```

## InMemoryRateLimitStore

The `InMemoryRateLimitStore` class provides an in-memory implementation of rate limiting storage for the API gateway. It supports multiple rate limiting strategies (fixed window, sliding window, and token bucket) and maintains rate limit state in memory using thread-safe collections. This store is ideal for single-instance deployments where distributed coordination is not required.

The implementation tracks request counts, token availability, and time windows for different rate limiting algorithms, providing accurate rate limit enforcement and status information. It includes methods for checking if requests are allowed, retrieving current rate limit status, and resetting limits for specific keys or globally.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<InMemoryRateLimitStore>();

var serviceProvider = services.BuildServiceProvider();
var rateLimitStore = serviceProvider.GetRequiredService<InMemoryRateLimitStore>();

// Create a rate limit policy for API clients
var policy = new RateLimitPolicy
{
    Enabled = true,
    RequestsPerMinute = 100, // Allow 100 requests per minute
    Strategy = RateLimitStrategy.TokenBucket, // Use token bucket algorithm
    BurstSize = 200 // Allow bursts up to 200 requests
};

// Check if a request is allowed (returns false when limit is exceeded)
bool isAllowed = await rateLimitStore.IsRequestAllowedAsync("client-123", policy);
Console.WriteLine($"Request allowed: {isAllowed}");

// Get current rate limit information for display to client
var rateLimitInfo = await rateLimitStore.GetEntryAsync("client-123", policy);
Console.WriteLine($"Rate limit: {policy.RequestsPerMinute} requests per minute");
Console.WriteLine($"Remaining tokens: {rateLimitInfo.Tokens}");
Console.WriteLine($"Reset in: {rateLimitInfo.RemainingTimeSeconds} seconds");

// Reset rate limits for a specific client
await rateLimitStore.ResetKeyAsync("client-123");
Console.WriteLine("Rate limits reset for client-123");

// Reset all rate limits globally
await rateLimitStore.ResetAllAsync();
Console.WriteLine("All rate limits reset globally");
```

## RedisRateLimitStore

The `RedisRateLimitStore` class provides a distributed Redis-backed implementation of the rate limiting storage interface (`IRateLimitStore`) for the API gateway. It supports all three rate limiting strategies (fixed window, sliding window, and token bucket) using Redis data structures for distributed coordination across multiple gateway instances.

This store is ideal for cloud-native deployments and microservices architectures where multiple API gateway instances need to share rate limiting state. It uses Redis sorted sets for sliding window tracking, strings for fixed window counting, and Redis hashes for token bucket state management, ensuring accurate rate limit enforcement across distributed systems.

## PerformanceAnalyzer

The `PerformanceAnalyzer` class provides a robust mechanism for tracking and aggregating performance metrics over time. It allows developers to record individual measurements and retrieve statistical summaries, including averages, percentiles, and min/max values, to facilitate performance monitoring and optimization.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using System;

// Create a performance analyzer
var analyzer = new PerformanceAnalyzer();

// Record measurements in milliseconds
analyzer.RecordMeasurement(150);
analyzer.RecordMeasurement(200);
analyzer.RecordMeasurement(250);

// Get specific statistics
double avg = analyzer.GetAverage();
long min = analyzer.GetMinimum();
long max = analyzer.GetMaximum();
long median = analyzer.GetMedian();
long p95 = analyzer.GetPercentile95();
int count = analyzer.GetCount();

Console.WriteLine($"Average: {avg:F2}ms, Count: {count}");

// Get a summary object
var summary = analyzer.GetSummary();
Console.WriteLine(summary.ToString());

// Clear measurements for reuse
analyzer.Clear();
```

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<RedisRateLimitStore>(provider =>
    new RedisRateLimitStore(
        "localhost:6379,abortConnect=false",
        provider.GetRequiredService<ILogger<RedisRateLimitStore>>()
    )
);

var serviceProvider = services.BuildServiceProvider();
var redisRateLimitStore = serviceProvider.GetRequiredService<RedisRateLimitStore>();

// Create a rate limit policy for API clients
var policy = new RateLimitPolicy
{
    Id = "api-client-rate-limit",
    Enabled = true,
    RequestsPerMinute = 1000, // Allow 1000 requests per minute
    Strategy = RateLimitStrategy.SlidingWindow, // Use sliding window algorithm for better accuracy
    BurstSize = 2000 // Allow bursts up to 2000 requests
};

// Check if a request is allowed (returns false when limit is exceeded)
bool isAllowed = await redisRateLimitStore.IsRequestAllowedAsync("client-123", policy);
Console.WriteLine($"Request allowed: {isAllowed}");

// Get current rate limit information for display to client
var rateLimitEntry = await redisRateLimitStore.GetEntryAsync("client-123", policy);
Console.WriteLine($"Rate limit: {policy.RequestsPerMinute} requests per minute");
Console.WriteLine($"Current count: {rateLimitEntry.Count}");
Console.WriteLine($"Remaining time: {rateLimitEntry.RemainingTimeSeconds} seconds until reset");

// Reset rate limits for a specific client
await redisRateLimitStore.ResetKeyAsync("client-123");
Console.WriteLine("Rate limits reset for client-123");

// Reset all rate limits globally
await redisRateLimitStore.ResetAllAsync();
Console.WriteLine("All rate limits reset globally");

// Dispose the Redis connection when done
redisRateLimitStore.Dispose();
```

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides global error handling for the API gateway, catching all unhandled exceptions and converting them to standardized HTTP error responses. It ensures consistent error formatting across all routes and services, logs exceptions for debugging, and maps gateway-specific exceptions to appropriate HTTP status codes.

Example usage:

```csharp
using DotNetApiGateway.Middleware;
using DotNetApiGateway.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add error handling middleware - should be registered early in the pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Example of handling gateway-specific exceptions
try
{
    // Your gateway routing logic here
}
catch (RateLimitExceededException ex)
{
    // ErrorResponse properties available:
    // - ErrorCode: "RATE_LIMIT_EXCEEDED"
    // - Message: The rate limit error message
    // - Timestamp: DateTime.UtcNow when error occurred
    // - Details: Optional dictionary with additional error details
    Console.WriteLine($"Rate limit error: {ex.Message} at {ex.Timestamp}");
}
catch (CircuitBreakerException ex)
{
    Console.WriteLine($"Circuit breaker error: {ex.Message} at {ex.Timestamp}");
}
catch (AuthenticationException ex)
{
    Console.WriteLine($"Authentication error: {ex.Message} at {ex.Timestamp}");
}
catch (RouteNotFoundException ex)
{
    Console.WriteLine($"Route not found: {ex.Message} at {ex.Timestamp}");
}
```

## CircuitBreakerExample

The `CircuitBreakerExample` demonstrates the circuit breaker pattern implementation in the API gateway, showing how it protects against cascading failures by monitoring service health and temporarily blocking requests to failing services. This example illustrates state transitions (Closed → Open → Half-Open → Closed), configuration parameters, and realistic usage scenarios.


Example usage:

```csharp
using DotNetApiGateway.Examples;
using System;
using System.Threading.Tasks;

// Run the circuit breaker example
await CircuitBreakerExample.Main();

// Output:
// === DotNet API Gateway - Circuit Breaker Example ===
//
// Step 1: Problem Without Circuit Breaker
// Step 2: Circuit Breaker Configuration
// Step 3: Circuit Breaker State Transitions
// ...
// ✓ Example completed successfully!
```

The example simulates real-world scenarios:

```csharp
// Create a simple circuit breaker with custom thresholds
var breaker = new SimpleCircuitBreaker(
    failureThreshold: 3,
    successThreshold: 2,
    timeoutSeconds: 5);

// Check if request can be executed
if (breaker.CanExecute())
{
    try
    {
        // Make request to backend service
        var response = await httpClient.GetAsync("https://backend/api/data");
        
        // Record success
        breaker.RecordSuccess();
    }
    catch (Exception ex)
    {
        // Record failure
        breaker.RecordFailure();
        
        // Circuit will open after failureThreshold failures
        if (breaker.State == CircuitState.Open)
        {
            // Return cached response or degraded functionality
            return await GetCachedResponseAsync();
        }
    }
}
else
{
    // Circuit is OPEN - fail fast
    return await GetFallbackResponseAsync();
}
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

## WebhookSubscription

The `WebhookSubscription` class represents a webhook subscription configuration that defines where and how webhook events should be delivered. It includes the callback URL, event types to subscribe to, authentication secret, activation status, and delivery retry policy configuration.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a webhook subscription for order events
var subscription = new WebhookSubscription
{
  Id = "order-events-sub",
  CallbackUrl = "https://webhook-handler.example.com/api/webhooks/order",
  EventTypes = new[] { "order.created", "order.updated", "order.cancelled" },
  Secret = "your-webhook-secret-key",
  Active = true,
  RetryPolicy = new WebhookRetryPolicy
  {
    MaxRetries = 5,
    InitialDelayMs = 1000,
    MaxDelayMs = 30000
  }
};

// Access delivery statistics
subscription.DeliveryStats.TotalDeliveries = 150;
subscription.DeliveryStats.SuccessfulDeliveries = 145;
subscription.DeliveryStats.FailedDeliveries = 5;
subscription.DeliveryStats.LastDeliveryTime = DateTime.UtcNow.AddMinutes(-2);

// Validate the subscription configuration
subscription.Validate();
```

## JsonUtility

The `JsonUtility` class provides utility methods for JSON serialization and deserialization operations with consistent formatting and error handling. It supports both compact and pretty-printed serialization, safe deserialization that returns null instead of throwing exceptions, and JSON validation. The class uses standardized JSON serialization options with camelCase property naming and proper null handling throughout the API gateway.

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Utilities;

// Create a sample user object
var user = new User
{
    Id = 123,
    Name = "John Doe",
    Email = "john.doe@example.com",
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

// Serialize to compact JSON string
string compactJson = JsonUtility.Serialize(user);
Console.WriteLine(compactJson);
// Output: {"id":123,"name":"John Doe","email":"john.doe@example.com","isActive":true,"createdAt":"2024-01-15T10:30:00Z"}

// Serialize to pretty-printed JSON for logging/debugging
string prettyJson = JsonUtility.SerializePretty(user);
Console.WriteLine(prettyJson);
/* Output:
{
  "id": 123,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z"
}
*/

// Deserialize JSON back to object
string jsonData = "{\"id\":456,\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}";
var deserializedUser = JsonUtility.Deserialize<User>(jsonData);
if (deserializedUser != null)
{
    Console.WriteLine($"Deserialized: {deserializedUser.Name} ({deserializedUser.Email})");
}

// Safely deserialize with null return on failure (won't throw exception)
string invalidJson = "{invalid json}";
var safeDeserialized = JsonUtility.DeserializeSafe<User>(invalidJson);
// Returns null instead of throwing JsonException

// Validate JSON without throwing exceptions
bool isValid = JsonUtility.IsValidJson(jsonData); // Returns true
bool isInvalid = JsonUtility.IsValidJson("{invalid"); // Returns false

// Parse JSON to dynamic JsonElement for unknown structures
var dynamicJson = JsonUtility.ParseDynamic("{\"status\":\"ok\",\"data\":{\"count\":10}}");
if (dynamicJson.HasValue)
{
    Console.WriteLine($"Status: {dynamicJson.Value.GetProperty("status").GetString()}");
}

// Merge two JSON documents (second overwrites first)
string config1 = "{\"apiVersion\":\"1.0\",\"timeout\":30}";
string config2 = "{\"timeout\":60,\"retries\":3}";
string mergedConfig = JsonUtility.MergeJson(config1, config2);
// Returns: {"apiVersion":"1.0","timeout":60,"retries":3}
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

## AnalyticsService

The `AnalyticsService` class provides advanced analytics and insights about API gateway operations. It collects and processes metrics on gateway health, performance trends, and route-specific analytics to help monitor system behavior and identify issues. The service aggregates data from metrics tracking and route repositories to generate comprehensive reports on request volumes, error rates, and response times.

The service offers methods for generating health reports, analyzing performance trends, and identifying problematic routes based on error rates or response times.

Example usage:

```csharp
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<MetricsService>();
services.AddSingleton<GatewayRouteRepository>();
services.AddSingleton<AnalyticsService>();

var serviceProvider = services.BuildServiceProvider();
var analyticsService = serviceProvider.GetRequiredService<AnalyticsService>();

// Get comprehensive gateway health report
var healthReport = await analyticsService.GetHealthReportAsync();
Console.WriteLine($"Gateway Health: {healthReport.HealthStatus}");
Console.WriteLine($"Success Rate: {healthReport.SuccessRate:F2}%");
Console.WriteLine($"Error Rate: {healthReport.ErrorRate:F2}%");
Console.WriteLine($"Total Requests: {healthReport.TotalRequests}");
Console.WriteLine($"Average Response Time: {healthReport.AverageResponseTimeMs:F2}ms");

// Get performance trends over the last 30 minutes
var trend = await analyticsService.GetPerformanceTrendAsync(lastNMinutes: 30);
Console.WriteLine($"Performance Trend: {trend.Period}");
Console.WriteLine($"Collection Time: {trend.CollectionTime:O}");
foreach (var sample in trend.Samples)
{
    Console.WriteLine($"  - Timestamp: {sample.Timestamp:O}");
    Console.WriteLine($"    Avg Response Time: {sample.AverageResponseTimeMs:F2}ms");
    Console.WriteLine($"    Requests Per Second: {sample.RequestsPerSecond:F2}");
}

// Get top 5 routes by request volume
var topRoutes = await analyticsService.GetTopRoutesByVolumeAsync(limit: 5);
Console.WriteLine("Top Routes by Volume:");
foreach (var route in topRoutes)
{
    Console.WriteLine($"  {route.RouteName}:");
    Console.WriteLine($"    Total Requests: {route.TotalRequests}");
    Console.WriteLine($"    Success Rate: {route.SuccessRate:F2}%");
    Console.WriteLine($"    Avg Response Time: {route.AverageResponseTimeMs:F2}ms");
}

// Get routes with highest error rates
var problematicRoutes = await analyticsService.GetProblematicRoutesAsync(limit: 10);
Console.WriteLine("Problematic Routes (Highest Error Rates):");
foreach (var route in problematicRoutes)
{
    Console.WriteLine($"  {route.RouteName}:");
    Console.WriteLine($"    Error Rate: {route.ErrorRate:F2}%");
    Console.WriteLine($"    Total Requests: {route.TotalRequests}");
    Console.WriteLine($"    Failed Requests: {route.FailedRequests}");
}

// Get slowest routes by response time
var slowestRoutes = await analyticsService.GetSlowestRoutesAsync(limit: 5);
Console.WriteLine("Slowest Routes:");
foreach (var route in slowestRoutes)
{
    Console.WriteLine($"  {route.RouteName}:");
    Console.WriteLine($"    Avg Response Time: {route.AverageResponseTimeMs:F2}ms");
    Console.WriteLine($"    Total Requests: {route.TotalRequests}");
}
```

## MetricsService

The `MetricsService` class collects and reports comprehensive metrics about API gateway operations, including request volumes, response times, success/failure rates, and per-route statistics. It tracks total requests, status code distribution, and maintains performance metrics for individual routes to help monitor system health and identify performance bottlenecks.

The service provides both synchronous and asynchronous methods for recording metrics and retrieving aggregated statistics, making it suitable for real-time monitoring and historical analysis.

Example usage:

```csharp
using DotNetApiGateway.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddSingleton<MetricsService>();

var serviceProvider = services.BuildServiceProvider();
var metricsService = serviceProvider.GetRequiredService<MetricsService>();

// Record a completed request's metrics
metricsService.RecordRequest(
    routeId: "user-api-route",
    statusCode: 200,
    duration: TimeSpan.FromMilliseconds(150)
);

// Record an async request (useful when route ID isn't available yet)
await metricsService.RecordRequestAsync(
    method: "GET",
    path: "/api/users/123",
    statusCode: 200,
    responseTimeMs: 150,
    timestamp: DateTime.UtcNow
);

// Get comprehensive gateway metrics
var gatewayMetrics = metricsService.GetMetrics();
Console.WriteLine($"Total Requests: {gatewayMetrics.TotalRequests}");
Console.WriteLine($"Success Rate: {gatewayMetrics.SuccessRate:F2}%");
Console.WriteLine($"Average Response Time: {gatewayMetrics.AverageResponseTimeMs:F2}ms");
Console.WriteLine($"Uptime: {gatewayMetrics.Uptime.TotalHours:F2} hours");
Console.WriteLine($"Requests Per Second: {gatewayMetrics.GetRequestsPerSecond():F2}");

// Get status code distribution
foreach (var kvp in gatewayMetrics.StatusCodeDistribution)
{
    Console.WriteLine($"Status {kvp.Key}: {kvp.Value} requests");
}

// Get metrics for a specific route
var routeMetrics = metricsService.GetRouteMetrics("user-api-route");
if (routeMetrics != null)
{
    Console.WriteLine($"Route: {routeMetrics.RouteId}");
    Console.WriteLine($"Total Requests: {routeMetrics.TotalRequests}");
    Console.WriteLine($"Success Rate: {(double)routeMetrics.SuccessfulRequests / routeMetrics.TotalRequests * 100:F2}%");
    Console.WriteLine($"Avg Response Time: {routeMetrics.AverageResponseTimeMs:F2}ms");
    Console.WriteLine($"Min/Max Response Time: {routeMetrics.MinResponseTimeMs:F2}/{routeMetrics.MaxResponseTimeMs:F2}ms");
    Console.WriteLine($"Requests Per Minute: {routeMetrics.GetRequestsPerMinute():F2}");
}

// Get aggregated counts
var totalRequests = await metricsService.GetTotalRequestCountAsync();
var successfulRequests = await metricsService.GetSuccessfulRequestCountAsync();
var failedRequests = await metricsService.GetFailedRequestCountAsync();
var avgResponseTime = await metricsService.GetAverageResponseTimeAsync();

// Reset all metrics (useful for testing or daily rollover)
metricsService.Reset();
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

## ClientIdentity

The `ClientIdentity` class represents authenticated client information used throughout the API gateway's request processing pipeline. It holds identity data extracted from authentication tokens (such as JWT claims) and provides helper methods for checking scopes, roles, and claim values. This type is used by `RequestContext` to maintain client context during request processing.

Example usage:

```csharp
using DotNetApiGateway.Models;
using System;
using System.Collections.Generic;

// Create a client identity from authentication token claims
var clientIdentity = new ClientIdentity
{
    Id = "user-456",
    Subject = "auth0|1234567890",
    Name = "John Doe",
    Email = "john.doe@example.com",
    Scopes = ["read:users", "write:users", "profile"],
    Roles = ["user", "premium"],
    Claims = new Dictionary<string, object>
    {
        ["tenant_id"] = "acme-corp",
        ["department"] = "engineering",
        ["preferred_language"] = "en-US"
    },
    ExpiresAt = DateTime.UtcNow.AddHours(1),
    IssuedAt = DateTime.UtcNow.AddMinutes(-5)
};

// Check if client has specific scopes
bool hasReadScope = clientIdentity.HasScope("read:users"); // Returns true
bool hasWriteScope = clientIdentity.HasScope("write:orders"); // Returns false

// Check if client has specific roles
bool isAdmin = clientIdentity.HasRole("admin"); // Returns false
bool isPremium = clientIdentity.HasRole("premium"); // Returns true

// Check multiple scopes at once
bool hasRequiredScopes = clientIdentity.HasAnyScopeOf(["read:users", "read:products"]); // Returns true
bool hasAllRequiredScopes = clientIdentity.HasAllScopesOf(["read:users", "write:users"]); // Returns true

// Check multiple roles at once
bool hasRequiredRoles = clientIdentity.HasAnyRoleOf(["admin", "moderator"]); // Returns false
bool hasAllRequiredRoles = clientIdentity.HasAllRolesOf(["user", "premium"]); // Returns true

// Access typed claims
string? tenantId = clientIdentity.GetClaim<string>("tenant_id"); // Returns "acme-corp"
int? departmentId = clientIdentity.GetClaim<int>("department_id"); // Returns null

// Check token expiration
bool isExpired = clientIdentity.IsExpired; // Returns false
if (clientIdentity.IsExpired)
{
    Console.WriteLine("Token has expired");
}

// Access public properties
Console.WriteLine($"Client ID: {clientIdentity.Id}");
Console.WriteLine($"Client Name: {clientIdentity.Name}");
Console.WriteLine($"Client Email: {clientIdentity.Email}");
Console.WriteLine($"Available Scopes: {string.Join(", ", clientIdentity.Scopes)}");
Console.WriteLine($"Available Roles: {string.Join(", ", clientIdentity.Roles)}");
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

## ValidationUtility

The `ValidationUtility` class provides utility methods for validating common data formats and types in the API gateway. It offers validation for email addresses, URLs, IP addresses, UUIDs, strings, collections, and type checking operations. These utilities ensure consistent validation logic across the gateway's request processing pipeline.

Example usage:

```csharp
using DotNetApiGateway.Utilities;
using System;
using System.Collections.Generic;

// Validate email address format
bool isValidEmail = ValidationUtility.IsValidEmail("user@example.com");
Console.WriteLine($"Is valid email: {isValidEmail}"); // Output: Is valid email: True

// Validate URL format
bool isValidUrl = ValidationUtility.IsValidUrl("https://api.example.com/users/123");
Console.WriteLine($"Is valid URL: {isValidUrl}"); // Output: Is valid URL: True

// Validate IPv4 address format
bool isValidIp = ValidationUtility.IsValidIpAddress("192.168.1.100");
Console.WriteLine($"Is valid IP address: {isValidIp}"); // Output: Is valid IP address: True

// Validate UUID/GUID format
bool isValidUuid = ValidationUtility.IsValidUuid("550e8400-e29b-41d4-a716-446655440000");
Console.WriteLine($"Is valid UUID: {isValidUuid}"); // Output: Is valid UUID: True

// Check if string is null or empty
bool isNullOrEmpty = ValidationUtility.IsNullOrEmpty("   ");
Console.WriteLine($"Is null or empty: {isNullOrEmpty}"); // Output: Is null or empty: True

// Validate string length
bool isValidLength = ValidationUtility.IsValidLength("hello", 3, 10);
Console.WriteLine($"Is valid length: {isValidLength}"); // Output: Is valid length: True

// Check if string is alphanumeric
bool isAlphanumeric = ValidationUtility.IsAlphanumeric("abc123");
Console.WriteLine($"Is alphanumeric: {isAlphanumeric}"); // Output: Is alphanumeric: True

// Check if string contains only ASCII characters
bool isAsciiOnly = ValidationUtility.IsAsciiOnly("Hello World!");
Console.WriteLine($"Is ASCII only: {isAsciiOnly}"); // Output: Is ASCII only: True

// Validate port number
bool isValidPort = ValidationUtility.IsValidPort(8080);
Console.WriteLine($"Is valid port: {isValidPort}"); // Output: Is valid port: True

// Validate HTTP method
bool isValidHttpMethod = ValidationUtility.IsValidHttpMethod("GET");
Console.WriteLine($"Is valid HTTP method: {isValidHttpMethod}"); // Output: Is valid HTTP method: True

// Validate HTTP status code
bool isValidStatusCode = ValidationUtility.IsValidHttpStatusCode(200);
Console.WriteLine($"Is valid HTTP status code: {isValidStatusCode}"); // Output: Is valid HTTP status code: True

// Check if object is null
bool isNull = ValidationUtility.IsNull(null);
Console.WriteLine($"Is null: {isNull}"); // Output: Is null: True

// Validate object type
bool isValidType = ValidationUtility.IsValidType<string>("hello");
Console.WriteLine($"Is valid type: {isValidType}"); // Output: Is valid type: True

// Check if collection is null or empty
bool isCollectionEmpty = ValidationUtility.IsNullOrEmpty(new List<string>());
Console.WriteLine($"Is collection empty: {isCollectionEmpty}"); // Output: Is collection empty: True

// Validate dictionary contains required keys
bool hasRequiredKeys = ValidationUtility.HasRequiredKeys(
    new Dictionary<string, string> { ["id"] = "123", ["name"] = "John" },
    "id", "name"
);
Console.WriteLine($"Has required keys: {hasRequiredKeys}"); // Output: Has required keys: True
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

## ConfigurationValidator

The `ConfigurationValidator` class validates configuration settings for the API gateway.

Example usage:

```csharp
using DotNetApiGateway.Configuration;
using DotNetApiGateway.Models;

// Create a configuration validator
var validator = new ConfigurationValidator();

// Validate gateway configuration
var gatewayResult = validator.ValidateGatewayConfig(new GatewayRoute
{
    Name = "user-api",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET", "PUT", "DELETE"]
});

// Validate a route configuration
var routeResult = validator.ValidateRoute(new GatewayRoute
{
    Name = "user-api",
    PathPattern = "/api/users/{id}",
    AllowedMethods = ["GET"]
});

// Validate a route target
var targetResult = validator.ValidateRouteTarget(new RouteTarget
{
    Name = "user-service-primary",
    BaseUrl = "https://user-service.internal:8080"
});

// Validate rate limit policy
var rateLimitResult = validator.ValidateRateLimitPolicy(new RateLimitPolicy
{
    Enabled = true,
    RequestsPerMinute = 1000
});

// Validate circuit breaker policy
var circuitBreakerResult = validator.ValidateCircuitBreakerPolicy(new CircuitBreakerPolicy
{
    Enabled = true,
    FailureThreshold = 5,
    SuccessThreshold = 3
});

// Validate cache policy
var cacheResult = validator.ValidateCachePolicy(new CachePolicy
{
    Enabled = true,
    DurationSeconds = 300
});

// Access validation errors
if (!gatewayResult.IsValid)
{
    foreach (var error in validator.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}

// Get error summary
string errorSummary = validator.GetErrorSummary();
if (!string.IsNullOrEmpty(errorSummary))
{
    Console.WriteLine(errorSummary);
}
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

## RateLimitPolicy

The `RateLimitPolicy` class defines rate limiting configuration for API gateway routes. It configures request limits per time window, burst handling, storage backends, and authentication bypass rules. The policy supports multiple rate limiting strategies (sliding window, fixed window, and token bucket), Redis-backed distributed rate limiting, and conditional bypass for authenticated users.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create a rate limit policy for public API endpoints
var publicApiPolicy = new RateLimitPolicy
{
    Id = "public-api-limit",
    Enabled = true,
    RequestsPerMinute = 100,
    RequestsPerHour = 1000,
    Strategy = RateLimitStrategy.SlidingWindow,
    KeyGenerator = "client-ip",
    BypassForAuthenticatedUsers = false,
    BurstSize = 20,
    StorageType = RateLimitStorageType.Memory,
    RedisConnectionString = null // Use memory storage
};

// Create a rate limit policy for authenticated API endpoints with higher limits
var authApiPolicy = new RateLimitPolicy
{
    Id = "authenticated-api-limit",
    Enabled = true,
    RequestsPerMinute = 1000,
    RequestsPerHour = 10000,
    Strategy = RateLimitStrategy.TokenBucket,
    KeyGenerator = "user-id",
    BypassForAuthenticatedUsers = true,
    BurstSize = 100,
    StorageType = RateLimitStorageType.Redis,
    RedisConnectionString = "localhost:6379"
};

// Create a rate limit policy for bursty workloads
var burstyPolicy = new RateLimitPolicy
{
    Id = "bursty-service-limit",
    Enabled = true,
    RequestsPerMinute = 500,
    RequestsPerHour = 5000,
    Strategy = RateLimitStrategy.TokenBucket,
    KeyGenerator = "service-name",
    BypassForAuthenticatedUsers = false,
    BurstSize = 200,
    StorageType = RateLimitStorageType.Memory
};

// Validate the policy configuration
publicApiPolicy.Validate();
authApiPolicy.Validate();

// Get limit information for the current window
int limitPerMinute = publicApiPolicy.GetLimitForWindow(RateLimitWindow.Minute);
int limitPerHour = publicApiPolicy.GetLimitForWindow(RateLimitWindow.Hour);

// Check if the policy is enabled
bool isEnabled = publicApiPolicy.IsEnabled();
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

## AggregatedRequest

The `AggregatedRequest` type encapsulates HTTP requests with properties for request identification, routing, headers, query parameters, body content, timeout configuration, and optional handling. It is used throughout the API gateway for request aggregation scenarios where multiple requests need to be combined or processed together.

Example usage:

```csharp
using DotNetApiGateway.Models;

// Create an aggregated request for processing
var request = new AggregatedRequest
{
    Id = "req-12345",
    Alias = "user-profile-request",
    Path = "/api/users/profile",
    Method = "GET",
    Headers = new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["Authorization"] = "Bearer token123"
    },
    QueryParameters = new Dictionary<string, string>
    {
        ["fields"] = "name,email,address",
        ["include"] = "orders"
    },
    Body = "{\"userId\": 123}",
    TimeoutSeconds = 30,
    Optional = false
};

// Validate the request configuration
request.Validate();

// Access request properties
Console.WriteLine($"Request ID: {request.Id}");
Console.WriteLine($"Request Path: {request.Path}");
Console.WriteLine($"Method: {request.Method}");
Console.WriteLine($"Timeout: {request.TimeoutSeconds} seconds");
```

## ConditionalAggregationTarget

The `ConditionalAggregationTarget` class represents a target for request aggregation that can be conditionally selected based on JSONPath expressions or other conditions. It is used within `AggregationPolicy` configurations to implement canary deployments, blue-green deployments, A/B testing, or routing requests to different backend services based on request characteristics.

Each conditional target defines an upstream URL, HTTP method, optional headers and body content, timeout configuration, and whether the target is optional (failure won't fail the entire aggregation).

Example usage:

```csharp
using DotNetApiGateway.Models;
using DotNetApiGateway.Constants;

// Create a conditional aggregation target for production traffic
var productionTarget = new ConditionalAggregationTarget
{
  Id = "production-backend",
  UpstreamUrl = "https://api.example.com/production",
  Method = HttpMethod.POST,
  Headers = new Dictionary<string, string>
  {
    ["X-Environment"] = "production",
    ["X-Version"] = "v2"
  },
  Body = "{\"source\": \"production\"}",
  TimeoutSeconds = 45,
  Optional = false
};

// Create a conditional aggregation target for canary deployment
var canaryTarget = new ConditionalAggregationTarget
{
  Id = "canary-backend",
  UpstreamUrl = "https://canary.api.example.com",
  Method = HttpMethod.POST,
  JsonPathCondition = "$.headers['X-Canary-User']", // Only selected users go to canary
  Headers = new Dictionary<string, string>
  {
    ["X-Environment"] = "canary",
    ["X-Version"] = "v2"
  },
  TimeoutSeconds = 30,
  Optional = true // Failure won't fail the aggregation
};

// Create a conditional aggregation target with JSONPath condition for A/B testing
var abTarget = new ConditionalAggregationTarget
{
  Id = "ab-test-group-b",
  UpstreamUrl = "https://ab-test.api.example.com/group-b",
  Method = HttpMethod.GET,
  JsonPathCondition = "$.userId % 2 == 0", // Route even user IDs to group B
  TimeoutSeconds = 25,
  Optional = false
};

// Validate the target configuration
productionTarget.Validate();
canaryTarget.Validate();
abTarget.Validate();

// Access target properties
Console.WriteLine($"Target ID: {productionTarget.Id}");
Console.WriteLine($"Upstream URL: {productionTarget.UpstreamUrl}");
Console.WriteLine($"HTTP Method: {productionTarget.Method}");
Console.WriteLine($"Timeout: {productionTarget.TimeoutSeconds} seconds");
Console.WriteLine($"Is Optional: {productionTarget.Optional}");
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

## RequestTransformationController

The `RequestTransformationController` provides endpoints for developers to test and validate header, body, and query parameter transformation rules before applying them to live API routes. It allows simulating how requests will be transformed by providing sample input data alongside the intended transformation configuration, aiding in the debugging and validation of transformation pipelines.

Example usage:

```csharp
using DotNetApiGateway.Models;

// This illustrates how the request models used by RequestTransformationController 
// are constructed for testing transformation logic.

// Example for HeaderTransformationRequest
var headerRequest = new HeaderTransformationRequest
{
    InputHeaders = new Dictionary<string, string> { { "X-API-Key", "secret-123" } },
    HeadersToAdd = new Dictionary<string, string> { { "X-Custom-Header", "value" } },
    HeadersToRemove = new List<string> { "X-Unwanted-Header" }
};

// Example for BodyTransformationRequest
var bodyRequest = new BodyTransformationRequest
{
    InputBody = "{\"key\": \"value\"}",
    TransformationRules = new Dictionary<string, object> { { "key", "newValue" } }
};

// Example for QueryParamTransformationRequest
var queryRequest = new QueryParamTransformationRequest
{
    InputParams = new Dictionary<string, string> { { "userId", "123" } },
    ParamMapping = new Dictionary<string, string> { { "userId", "id" } },
    ParamsToRemove = new List<string> { "unusedParam" }
};
```
