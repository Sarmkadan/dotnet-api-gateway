# CircuitBreakerPolicy

The `CircuitBreakerPolicy` type defines the configuration and runtime behavior of a circuit breaker used within the API gateway to protect downstream services from cascading failures. It encapsulates thresholds for opening and closing the circuit, timeout values, retry settings, and validation logic, allowing callers to declaratively control fault‑tolerance policies.

## API

| Member | Type | Description |
|--------|------|-------------|
| **Id** | `string` | A unique identifier for the policy instance. Used for logging, diagnostics, and distinguishing multiple policies within the same gateway. Must not be null or empty; setting an invalid value will cause `Validate` to throw. |
| **FailureThreshold** | `int` | The number of consecutive failures (as determined by `IsFailureStatus`) required to trip the circuit breaker to the open state. Must be greater than zero; otherwise validation fails. |
| **SuccessThreshold** | `int` | The number of consecutive successful responses needed while the circuit is half‑open to transition back to the closed state. Must be greater than zero; otherwise validation fails. |
| **TimeoutSeconds** | `int` | The duration (in seconds) the circuit breaker remains open before attempting a half‑open trial. Must be non‑negative; a value of zero disables the timeout and keeps the circuit open until manually reset. |
| **FailureStatusCodes** | `int[]` | An array of HTTP status codes that are considered failures for the purpose of incrementing the failure counter. Empty or null arrays are treated as “no specific codes”, meaning any non‑success status is considered a failure. Validation throws if any element is outside the range 100‑599. |
| **Enabled** | `bool` | Indicates whether the circuit breaker logic is active. When `false`, the gateway bypasses all circuit breaker checks and treats every request as passed through. |
| **MaxRetries** | `int` | The maximum number of retry attempts to perform when a request fails and the circuit is closed. Must be zero or greater; negative values cause validation to fail. |
| **RetryDelayMilliseconds** | `int` | The fixed delay, in milliseconds, between successive retry attempts. Must be non‑negative; a value of zero results in immediate retries. |
| **Validate** | `void` | Verifies that all property values are within acceptable ranges and that the internal state is consistent. Throws `ArgumentException` with a descriptive message if any validation rule is violated (e.g., negative thresholds, duplicate or out‑of‑range status codes, null/empty `Id`). This method should be called after constructing or modifying the policy before it is used by the gateway. |
| **IsFailureStatus** | `bool` | Returns `true` if the most recently evaluated HTTP status code (provided externally by the gateway) matches one of the codes in `FailureStatusCodes` or, when the array is empty/null, if the status code is not in the 2xx range. This property does not accept parameters; it relies on the gateway having set an internal status value prior to invocation. |
| **IsEnabled** | `bool` | Returns the current effective state of the circuit breaker, factoring in both the `Enabled` flag and the internal open/half‑open/closed state. When `true`, the policy will apply failure counting, timeout, and retry logic; when `false`, the gateway ignores the policy entirely. |

## Usage

### Basic configuration and validation

```csharp
using DotNetApiGateway.Policies;

var policy = new CircuitBreakerPolicy
{
    Id = "service-a-breaker",
    FailureThreshold = 5,
    SuccessThreshold = 2,
    TimeoutSeconds = 30,
    FailureStatusCodes = new[] { 500, 502, 503, 504 },
    Enabled = true,
    MaxRetries = 3,
    RetryDelayMilliseconds = 500
};

policy.Validate; // throws if any property is invalid
```

### Checking status and evaluating the breaker state

```csharp
// Assume the gateway has just processed a request and stored the status code
policy.IsFailureStatus; // true if the status code matches FailureStatusCodes or is non‑2xx when the array is empty

if (policy.IsEnabled && !policy.IsFailureStatus)
{
    // Success path – increment success counter internally (handled by gateway)
}
else
{
    // Failure path – increment failure counter; gateway will trip breaker when threshold reached
}
```

## Notes

- **Thread safety**: The properties of `CircuitBreakerPolicy` are intended to be immutable after validation. Concurrent reads are safe, but concurrent writes to any property without external synchronization may lead to race conditions and inconsistent state. It is recommended to configure the policy before it is registered with the gateway and treat it as read‑only thereafter.
- **Empty `FailureStatusCodes`**: When the array is null or empty, `IsFailureStatus` treats any HTTP status code outside the 2xx range as a failure. This behavior mirrors typical circuit breaker semantics where unspecified codes default to failure treatment.
- **Timeout semantics**: A `TimeoutSeconds` value of zero disables the automatic half‑open transition; the circuit remains open until the gateway manually resets it (e.g., via administrative API). Negative values are invalid and will cause `Validate` to throw.
- **Retry interaction**: `MaxRetries` and `RetryDelayMilliseconds` are only consulted when the circuit is closed and a request fails. If the circuit is open, retries are not attempted regardless of these settings.
- **Validation timing**: Call `Validate` immediately after constructing or mutating the policy instance. The gateway does not re‑validate the policy on each request; using an invalid configuration after validation may result in undefined behavior.
