# RateLimitingExample

The `RateLimitingExample` class serves as a demonstration implementation for token bucket rate limiting logic within the `dotnet-api-gateway` project. It encapsulates a `SimpleTokenBucketRateLimiter` instance to manage request throughput, providing static entry points for execution and instance members to evaluate permission status and retrieve current token availability. This type is designed to illustrate the integration of rate limiting strategies without enforcing specific infrastructure dependencies.

## API

### `public static async Task Main`
The primary entry point for the application or demonstration scenario. This method initializes the rate limiting context and executes the asynchronous workflow to showcase the limiter's behavior over time.
*   **Parameters**: None (accepts standard `string[] args` implicitly if defined in the containing program structure, though signature indicates parameterless static invocation).
*   **Return Value**: A `Task` representing the asynchronous operation, completing when the demonstration sequence finishes.
*   **Exceptions**: May throw standard runtime exceptions if the underlying asynchronous operations fail or if the runtime environment is misconfigured.

### `public SimpleTokenBucketRateLimiter`
Exposes the underlying rate limiter instance used by this class. This property provides direct access to the `SimpleTokenBucketRateLimiter` object responsible for the actual token bucket algorithm mechanics.
*   **Parameters**: None (property getter).
*   **Return Value**: The `SimpleTokenBucketRateLimiter` instance associated with this example.
*   **Exceptions**: None typically thrown by the getter itself; however, accessing members on the returned object may throw depending on its internal state.

### `public bool IsAllowed`
Determines whether a request should be permitted based on the current state of the token bucket. This method attempts to consume a token if available.
*   **Parameters**: None.
*   **Return Value**: Returns `true` if a token was successfully consumed and the request is allowed; otherwise, returns `false`.
*   **Exceptions**: None explicitly defined; relies on the thread-safety of the underlying limiter implementation.

### `public double GetTokensRemaining`
Retrieves the current number of tokens available in the bucket without modifying the state or consuming a token.
*   **Parameters**: None.
*   **Return Value**: A `double` representing the precise count of remaining tokens.
*   **Exceptions**: None.

## Usage

### Basic Permission Check
The following example demonstrates how to instantiate the example class (or access its context) and verify if a request is allowed before proceeding with business logic.

```csharp
// Assuming an instance context where the limiter is initialized
var example = new RateLimitingExample(); 

if (example.IsAllowed)
{
    // Proceed with the protected operation
    await ProcessRequestAsync();
}
else
{
    // Handle rate limit exceeded scenario
    Console.WriteLine("Request rejected: Rate limit exceeded.");
}
```

### Monitoring Token Availability
This example illustrates how to inspect the current capacity of the token bucket to make logging decisions or implement adaptive backoff strategies.

```csharp
var example = new RateLimitingExample();
double currentTokens = example.GetTokensRemaining();

if (currentTokens < 1.0)
{
    // Log warning when bucket is nearly empty
    Console.WriteLine($"Warning: Low token capacity ({currentTokens:F2}).");
}

// Attempt to execute regardless, checking result immediately
bool allowed = example.IsAllowed;
```

## Notes

*   **Thread Safety**: The `IsAllowed` and `GetTokensRemaining` members interact with the shared `SimpleTokenBucketRateLimiter` instance. While the specific thread-safety guarantees depend on the implementation of `SimpleTokenBucketRateLimiter`, typical token bucket algorithms require atomic operations for token consumption. If the underlying limiter is not explicitly thread-safe, concurrent calls to `IsAllowed` from multiple threads may result in race conditions where more tokens are consumed than available.
*   **State Mutation**: Calling `IsAllowed` is a mutating operation that decreases the token count upon success. In contrast, `GetTokensRemaining` is a read-only observation. The value returned by `GetTokensRemaining` may become stale immediately if another thread invokes `IsAllowed` concurrently.
*   **Token Precision**: The `GetTokensRemaining` method returns a `double`, indicating that the implementation supports fractional tokens, likely to accommodate continuous token refill rates rather than discrete integer increments.
*   **Execution Context**: The `Main` method is asynchronous (`async Task`). Consumers invoking this method directly must ensure they await the task to properly handle any asynchronous initialization or cleanup logic contained within the demonstration workflow.
