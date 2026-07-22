# TestGatewayRouteRepository

Provides a test implementation of `IGatewayRouteRepository` for unit testing routing logic in the API Gateway. This repository simulates route storage and retrieval operations with configurable route definitions and target selection strategies, enabling deterministic testing of route matching, target selection, and URL transformation behaviors.

## API

### `TestGatewayRouteRepository`

Initializes a new instance of the test repository with default empty route and target collections.

### `RoutingServiceTests`

Static class containing all test methods for validating gateway routing behavior using the test repository.

### `async Task FindRouteAsync_ExactPathMatch_ReturnsRoute`

Validates that an exact path match returns the expected route definition.

- **Parameters**: `string path`, `HttpMethod method`
- **Returns**: `Task<RouteDefinition>` containing the matched route
- **Throws**: `RouteNotFoundException` if no exact match exists

### `async Task FindRouteAsync_PrefixTemplateMatch_ReturnsRoute`

Ensures that a route with a prefix template (e.g., `/api/{version}/users`) correctly matches incoming requests with matching prefixes.

- **Parameters**: `string path`, `HttpMethod method`
- **Returns**: `Task<RouteDefinition>` containing the matched route
- **Throws**: `RouteNotFoundException` if no prefix match exists

### `async Task FindRouteAsync_WildcardMatch_ReturnsRoute`

Confirms that wildcard route patterns (e.g., `/health/*`) match any subpath under the specified segment.

- **Parameters**: `string path`, `HttpMethod method`
- **Returns**: `Task<RouteDefinition>` containing the matched route
- **Throws**: `RouteNotFoundException` if no wildcard match exists

### `async Task FindRouteAsync_NoMatch_ThrowsRouteNotFoundException`

Verifies that attempting to find a route with no matching path or method throws the expected exception.

- **Parameters**: `string path`, `HttpMethod method`
- **Throws**: `RouteNotFoundException` with descriptive message

### `async Task FindRouteAsync_MethodNotSupported_ThrowsRouteNotFoundException`

Checks that a route is not matched when the HTTP method does not match any defined route methods.

- **Parameters**: `string path`, `HttpMethod method`
- **Throws**: `RouteNotFoundException` indicating method mismatch

### `async Task SelectTarget_RoundRobinStrategy_DistributesEvenly`

Tests that the round-robin target selection strategy evenly distributes requests across available healthy targets.

- **Parameters**: `RouteDefinition route`, `string clientIp` (optional)
- **Returns**: `Task<Target>` representing the selected target
- **Throws**: `GatewayException` if no healthy targets are available

### `void SelectTarget_NoHealthyTargets_ThrowsGatewayException`

Ensures that selecting a target with no healthy endpoints throws a `GatewayException`.

- **Parameters**: `RouteDefinition route`, `string clientIp`
- **Throws**: `GatewayException` with message indicating no healthy targets

### `void SelectTarget_IpHashStrategy_ReturnsConsistentTargetForSameIp`

Validates that the IP-hash strategy consistently returns the same target for the same client IP.

- **Parameters**: `RouteDefinition route`, `string clientIp`
- **Returns**: `Target` selected via IP-based hashing
- **Throws**: `GatewayException` if no targets are available

### `void SelectTarget_LeastConnectionsStrategy_ReturnsLowestWeightTarget`

Confirms that the least-connections strategy selects the target with the fewest active connections.

- **Parameters**: `RouteDefinition route`
- **Returns**: `Target` with the lowest connection count
- **Throws**: `GatewayException` if no targets are available

### `void BuildForwardUrl_CombinesBaseUrlAndPathCorrectly`

Tests that the base URL and path are correctly combined to form the forward URL.

- **Parameters**: `RouteDefinition route`, `string relativePath`
- **Returns**: `string` representing the full forward URL
- **Throws**: None

### `void ApplyHeaderTransforms_AddsAndOverridesHeaders`

Validates that header transformations correctly add new headers and override existing ones based on route configuration.

- **Parameters**: `IDictionary<string, string> headers`, `RouteDefinition route`
- **Returns**: `IDictionary<string, string>` with transformed headers
- **Throws**: None

### `async Task GetAllActiveRoutesAsync_ReturnsOnlyActiveRoutes`

Ensures that only routes marked as active are returned by the repository.

- **Returns**: `Task<IEnumerable<RouteDefinition>>` containing only active routes
- **Throws**: None

### `async Task CreateRouteAsync_ValidRoute_CallsRepositoryAndLogs`

Tests that creating a valid route persists it and logs the operation.

- **Parameters**: `RouteDefinition route`
- **Returns**: `Task<bool>` indicating success
- **Throws**: None

### `async Task DeleteRouteAsync_ExistingRoute_ReturnsTrueAndLogs`

Confirms that deleting an existing route removes it and returns success.

- **Parameters**: `string routeId`
- **Returns**: `Task<bool>` indicating successful deletion
- **Throws**: None

## Usage

### Example 1: Testing Route Matching
