# ExternalApiClient

The `ExternalApiClient` class provides a typed HTTP client for communicating with external APIs. It wraps an underlying `HttpClient` and exposes generic methods for common HTTP verbs, handling JSON serialization and deserialization, and returning either typed responses or raw `HttpResponseMessage` objects. This class is intended to be reused across the application to manage connection pooling and reduce resource overhead.

## API

### `public ExternalApiClient()`

- **Purpose**: Initializes a new instance of the `ExternalApiClient`.
- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

### `public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)`

- **Purpose**: Sends a GET request to the specified URI and deserializes the response body to type `T`.
- **Parameters**:
  - `requestUri` (`string`): The endpoint URI.
  - `cancellationToken` (`CancellationToken`, optional): Token to cancel the operation.
- **Return value**: `Task<T?>` – The deserialized response object, or `null` if the response status indicates no content (e.g., HTTP 204) or deserialization fails.
- **Throws**: `HttpRequestException` on network or transport errors; `TaskCanceledException` if the operation is cancelled; `InvalidOperationException` if deserialization fails.

### `public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest requestBody, CancellationToken cancellationToken = default)`

- **Purpose**: Sends a POST request with a serialized request body and deserializes the response to type `TResponse`.
- **Parameters**:
  - `requestUri` (`string`): The endpoint URI.
  - `requestBody` (`TRequest`): The object to serialize as JSON in the request body.
  - `cancellationToken` (`CancellationToken`, optional): Token to cancel the operation.
- **Return value**: `Task<TResponse?>` – The deserialized response object, or `null` if the response has no content or deserialization fails.
- **Throws**: `ArgumentNullException` if `requestBody` is `null`; `HttpRequestException`; `TaskCanceledException`; `InvalidOperationException` on deserialization failure.

### `public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest requestBody, CancellationToken cancellationToken = default)`

- **Purpose**: Sends a PUT request with a serialized request body and deserializes the response to type `TResponse`.
- **Parameters**: Same as `PostAsync`.
- **Return value**: Same as `PostAsync`.
- **Throws**: Same as `PostAsync`.

### `public async Task<bool> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)`

- **Purpose**: Sends a DELETE request to the specified URI and returns `true` if the response status code indicates success (2xx), otherwise `false`.
- **Parameters**:
  - `requestUri` (`string`): The endpoint URI.
  - `cancellationToken` (`CancellationToken`, optional): Token to cancel the operation.
- **Return value**: `Task<bool>` – `true` for a successful status code, `false` otherwise.
- **Throws**: `HttpRequestException`; `TaskCanceledException`.

### `public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)`

- **Purpose**: Sends an arbitrary `HttpRequestMessage` and returns the raw response without any deserialization or additional processing.
- **Parameters**:
  - `request` (`HttpRequestMessage`): The fully constructed HTTP request message.
  - `cancellationToken` (`CancellationToken`, optional): Token to cancel the operation.
- **Return value**: `Task<HttpResponseMessage>` – The response message.
- **Throws**: `HttpRequestException`; `TaskCanceledException`.

### `public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)`

- **Purpose**: Sends an `HttpRequestMessage` and returns the raw response. This method applies built-in policies such as authentication header injection, retry logic, or logging, unlike `SendRequestAsync` which passes the request through without modification.
- **Parameters**: Same as `SendRequestAsync`.
- **Return value**: Same as `SendRequestAsync`.
- **Throws**: Same as `SendRequestAsync`.

## Usage

### Example 1: Fetching a resource with `GetAsync`

```csharp
var client = new ExternalApiClient();
var user = await client.GetAsync<User>("https://api.example.com/users/42");
if (user != null)
{
    Console.WriteLine($"User: {user.Name}");
}
```

### Example 2: Creating a resource with `PostAsync`

```csharp
var client = new ExternalApiClient();
var newOrder = new Order { ProductId = 100, Quantity = 2 };
var createdOrder = await client.PostAsync<Order, Order>("https://api.example.com/orders", newOrder);
if (createdOrder != null)
{
    Console.WriteLine($"Order created: {createdOrder.Id}");
}
```

## Notes

- **Thread safety**: The `ExternalApiClient` is designed to be thread-safe. Multiple concurrent calls to any of its methods are supported. The underlying `HttpClient` should not be disposed while requests are in flight; the client is intended to be reused as a singleton or long-lived instance.
- **Null handling**: Passing `null` for `requestUri` or `requestBody` (where applicable) will throw `ArgumentNullException`. Typed methods return `null` for responses with no content (e.g., HTTP 204) or when deserialization fails; always check for `null` before accessing properties.
- **Cancellation**: All async methods accept an optional `CancellationToken`. If the token is cancelled, a `TaskCanceledException` is thrown. Use `CancellationToken.None` only when cancellation is not required.
- **Error handling**: Non-success HTTP status codes (4xx, 5xx) do not throw by default. For typed methods, they result in a `null` or `false` return value. Use `SendRequestAsync` or `SendAsync` to inspect the full `HttpResponseMessage` and handle errors explicitly.
- **Disposal**: The class does not expose `IDisposable` directly. For proper lifecycle management, consider using `IHttpClientFactory` to obtain `HttpClient` instances and avoid manual disposal.
- **Serialization**: JSON serialization is used by default. Ensure that types `T`, `TRequest`, and `TResponse` are serializable (e.g., have parameterless constructors and appropriate attributes). Deserialization failures throw `InvalidOperationException`.
