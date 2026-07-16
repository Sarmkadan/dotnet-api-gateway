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
