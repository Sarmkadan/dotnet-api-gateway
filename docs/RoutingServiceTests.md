# RoutingServiceTests

Unit tests for the `RoutingService` class, validating route resolution, target selection strategies, URL construction, header transformations, and CRUD operations for routes in the API gateway.

## API

### `FindRouteAsync_ExistingRoute_ReturnsRoute`
Validates that `RoutingService.FindRouteAsync` returns the correct route when a matching route exists. No parameters or exceptions expected.

### `FindRouteAsync_NonExistentRoute_ThrowsRouteNotFoundException`
Ensures `RoutingService.FindRouteAsync` throws a `RouteNotFoundException` when no route matches the request.

### `SelectTarget_RoundRobin_DistributesEvenly`
Tests that the round-robin target selection strategy distributes requests evenly across available targets.

### `SelectTarget_IpHash_SameIpSameTarget`
Confirms that the IP-hash target selection strategy consistently routes requests from the same client IP to the same target.

### `SelectTarget_IpHash_NoIpFallsBackToRoundRobin`
Verifies that the IP-hash strategy falls back to round-robin when no client IP is available.

### `SelectTarget_LeastConnections_SelectsByWeight`
Checks that the least-connections strategy selects targets based on their current connection weights.

### `SelectTarget_NoHealthyTargets_ThrowsGatewayException`
Ensures `RoutingService.SelectTarget` throws a `GatewayException` when no healthy targets are available.

### `SelectTarget_FiltersOnlyHealthyTargets`
Validates that the target selection process only considers healthy targets.

### `BuildForwardUrl_CombinesTargetAndPath`
Tests that `RoutingService.BuildForwardUrl` correctly combines the selected target’s base URL with the route’s path.

### `ApplyHeaderTransforms_AddsTransformedHeaders`
Confirms that header transformations add new headers as specified by the route’s transformation rules.

### `ApplyHeaderTransforms_OverridesExistingHeaders`
Ensures that header transformations override existing headers when configured to do so.

### `GetAllActiveRoutesAsync_ReturnsActiveRoutes`
Validates that `RoutingService.GetAllActiveRoutesAsync` returns only active (non-disabled) routes.

### `CreateRouteAsync_ValidRoute_AddsRoute`
Tests that a valid route is successfully added via `RoutingService.CreateRouteAsync`.

### `CreateRouteAsync_InvalidRoute_ThrowsArgumentException`
Ensures `RoutingService.CreateRouteAsync` throws an `ArgumentException` when an invalid route is provided.

### `UpdateRouteAsync_ValidRoute_UpdatesRoute`
Confirms that a valid route is correctly updated via `RoutingService.UpdateRouteAsync`.

### `DeleteRouteAsync_ExistingRoute_DeletesRoute`
Validates that an existing route is successfully deleted via `RoutingService.DeleteRouteAsync`.

### `DeleteRouteAsync_NonExistentRoute_ReturnsFalse`
Ensures `RoutingService.DeleteRouteAsync` returns `false` when attempting to delete a non-existent route.

## Usage

### Example 1: Testing Route Resolution
