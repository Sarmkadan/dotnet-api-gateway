# CircuitBreakerController

The `CircuitBreakerController` provides a centralized HTTP interface for monitoring and managing the state of circuit breakers within the API Gateway. It exposes endpoints to retrieve the status of individual circuits or aggregate views based on their current state (Open, Half-Open, or Closed) and offers administrative actions to manually reset specific circuits or the entire system, facilitating operational oversight and recovery during transient downstream failures.

## API

### `CircuitBreakerController`
Initializes a new instance of the controller. This constructor typically injects required services for circuit breaker management, though specific dependencies are handled internally by the dependency injection container.

### `GetAllCircuitBreakerStatuses`
Retrieves the current status of all registered circuit breakers in the system.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IActionResult>` containing a collection of status objects for every circuit.
*   **Exceptions**: May throw an exception if the underlying circuit breaker registry is inaccessible or if serialization of the status collection fails.

### `GetCircuitBreakerStatus`
Retrieves the detailed status of a specific circuit breaker identified by its unique name or ID.
*   **Parameters**: Accepts a string identifier (typically via route or query parameter) specifying the target circuit.
*   **Return Value**: Returns a `Task<IActionResult>` containing the status details if found, or a `NotFound` result if the identifier does not match any registered circuit.
*   **Exceptions**: Throws an exception if the input identifier is null or malformed, or if the internal lookup mechanism encounters an error.

### `GetOpenCircuits`
Returns a list of all circuit breakers currently in the `Open` state, indicating that requests to the associated downstream services are currently being blocked.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IActionResult>` containing a filtered list of circuits where the state is `Open`.
*   **Exceptions**: May throw if the state enumeration process fails due to concurrency issues or internal state corruption.

### `GetHalfOpenCircuits`
Returns a list of all circuit breakers currently in the `HalfOpen` state, indicating that the system is probing the downstream service to determine if it has recovered.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IActionResult>` containing a filtered list of circuits where the state is `HalfOpen`.
*   **Exceptions**: May throw if the state enumeration process fails.

### `GetClosedCircuits`
Returns a list of all circuit breakers currently in the `Closed` state, indicating normal operation where requests are flowing to the downstream service.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IActionResult>` containing a filtered list of circuits where the state is `Closed`.
*   **Exceptions**: May throw if the state enumeration process fails.

### `ResetCircuit`
Manually forces a specific circuit breaker to transition to the `Closed` state, effectively clearing any failure counts and re-enabling traffic to the associated endpoint immediately.
*   **Parameters**: Accepts a string identifier specifying the target circuit to reset.
*   **Return Value**: Returns a `Task<IActionResult>` indicating success (typically `Ok` or `NoContent`) or `NotFound` if the circuit does not exist.
*   **Exceptions**: Throws an exception if the identifier is invalid, or if the reset operation conflicts with an ongoing state transition.

### `ResetAllCircuits`
Manually forces all registered circuit breakers in the system to transition to the `Closed` state.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IActionResult>` indicating the completion of the bulk reset operation.
*   **Exceptions**: May throw if the bulk operation fails partially or completely due to internal locking mechanisms or registry errors.

## Usage

### Example 1: Monitoring Open Circuits
The following example demonstrates how to query the controller to identify which downstream services are currently unavailable due to open circuits.

```csharp
public async Task CheckSystemHealth(HttpClient httpClient)
{
    var response = await httpClient.GetAsync("/api/circuit-breaker/open");
    
    if (response.IsSuccessStatusCode)
    {
        var openCircuits = await response.Content.ReadFromJsonAsync<List<CircuitStatus>>();
        
        if (openCircuits != null && openCircuits.Any())
        {
            Console.WriteLine($"Alert: {openCircuits.Count} circuits are currently open.");
            foreach (var circuit in openCircuits)
            {
                Console.WriteLine($"- {circuit.Name}: Last failure at {circuit.LastFailureTime}");
            }
        }
    }
}
```

### Example 2: Resetting a Specific Stuck Circuit
This example shows how to manually reset a specific circuit breaker named "PaymentService" after verifying that the downstream issue has been resolved.

```csharp
public async Task RecoverPaymentService(HttpClient httpClient)
{
    var circuitName = "PaymentService";
    var response = await httpClient.PostAsync($"/api/circuit-breaker/reset/{circuitName}", null);

    if (response.StatusCode == System.Net.HttpStatusCode.OK)
    {
        Console.WriteLine($"Circuit '{circuitName}' has been successfully reset to Closed state.");
    }
    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        Console.WriteLine($"Circuit '{circuitName}' not found.");
    }
    else
    {
        Console.WriteLine($"Failed to reset circuit. Status: {response.StatusCode}");
    }
}
```

## Notes

*   **Thread Safety**: As the controller methods are `async` and interact with shared state (the circuit breaker registry), the underlying implementation must ensure thread-safe access to circuit states. Concurrent calls to `ResetAllCircuits` while individual requests are evaluating state may result in transient inconsistencies in the returned data, though the state transitions themselves should remain atomic.
*   **Race Conditions**: When calling `ResetCircuit` on a specific circuit, be aware that if the circuit is naturally transitioning from `HalfOpen` to `Closed` or `Open` at the exact moment of the request, the manual reset might be overwritten by the automatic state machine logic immediately afterward.
*   **Performance**: The `GetAllCircuitBreakerStatuses` method iterates over the entire registry. In systems with a very large number of dynamically created circuits, this operation may incur higher latency compared to the filtered endpoints (`GetOpenCircuits`, etc.).
*   **Existence Checks**: Methods targeting specific circuits (`GetCircuitBreakerStatus`, `ResetCircuit`) will return HTTP 404 if the provided identifier does not correspond to an initialized circuit breaker. Ensure the circuit has been triggered at least once or registered explicitly before attempting to query or reset it.
