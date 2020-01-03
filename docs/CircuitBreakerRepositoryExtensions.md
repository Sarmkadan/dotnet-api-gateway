# CircuitBreakerRepositoryExtensions
The `CircuitBreakerRepositoryExtensions` class provides a set of extension methods for managing circuit breakers in a repository. These methods enable retrieval, update, and manipulation of circuit breaker statuses, allowing for efficient management of circuit breakers in a system.

## API
* `GetByServiceNameOrDefaultAsync`: Retrieves a `CircuitBreakerStatus` by service name, returning the default value if no matching circuit breaker is found. Parameters: service name. Return value: `CircuitBreakerStatus?`. Throws: exceptions related to repository access or data retrieval.
* `GetByStateAsync`: Retrieves a collection of `CircuitBreakerStatus` objects based on their state. Parameters: state. Return value: `IEnumerable<CircuitBreakerStatus>`. Throws: exceptions related to repository access or data retrieval.
* `GetOpenCircuitsAsync`: Retrieves a collection of `CircuitBreakerStatus` objects that are currently open. Parameters: none. Return value: `IEnumerable<CircuitBreakerStatus>`. Throws: exceptions related to repository access or data retrieval.
* `ExistsByIdAsync`: Checks if a circuit breaker with the specified ID exists. Parameters: ID. Return value: `bool`. Throws: exceptions related to repository access or data retrieval.
* `UpdateBatchAsync`: Updates a batch of circuit breakers in the repository. Parameters: batch of circuit breakers. Return value: `Task`. Throws: exceptions related to repository access or data update.
* `ToDictionaryByServiceNameAsync`: Retrieves a dictionary of `CircuitBreakerStatus` objects keyed by service name. Parameters: none. Return value: `Dictionary<string, CircuitBreakerStatus>`. Throws: exceptions related to repository access or data retrieval.
* `ResetAllToClosedAsync`: Resets all circuit breakers in the repository to a closed state. Parameters: none. Return value: `Task`. Throws: exceptions related to repository access or data update.
* `GetByStatesAsync`: Retrieves a collection of `CircuitBreakerStatus` objects based on their states. Parameters: states. Return value: `IEnumerable<CircuitBreakerStatus>`. Throws: exceptions related to repository access or data retrieval.
* `UpsertAsync`: Upserts a `CircuitBreakerStatus` object in the repository, creating a new one if it does not exist or updating the existing one. Parameters: `CircuitBreakerStatus`. Return value: `CircuitBreakerStatus`. Throws: exceptions related to repository access or data update.

## Usage
```csharp
// Example 1: Retrieving circuit breaker status by service name
var circuitBreakerStatus = await CircuitBreakerRepositoryExtensions.GetByServiceNameOrDefaultAsync("my-service");
if (circuitBreakerStatus.HasValue)
{
    Console.WriteLine($"Circuit breaker status for my-service: {circuitBreakerStatus.Value}");
}
else
{
    Console.WriteLine("No circuit breaker found for my-service");
}

// Example 2: Updating a batch of circuit breakers
var circuitBreakers = new List<CircuitBreakerStatus>
{
    new CircuitBreakerStatus { Id = 1, ServiceName = "my-service-1", State = CircuitBreakerState.Closed },
    new CircuitBreakerStatus { Id = 2, ServiceName = "my-service-2", State = CircuitBreakerState.Open },
};
await CircuitBreakerRepositoryExtensions.UpdateBatchAsync(circuitBreakers);
Console.WriteLine("Batch update complete");
```

## Notes
When using these extension methods, consider the following edge cases:
* If multiple circuit breakers have the same service name, `GetByServiceNameOrDefaultAsync` will return the first one it encounters.
* `UpdateBatchAsync` will update all circuit breakers in the batch, even if some of them do not exist in the repository.
* `ResetAllToClosedAsync` will reset all circuit breakers to a closed state, regardless of their current state.
* These methods are designed to be thread-safe, but concurrent access to the repository may still lead to unexpected behavior. It is recommended to use these methods within a transaction or with proper synchronization mechanisms to ensure data consistency.
