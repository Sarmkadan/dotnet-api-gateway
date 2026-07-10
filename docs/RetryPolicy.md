# RetryPolicy

A utility class that implements retry logic for HTTP requests, allowing transient faults to be handled gracefully with configurable retry attempts and delays.

## API

### `RetryPolicy()`
Initializes a new instance of the `RetryPolicy` class with default retry settings.

### `async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)`
Executes an HTTP request with retry logic applied.

- **request**: The HTTP request message to execute.
- **cancellationToken**: A token to monitor for cancellation requests.
- **Returns**: A task that represents the HTTP response message.
- **Throws**: `ArgumentNullException` if `request` is `null`.

### `async Task<T> ExecuteAsync<T>(HttpRequestMessage request, Func<HttpResponseMessage, Task<T>> responseHandler, CancellationToken cancellationToken = default)`
Executes an HTTP request with retry logic and applies a custom response handler to extract a result.

- **request**: The HTTP request message to execute.
- **responseHandler**: A function that processes the HTTP response and returns a task with the result.
- **cancellationToken**: A token to monitor for cancellation requests.
- **Returns**: A task that represents the result of type `T`.
- **Throws**:
  - `ArgumentNullException` if `request` or `responseHandler` is `null`.
  - Propagates exceptions from `responseHandler`.

### `IDisposable? BeginScope<TState>(TState state)`
Begins a logical operation scope.

- **state**: The identifier for the scope.
- **Returns**: A disposable object that ends the logical operation scope on dispose.
- **Remarks**: This method is part of the logging infrastructure and may return `null` if logging is disabled.

### `bool IsEnabled`
Gets a value indicating whether the current retry policy is enabled.

- **Returns**: `true` if the retry policy is enabled; otherwise, `false`.

### `void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)`
Writes a log entry.

- **logLevel**: Entry will be written on this level.
- **eventId**: Id of the event.
- **state**: The entry to be written. Can be also an object.
- **exception**: The exception related to this entry.
- **formatter**: Function to create a string message of the state and exception.
- **Remarks**: This method is part of the logging infrastructure and may be a no-op if logging is disabled.

## Usage

### Basic HTTP Request Retry
