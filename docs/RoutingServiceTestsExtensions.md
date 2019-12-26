# RoutingServiceTestsExtensions

`RoutingServiceTestsExtensions` is a static utility class providing factory methods for generating test doubles and mock data used in unit and integration tests for the routing service within the `dotnet-api-gateway` project. These methods simplify the creation of consistent, reusable test objects such as `GatewayRoute` and `RouteTarget` instances, including edge cases like unhealthy targets or routes with header transformations.

## API

### `CreateTestRoute`
