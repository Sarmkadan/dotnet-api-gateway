# CircuitBreakerStatus

Represents the current status and statistics of a circuit breaker instance within the API gateway. This type tracks operational metrics such as failure/success counts, state transitions, and error history to enable monitoring and adaptive behavior in service-to-service communication.

## API

### Properties

- **`string Id`**  
  Unique identifier for the circuit breaker instance. Used to correlate status with specific service endpoints or configurations.

- **`string ServiceName`**  
  Name of the downstream service associated with this circuit breaker. Facilitates identification in logs and dashboards.

- **`CircuitBreakerState State`**  
  Current operational state of the circuit breaker (e.g., Closed, Open, HalfOpen). Determines request routing behavior.

- **`int FailureCount`**  
  Number of consecutive failures recorded since the last state transition. Resets to zero when state changes or on success.

- **`int SuccessCount`**  
  Number of consecutive successful requests since the last state transition. Resets to zero when state changes or on failure.

- **`DateTime LastStateChangeAt`**  
  Timestamp of the most recent state transition. Used for timeout calculations and diagnostics.

- **`DateTime? LastFailureAt`**  
  Nullable timestamp of the last failed request. Null if no failures have occurred since initialization or reset.

- **`DateTime? LastSuccessAt`**  
  Nullable timestamp of the last successful request. Null if no successes have occurred since initialization or reset.

- **`int TotalFailures`**  
  Cumulative count of all failures since the circuit breaker was created or last reset.

- **`int TotalSuccesses`**  
  Cumulative count of all successful requests since the circuit breaker was created or last reset.

- **`long TotalRequests`**  
  Total number of requests processed (success + failure) since creation or last reset.

- **`string? LastError`**  
  Error message from the most recent failure. Null if the last request succeeded or no failures have occurred.

### Methods

- **`void RecordSuccess()`**  
  Increments `SuccessCount` and `TotalSuccesses`, updates `LastSuccessAt`, and may trigger state transitions based on internal logic. Does not throw exceptions under normal operation.

- **`void RecordFailure(string error)`**  
  Increments `FailureCount` and `TotalFailures`, updates `LastFailureAt` and `LastError`. May trigger state transitions. The `error` parameter captures diagnostic information about the failure. Does not throw exceptions under normal operation.

- **`void ChangeState(CircuitBreakerState newState)`**  
  Transitions the circuit breaker to a new state. Updates `LastStateChangeAt` and resets `FailureCount`/`SuccessCount` as required by state machine rules. Throws `ArgumentException` if `newState` is invalid or violates state transition constraints.

- **`void Reset()`**  
  Resets all counters (`FailureCount`, `SuccessCount`, `TotalFailures`, `TotalSuccesses`, `TotalRequests`) to zero, clears `LastError`, and sets `State` to Closed. Updates `LastStateChangeAt` to current time. Does not throw exceptions.

- **`double GetSuccessRate()`**  
  Calculates the ratio of successful requests to total requests as a percentage (0.0 to 100.0). Returns 0.0 if `TotalRequests` is zero.

- **`double GetFailureRate()`**  
  Calculates the ratio of failed requests to total requests as a percentage (0.0 to 100.0). Returns 0.0 if `TotalRequests` is zero.

## Usage

### Example 1: Monitoring Circuit Breaker Metrics

```csharp
var circuitBreaker = new CircuitBreakerStatus("svc-123", "PaymentService");

// Simulate request outcomes
circuitBreaker.RecordSuccess();
circuitBreaker.RecordSuccess();
circuitBreaker.RecordFailure("Timeout after 5s");

Console.WriteLine($"Current state: {circuitBreaker.State}");
Console.WriteLine($"Success rate: {circuitBreaker.GetSuccessRate():F2}%");
Console.WriteLine($"Last error: {circuitBreaker.LastError}");
```

### Example 2: Resetting After Recovery

```csharp
var circuitBreaker = new CircuitBreakerStatus("svc-456", "InventoryService");

// Simulate degraded state
circuitBreaker.RecordFailure("Connection refused");
circuitBreaker.RecordFailure("Connection refused");
circuitBreaker.ChangeState(CircuitBreakerState.Open);

// After recovery timeout, reset for retry
circuitBreaker.Reset();

Console.WriteLine($"State after reset: {circuitBreaker.State}");
Console.WriteLine($"Total requests: {circuitBreaker.TotalRequests}");
```

## Notes

- **Thread Safety**: This type is not thread-safe. Concurrent calls to `RecordSuccess()`, `RecordFailure()`, or `ChangeState()` may result in race conditions or inconsistent state. External synchronization is required in multi-threaded scenarios.

- **Division Edge Cases**: Both `GetSuccessRate()` and `GetFailureRate()` return 0.0 when `TotalRequests` is zero to prevent division-by-zero exceptions. Consumers should handle this explicitly if needed.

- **Nullable Timestamps**: `LastFailureAt` and `LastSuccessAt` remain null until their respective events occur. Check for null before accessing to avoid exceptions.

- **State Transition Rules**: `ChangeState()` enforces valid state transitions (e.g., Open → HalfOpen → Closed). Invalid transitions throw `ArgumentException`. Consult `CircuitBreakerState` documentation for allowed transitions.

- **Error Message Truncation**: `LastError` may truncate or sanitize input to prevent memory exhaustion from excessively long error messages.
