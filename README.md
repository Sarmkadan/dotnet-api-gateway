// ... (rest of the file remains the same)

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

## ... (rest of the file remains the same)
