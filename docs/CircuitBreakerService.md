# CircuitBreakerService

The `CircuitBreakerService` provides a centralized implementation of the circuit‑breaker pattern for protecting downstream service calls within the API gateway. It tracks the state of each named circuit, records successes and failures, and exposes queries that allow callers to determine whether an operation may proceed.

## API

### CircuitBreakerService()
Initializes a new instance of the service. Dependencies such as configuration and logging are supplied via dependency injection; the constructor takes no explicit parameters.

### GetOrCreateStatusAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Retrieves the existing `CircuitBreakerStatus` for `circuitId` or creates a new one if none exists.  
**Parameters:**  
- `circuitId`: Identifier of the circuit (must not be null or whitespace).  
- `cancellationToken`: Optional token to observe cancellation requests.  
**Return Value:** A `Task<CircuitBreakerStatus>` that completes with the status object for the specified circuit.  
**Exceptions:**  
- `ArgumentException` if `circuitId` is null, empty, or consists only of whitespace.  
- `OperationCanceledException` if the token is triggered before the operation completes.  
- Any exception thrown by the underlying storage mechanism (e.g., database access) is propagated.

### IsCircuitOpenAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Determines whether the circuit identified by `circuitId` is currently in the open state.  
**Parameters:** Same as above.  
**Return Value:** A `Task<bool>` that yields `true` when the circuit is open (requests should be short‑circuited), otherwise `false`.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### CanAttemptAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Checks if an attempt to invoke the protected operation is allowed based on the circuit’s current state and configured thresholds.  
**Parameters:** Same as above.  
**Return Value:** A `Task<bool>` yielding `true` when the call may proceed, `false` when it should be blocked.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### RecordSuccessAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Notifies the service that a protected operation succeeded, which may transition the circuit toward a closed or half‑open state.  
**Parameters:** Same as above.  
**Return Value:** A `Task` that completes when the success has been recorded.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### RecordFailureAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Notifies the service that a protected operation failed, incrementing failure counters and potentially opening the circuit.  
**Parameters:** Same as above.  
**Return Value:** A `Task` that completes when the failure has been recorded.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### GetOpenCircuitsAsync(CancellationToken cancellationToken = default)
**Purpose:** Retrieves the status of all circuits that are currently open.  
**Parameters:** Optional cancellation token.  
**Return Value:** A `Task<IEnumerable<CircuitBreakerStatus>>` containing status objects for each open circuit.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### GetStatusAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Attempts to fetch the current status for a specific circuit without creating it if absent.  
**Parameters:** Same as above.  
**Return Value:** A `Task<CircuitBreakerStatus?>` yielding the status or `null` when no status exists for `circuitId`.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### GetAllStatusesAsync(CancellationToken cancellationToken = default)
**Purpose:** Returns the status of every circuit known to the service.  
**Parameters:** Optional cancellation token.  
**Return Value:** A `Task<IEnumerable<CircuitBreakerStatus>>` containing all circuit statuses.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### ResetCircuitAsync(string circuitId, CancellationToken cancellationToken = default)
**Purpose:** Forces the circuit identified by `circuitId` into the closed state, clearing failure counters.  
**Parameters:** Same as above.  
**Return Value:** A `Task` that completes when the reset operation finishes.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

### ResetAllCircuitsAsync(CancellationToken cancellationToken = default)
**Purpose:** Resets every tracked circuit to the closed state.  
**Parameters:** Optional cancellation token.  
**Return Value:** A `Task` that completes when all circuits have been reset.  
**Exceptions:** Same as `GetOrCreateStatusAsync`.

## Usage

### Example 1: Guarding an HTTP downstream call
```csharp
public class DownstreamClient
{
    private readonly CircuitBreakerService _breaker;
    private readonly HttpClient _http;

    public DownstreamClient(CircuitBreakerService breaker, IHttpClientFactory factory)
    {
        _breaker = breaker;
        _http = factory.CreateClient("downstream");
    }

    public async Task<string> GetDataAsync(string resourceId)
    {
        const string circuit = "DownstreamApi";

        if (!await _breaker.CanAttemptAsync(circuit))
        {
            throw new InvalidOperationException("Circuit is open; request blocked.");
        }

        try
        {
            var response = await _http.GetAsync($"/api/resource/{resourceId}");
            response.EnsureSuccessStatusCode();

            await _breaker.RecordSuccessAsync(circuit);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            await _breaker.RecordFailureAsync(circuit);
            throw;
        }
    }
}
```

### Example 2: Administrative endpoint to view and reset open circuits
```csharp
[ApiController]
[Route("admin/breaker")]
public class BreakerController : ControllerBase
{
    private readonly CircuitBreakerService _breaker;

    public BreakerController(CircuitBreakerService breaker)
    {
        _breaker = breaker;
    }

    [HttpGet("open")]
    public async Task<ActionResult<IEnumerable<CircuitBreakerStatus>>> GetOpen()
    {
        var open = await _breaker.GetOpenCircuitsAsync();
        return Ok(open);
    }

    [HttpPost("reset/{circuitId}")]
    public async Task<IActionResult> Reset(string circuitId)
    {
        await _breaker.ResetCircuitAsync(circuitId);
        return NoContent();
    }

    [HttpPost("reset-all")]
    public async Task<IActionResult> ResetAll()
    {
        await _breaker.ResetAllCircuitsAsync();
        return NoContent();
    }
}
```

## Notes
- The service is designed for concurrent access; all public methods are safe to call from multiple threads without external synchronization.  
- Passing a null, empty, or whitespace‑only `circuitId` to any method that accepts it will result in an `ArgumentException`.  
- Methods that create a status (`GetOrCreateStatusAsync`) will lazily initialize the circuit’s internal counters; therefore the first call may incur a small overhead.  
- If the underlying persistence store (if any) becomes unavailable, the service will propagate the originating exception; callers should consider wrapping calls in a try/catch when operating in degraded environments.  
- Resetting a circuit does not affect the configuration of failure thresholds or timeout values; those remain as originally supplied via the service’s dependencies.  
- The `CircuitBreakerStatus?` return type of `GetStatusAsync` enables callers to distinguish between “circuit exists but is closed” and “no status has ever been recorded for this identifier.”  
- Cancellation tokens are honored wherever I/O or asynchronous work is performed; if a token is triggered, the method throws `OperationCanceledException` and no state change is persisted.
