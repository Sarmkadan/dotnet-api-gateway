# GatewayRouteRepository
The `GatewayRouteRepository` class is designed to manage gateway routes, providing methods for adding, updating, deleting, and retrieving routes. It serves as a central repository for route data, allowing for efficient and organized route management.

## API
The `GatewayRouteRepository` class provides the following public members:
* `GetByIdAsync`: Retrieves a `GatewayRoute` by its ID. Returns `null` if no route is found.
* `GetAllAsync`: Retrieves all `GatewayRoute` instances.
* `AddAsync`: Adds a new `GatewayRoute` instance. Returns the added route.
* `UpdateAsync`: Updates an existing `GatewayRoute` instance. Returns the updated route.
* `DeleteAsync`: Deletes a `GatewayRoute` instance by its ID. Returns `true` if the deletion was successful, `false` otherwise.
* `ExistsAsync`: Checks if a `GatewayRoute` instance with the given ID exists. Returns `true` if the route exists, `false` otherwise.
* `GetActiveRoutesAsync`: Retrieves all active `GatewayRoute` instances.
* `FindRouteByPathAsync`: Retrieves a `GatewayRoute` instance by its path. Returns `null` if no route is found.
* `GetRoutesByNameAsync`: Retrieves all `GatewayRoute` instances with the given name.
* `ClearAll`: Clears all `GatewayRoute` instances from the repository.
* `GetCountAsync`: Retrieves the total number of `GatewayRoute` instances in the repository.

## Usage
Here are two examples of using the `GatewayRouteRepository` class:
```csharp
// Example 1: Adding and retrieving a route
var repository = new GatewayRouteRepository();
var route = new GatewayRoute { Id = 1, Path = "/example" };
var addedRoute = await repository.AddAsync(route);
var retrievedRoute = await repository.GetByIdAsync(1);
Console.WriteLine(retrievedRoute.Path); // Output: /example

// Example 2: Updating and deleting a route
var repository = new GatewayRouteRepository();
var route = new GatewayRoute { Id = 1, Path = "/example" };
await repository.AddAsync(route);
route.Path = "/updated-example";
var updatedRoute = await repository.UpdateAsync(route);
Console.WriteLine(updatedRoute.Path); // Output: /updated-example
await repository.DeleteAsync(1);
var exists = await repository.ExistsAsync(1);
Console.WriteLine(exists); // Output: False
```

## Notes
The `GatewayRouteRepository` class is designed to be thread-safe, allowing for concurrent access and modification of route data. However, it is essential to note that the `ClearAll` method will remove all routes from the repository, potentially affecting other parts of the application. Additionally, the `GetCountAsync` method may not reflect the exact number of routes if concurrent modifications are occurring. When using the `FindRouteByPathAsync` and `GetRoutesByNameAsync` methods, be aware that they may return `null` or an empty collection if no matching routes are found. It is also important to handle exceptions that may be thrown by the repository methods, such as when attempting to add or update a route with an invalid ID.
