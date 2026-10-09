# Circuit Breaker

The gateway uses a circuit breaker to stop sending traffic to a downstream service that is failing, so that one broken service does not drag down the whole gateway.

## States

| State | Behaviour |
|-------|-----------|
| `Closed` | Normal operation. Requests pass through. Failures are counted. |
| `Open` | Requests are rejected immediately with `CircuitBreakerException`. No call reaches the downstream service. |
| `HalfOpen` | After the timeout, a limited number of trial requests are allowed. Enough successes close the circuit, a single failure reopens it. |

Transitions:

- `Closed` -> `Open`: `FailureCount` reaches `FailureThreshold`.
- `Open` -> `HalfOpen`: `TimeoutSeconds` have passed since the circuit opened (checked on the next request).
- `HalfOpen` -> `Closed`: `SuccessCount` reaches `SuccessThreshold`.
- `HalfOpen` -> `Open`: any failure.

In `Closed` state, each successful request decrements the failure counter (never below zero), so occasional errors do not accumulate forever.

## Policy settings

`CircuitBreakerPolicy` (`Models/CircuitBreakerPolicy.cs`) holds the rules:

| Property | Default | Allowed range | Meaning |
|----------|---------|---------------|---------|
| `FailureThreshold` | 5 | >= 1 | Failures in `Closed` state before the circuit opens. |
| `SuccessThreshold` | 2 | >= 1 | Successes in `HalfOpen` state before the circuit closes. |
| `TimeoutSeconds` | 60 | 10 - 600 | How long the circuit stays `Open` before trial requests are allowed. |
| `FailureStatusCodes` | 500, 502, 503, 504 | - | Response codes that count as failures. |
| `Enabled` | true | - | When `false`, the circuit breaker is bypassed entirely. |
| `MaxRetries` | 3 | 0 - 10 | Retry attempts for a failed request. |
| `RetryDelayMilliseconds` | 100 | 10 - 5000 | Delay between retry attempts. |

`Validate()` throws `ArgumentException` when a value is outside its range. Call it when loading configuration so bad values fail at startup, not at the first failure.

## Runtime behaviour

`CircuitBreakerService` (`Services/CircuitBreakerService.cs`) holds the state machine. For each downstream service it provides:

- `CanAttemptAsync` - returns `true` if a request may proceed. In `Open` state it throws `CircuitBreakerException` with the number of seconds left until the next trial.
- `RecordSuccessAsync` / `RecordFailureAsync` - update counters and drive state transitions.
- State changes are serialised per service name with a `SemaphoreSlim`, so concurrent requests do not corrupt the counters.

`CircuitBreakerPolicy.IsFailureStatus` decides which status codes count as failures (it checks `FailureStatusCodes`). Calling code is responsible for applying it; it is not applied automatically.

When a policy has `Enabled = false`, `CanAttemptAsync` returns `true` and the record methods do nothing.

Note: at the time of writing, the request-forwarding pipeline does not call these methods, and `MaxRetries` / `RetryDelayMilliseconds` are validated but not used in production code. Check the forwarding path before relying on automatic circuit breaking.

## Monitoring API

`CircuitBreakerController` (`Controllers/CircuitBreakerController.cs`) exposes the state under `/api/CircuitBreaker`:

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/CircuitBreaker/statuses` | All circuit breaker statuses. |
| GET | `/api/CircuitBreaker/statuses/{serviceName}` | Status for one service. Returns 404 if none exists. |
| GET | `/api/CircuitBreaker/statuses/open` | Circuits currently `Open`. |
| GET | `/api/CircuitBreaker/statuses/half-open` | Circuits currently `HalfOpen`. |
| GET | `/api/CircuitBreaker/statuses/closed` | Circuits currently `Closed`. |
| POST | `/api/CircuitBreaker/statuses/{serviceName}/reset` | Reset one circuit to `Closed`. |
| POST | `/api/CircuitBreaker/statuses/reset-all` | Reset all circuits to `Closed`. |

Each status (`Models/CircuitBreakerStatus.cs`) contains the current state, `FailureCount`, `SuccessCount`, `LastStateChangeAt`, `LastError`, and lifetime totals (`TotalRequests`, `TotalFailures`, `TotalSuccesses`).

Resetting a circuit clears its counters and sets it to `Closed` without waiting for the timeout. Use it only after the downstream service has been checked, otherwise traffic goes straight back to a broken service.

## Operational notes

- Circuit state is kept per service name. Two routes that point to the same downstream service share one circuit.
- A circuit that opens will stay open for at least `TimeoutSeconds`, even if the downstream service recovers earlier. Lower the timeout if recovery is usually fast.
- Set `FailureThreshold` high enough that a single slow request does not open the circuit, but low enough that a real outage is caught within a few seconds of traffic.
- Watch `GET /api/CircuitBreaker/statuses/open` in monitoring. An open circuit means users are getting errors from that service right now.
