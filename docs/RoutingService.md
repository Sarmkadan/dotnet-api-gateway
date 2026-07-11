# RoutingService

Central service in the `dotnet-api-gateway` project responsible for resolving incoming requests to configured gateway routes, selecting target endpoints, and transforming request headers before forwarding traffic. Integrates with the gateway's route repository to maintain and manipulate the active routing configuration.

## API

### `RoutingService`

Public constructor for the `RoutingService` class. Initializes a new instance of the routing service with required dependencies for route resolution, target selection, and route management.

### `async Task<GatewayRoute?> FindRouteAsync(string path, string method)`

Locates the first matching `GatewayRoute` for the given HTTP path and method.

- **Parameters**
  - `path` – The normalized request path to match against configured routes.
  - `method` – The HTTP method (e.g., `GET`, `POST`) to match.
- **Returns**
  - A `Task` resolving to the matched `GatewayRoute`, or `null` if no route matches.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` or `method` is `null`.
  - Throws `InvalidOperationException` if the route repository is unavailable.

### `RouteTarget SelectTarget(GatewayRoute route, HttpContext context)`

Selects a target endpoint from the provided route based on configured selection criteria (e.g., load balancing, priority).

- **Parameters**
  - `route` – The `GatewayRoute` defining available targets and selection policy.
  - `context` – The current `HttpContext` for evaluating dynamic selection criteria.
- **Returns**
  - A `RouteTarget` representing the chosen downstream endpoint.
- **Exceptions**
  - Throws `ArgumentNullException` if `route` or `context` is `null`.
  - Throws `InvalidOperationException` if no valid target is available.

### `string BuildForwardUrl(RouteTarget target, string path)`

Constructs the absolute URL to forward the request to the selected target using the target's base address and the original path.

- **Parameters**
  - `target` – The selected `RouteTarget` containing the base address.
  - `path` – The original request path to append to the target base.
- **Returns**
  - The fully qualified URL as a string.
- **Exceptions**
  - Throws `ArgumentNullException` if `target` or `path` is `null`.
  - Throws `FormatException` if the target base address is malformed.

### `Dictionary<string, string> ApplyHeaderTransforms(GatewayRoute route, HttpContext context)`

Applies header transformations defined in the route configuration to the incoming request headers.

- **Parameters**
  - `route` – The `GatewayRoute` containing header transformation rules.
  - `context` – The current `HttpContext` whose request headers are to be transformed.
- **Returns**
  - A `Dictionary<string, string>` of transformed header names and values.
- **Exceptions**
  - Throws `ArgumentNullException` if `route` or `context` is `null`.

### `async Task<IEnumerable<GatewayRoute>> GetAllActiveRoutesAsync()`

Retrieves all currently active routes from the route repository.

- **Returns**
  - A `Task` resolving to an `IEnumerable<GatewayRoute>` of active routes.
- **Exceptions**
  - Throws `InvalidOperationException` if the route repository is unavailable or returns invalid data.

### `async Task<GatewayRoute> CreateRouteAsync(GatewayRoute route)`

Adds a new route to the route repository and returns the created route.

- **Parameters**
  - `route` – The `GatewayRoute` to create.
- **Returns**
  - A `Task` resolving to the created `GatewayRoute`.
- **Exceptions**
  - Throws `ArgumentNullException` if `route` is `null`.
  - Throws `InvalidOperationException` if the route already exists or creation fails.

### `async Task<GatewayRoute> UpdateRouteAsync(GatewayRoute route)`

Updates an existing route in the route repository and returns the updated route.

- **Parameters**
  - `route` – The `GatewayRoute` with updated properties.
- **Returns**
  - A `Task` resolving to the updated `GatewayRoute`.
- **Exceptions**
  - Throws `ArgumentNullException` if `route` is `null`.
  - Throws `InvalidOperationException` if the route does not exist or update fails.

### `async Task<bool> DeleteRouteAsync(string routeId)`

Removes a route from the route repository by its unique identifier.

- **Parameters**
  - `routeId` – The unique identifier of the route to delete.
- **Returns**
  - A `Task<bool>` indicating whether the deletion was successful.
- **Exceptions**
  - Throws `ArgumentNullException` if `routeId` is `null`.
  - Throws `InvalidOperationException` if deletion fails due to repository constraints.

## Usage
