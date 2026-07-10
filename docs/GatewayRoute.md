# GatewayRoute

Represents a configurable route within a .NET API Gateway, defining how incoming requests are matched, processed, and forwarded to backend services. It encapsulates routing rules, policies, transformations, and metadata necessary to manage traffic efficiently and securely.

## API

### `Id`
A unique identifier for the route. Used internally to reference the route within the gateway configuration.

### `Name`
A human-readable name for the route. Primarily used for logging, monitoring, and administrative purposes.

### `PathPattern`
A string representing the route pattern to match incoming requests (e.g., `/api/v1/users/{id}`). Supports standard route parameter syntax and is used to determine if a request should be handled by this route.

### `AllowedMethods`
An array of HTTP methods (e.g., `["GET", "POST"]`) that this route will accept. Requests with methods not in this list are rejected.

### `Targets`
An array of `RouteTarget` objects specifying the backend services to which matching requests should be forwarded. Each target includes a URI and optional weight for load balancing.

### `RateLimitPolicy`
An optional `RateLimitPolicy` object defining rate limiting rules for requests matching this route. If `null`, no rate limiting is applied.

### `CircuitBreakerPolicy`
An optional `CircuitBreakerPolicy` object defining circuit breaker rules for requests matching this route. If `null`, no circuit breaking is applied.

### `CachePolicy`
An optional `CachePolicy` object defining caching behavior for responses from this route. If `null`, no caching is applied.

### `AuthenticationPolicy`
An optional `AuthenticationPolicy` object defining authentication requirements for requests matching this route. If `null`, no authentication is enforced.

### `RequestCoalescingPolicy`
An optional `RequestCoalescingPolicy` object defining how duplicate concurrent requests should be coalesced. If `null`, no coalescing is applied.

### `AggregationPolicy`
An optional `AggregationPolicy` object defining how multiple backend responses should be aggregated into a single response. If `null`, no aggregation is applied.

### `VersioningPolicy`
An optional `ApiVersioningPolicy` object defining API versioning rules for this route. If `null`, no versioning is enforced.

### `TransformationRules`
A list of `TransformationRule` objects defining request and response transformations to apply before forwarding or after receiving responses.

### `IsActive`
A boolean indicating whether the route is currently active and should process incoming requests. Inactive routes are ignored by the gateway.

### `TimeoutSeconds`
An integer specifying the maximum number of seconds to wait for a response from backend targets before timing out the request.

### `CustomHeaders`
A dictionary of key-value pairs representing HTTP headers to add to responses forwarded by this route. Useful for injecting metadata or security headers.

### `CreatedAt`
A `DateTime` indicating when the route was created. Immutable after creation.

### `ModifiedAt`
An optional `DateTime` indicating when the route was last modified. `null` if the route has never been modified.

### `Validate()`
Validates the route configuration. Throws an exception if required fields are missing, invalid, or inconsistent (e.g., `PathPattern` is malformed, `Targets` is empty, or `TimeoutSeconds` is negative).

### `MatchesPath(string path)`
Determines whether the given request path matches the route's `PathPattern`.

- **Parameters**:
  - `path`: The request path to match (e.g., `/api/v1/users/123`).
- **Return Value**: `true` if the path matches the pattern; otherwise, `false`.

## Usage

### Example 1: Basic Route Configuration
