# CircuitBreakerStatusTests

`CircuitBreakerStatusTests` is a test suite that validates the behavior of the circuit breaker status tracking component within the `dotnet-api-gateway` project. It exercises state transitions, success/failure counting, rate calculations, counter resets, and integration with the underlying repository layer to ensure the circuit breaker correctly implements its resilience patterns under both synchronous and asynchronous conditions.

## API

### public void RecordSuccess_InitialState_IncrementsAllCounters
Verifies that recording a success when the circuit breaker is in its initial state increments both the success counter and the total request counter.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Assertion failures if counters are not incremented as expected.

### public void RecordFailure_SetsLastErrorAndIncrements
Confirms that recording a failure sets the `LastError` property and increments the failure counter along with the total request counter.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if `LastError` remains unset or counters are not updated.

### public void RecordSuccess_ClearsLastError
Ensures that after a failure has been recorded, a subsequent success clears the `LastError` property.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if `LastError` is not cleared.

### public void GetSuccessRate_WithNoRequests_ReturnsOne
Tests that the success rate calculation returns `1` (100%) when no requests have been recorded, avoiding division-by-zero scenarios.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the returned rate is not `1`.

### public void GetSuccessRate_WithMixedRequests_CalculatesCorrectly
Validates that the success rate is computed accurately when a mix of successes and failures has been recorded.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the calculated rate deviates from the expected ratio.

### public void GetFailureRate_PlusSuccessRate_AlwaysEqualsOne
Asserts the invariant that the sum of the success rate and the failure rate equals `1` under all recorded request combinations.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the invariant is violated.

### public void ChangeState_ToClosed_ResetsFailureAndSuccessCounters
Checks that transitioning the circuit breaker state to `Closed` resets both the failure counter and the success counter.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if either counter is not reset.

### public void ChangeState_ToHalfOpen_ResetsSuccessCountOnly
Verifies that changing the state to `HalfOpen` resets only the success counter while leaving the failure counter intact.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the success counter is not reset or the failure counter is incorrectly modified.

### public void ChangeState_SameState_DoesNotResetCounters
Ensures that setting the circuit breaker state to its current state does not cause any counter resets.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if counters are altered during a no-op state change.

### public void Reset_ClearsStateCountersAndError
Tests that invoking `Reset` clears the circuit state, all counters, and the `LastError` property.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if any tracked value persists after reset.

### public async Task RecordFailure_BeyondThreshold_OpensCircuit
Asynchronously validates that recording failures beyond the configured threshold causes the circuit to transition to the `Open` state.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if the circuit does not open after exceeding the failure limit.

### public async Task RecordSuccess_InHalfOpen_MeetsSuccessThreshold_ClosesCircuit
Asynchronously confirms that when the circuit is `HalfOpen`, recording enough consecutive successes to meet the success threshold transitions the circuit back to `Closed`.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if the circuit fails to close after the required successes.

### public async Task RecordFailure_InHalfOpen_ReopensCircuit
Asynchronously verifies that a single failure recorded while the circuit is `HalfOpen` immediately reopens the circuit.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if the circuit does not return to `Open` upon failure.

### public async Task CanAttempt_WhenPolicyDisabled_BypassesCircuitState
Asynchronously tests that when the circuit breaker policy is disabled, `CanAttempt` returns `true` regardless of the underlying circuit state.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if `CanAttempt` incorrectly respects circuit state while disabled.

### public async Task ResetCircuit_OpensCircuit_RestoresClosed
Asynchronously demonstrates that forcibly resetting an open circuit restores it to the `Closed` state and clears associated counters.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if the circuit is not restored to `Closed` or counters remain.

### public async Task IRepository_MockSetup_ReturnsExpectedStatus
Asynchronously validates that the mocked repository interface returns the expected circuit breaker status object, confirming correct dependency setup for integration scenarios.  
**Parameters:** None.  
**Returns:** `Task`.  
**Throws:** Assertion failures if the repository mock does not provide the anticipated status.

## Usage

### Example 1: Verifying Full Lifecycle in a Unit Test
```csharp
[TestMethod]
public async Task CircuitBreaker_FullLifecycle_TransitionsCorrectly()
{
    var tests = new CircuitBreakerStatusTests();

    // Initial state and success recording
    tests.RecordSuccess_InitialState_IncrementsAllCounters();
    tests.GetSuccessRate_WithNoRequests_ReturnsOne();

    // Induce failure and verify error handling
    tests.RecordFailure_SetsLastErrorAndIncrements();
    tests.RecordSuccess_ClearsLastError();

    // Force circuit open and verify reset behavior
    await tests.RecordFailure_BeyondThreshold_OpensCircuit();
    tests.ChangeState_ToClosed_ResetsFailureAndSuccessCounters();

    // Half-open probing and recovery
    tests.ChangeState_ToHalfOpen_ResetsSuccessCountOnly();
    await tests.RecordSuccess_InHalfOpen_MeetsSuccessThreshold_ClosesCircuit();
}
```

### Example 2: Testing Rate Invariants and Policy Bypass
```csharp
[TestMethod]
public async Task CircuitBreaker_RatesAndBypass_RemainConsistent()
{
    var tests = new CircuitBreakerStatusTests();

    // Validate rate calculations under mixed traffic
    tests.RecordFailure_SetsLastErrorAndIncrements();
    tests.RecordSuccess_InitialState_IncrementsAllCounters();
    tests.GetSuccessRate_WithMixedRequests_CalculatesCorrectly();
    tests.GetFailureRate_PlusSuccessRate_AlwaysEqualsOne();

    // Ensure policy disable bypasses circuit state
    await tests.CanAttempt_WhenPolicyDisabled_BypassesCircuitState();

    // Confirm repository integration returns expected status
    await tests.IRepository_MockSetup_ReturnsExpectedStatus();
}
```

## Notes

- **Counter reset semantics:** `ChangeState` methods enforce distinct reset rules—`Closed` clears both counters, `HalfOpen` clears only successes, and same-state transitions are idempotent. Callers must not rely on implicit resets across unrelated state changes.
- **Rate invariants:** `GetSuccessRate` returns `1` when no requests exist to prevent division by zero. The invariant `GetFailureRate + GetSuccessRate == 1` is guaranteed only when at least one request has been recorded; both methods should be used together for consistency checks.
- **Thread safety:** The test methods themselves are single-threaded assertions. The underlying circuit breaker status implementation must ensure atomic counter updates and state transitions when used concurrently; these tests do not directly stress concurrent access but imply correctness expectations for production use.
- **Asynchronous behavior:** Async test methods (`RecordFailure_BeyondThreshold_OpensCircuit`, etc.) assume an asynchronous execution context. Time-sensitive thresholds (failure limits, success counts in half-open state) must be respected without race conditions in real implementations.
- **Repository integration:** `IRepository_MockSetup_ReturnsExpectedStatus` confirms that the dependency on `IRepository` is correctly abstracted. Production code should inject a repository implementation that persists status atomically to avoid stale state reads.
