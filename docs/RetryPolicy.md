# RetryPolicy

A robust retry policy implementation for HTTP requests that prevents retry storms and handles transient failures gracefully. Features include:

- **Jittered exponential backoff**: Randomized delays between retries to prevent thundering herd problems
- **Retry budget**: Token bucket algorithm to limit total retries across all requests
- **Idempotent method filtering**: Only retries GET, HEAD, and OPTIONS by default (configurable for other methods)
- **Comprehensive error handling**: Retries on 5xx errors, timeouts, and connection failures
- **Per-request retry budget**: Each retry consumes from a shared token bucket

## API

## API

### `RetryPolicy(...)`

Initializes a new instance of the `RetryPolicy` class with configurable retry settings.

**Parameters:**
- **maxRetries** (int): Maximum number of retry attempts (default: 3)
- **initialDelayMs** (int): Initial delay in milliseconds before first retry (default: 100)
- **maxDelayMs** (int): Maximum delay in milliseconds for any retry (default: 30000)
- **backoffMultiplier** (double): Multiplier for exponential backoff (default: 2.0)
- **maxRetryBudgetTokens** (int): Maximum retry tokens in the budget bucket (default: 100)
- **retryBudgetRefillRatePerSecond** (double): Tokens refilled per second (default: 1.0)
- **allowNonIdempotentRetries** (bool): Whether to allow retries on non-idempotent methods like POST/PUT/DELETE (default: false)
- **logger** (ILogger<RetryPolicy>): Logger instance (default: null)

**Throws:** `ArgumentOutOfRangeException` when parameters are out of valid range.

### `async Task<HttpResponseMessage> ExecuteAsync(HttpClient client, HttpRequestMessage request, Func<HttpStatusCode, bool>? shouldRetry = null, CancellationToken cancellationToken = default)`

Executes an HTTP request with retry logic applied. Automatically handles:
- Retry budget consumption (each retry consumes a token)
- Jittered exponential backoff between retries
- Idempotent method checking (unless `allowNonIdempotentRetries` is true)
- Transient error detection (5xx, timeouts, connection failures)

**Parameters:**
- **client**: The HTTP client to use for the request
- **request**: The HTTP request message to execute
- **shouldRetry**: Optional predicate to determine if a status code should be retried
- **cancellationToken**: A token to monitor for cancellation requests

**Returns**: A task that represents the HTTP response message

**Throws**:
- `ArgumentNullException` if `client` or `request` is `null`
- `InvalidOperationException` if retry budget is exhausted

### `async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, Func<Exception, bool>? shouldRetry = null, CancellationToken cancellationToken = default)`

Executes an async operation with retry logic applied.

**Parameters:**
- **operation**: The async operation to execute
- **shouldRetry**: Optional predicate to determine if an exception should be retried
- **cancellationToken**: A token to monitor for cancellation requests

**Returns**: A task that represents the result of type `T`

**Throws**:
- `ArgumentNullException` if `operation` is `null`
- `InvalidOperationException` if retry budget is exhausted during retries

### `bool IsTransientStatusCode(HttpStatusCode statusCode)`

Determines if an HTTP status code represents a transient error that can be retried.

**Parameters:**
- **statusCode**: The HTTP status code to check

**Returns**: `true` if the status code is transient; otherwise, `false`

### `bool IsIdempotentMethod(HttpMethod method)`

Determines if an HTTP method is idempotent (safe to retry).

**Parameters:**
- **method**: The HTTP method to check

**Returns**: `true` if the method is idempotent (GET, HEAD, OPTIONS); otherwise, `false`

## Usage

### Basic HTTP Request Retry
