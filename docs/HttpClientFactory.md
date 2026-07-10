# HttpClientFactory

`HttpClientFactory` is a utility class designed to manage and reuse `HttpClient` instances in a controlled manner within the `dotnet-api-gateway` project. It provides mechanisms to create, retrieve, and manage transient or named `HttpClient` instances with configurable timeouts, ensuring efficient resource usage and avoiding common pitfalls like socket exhaustion.

## API

### `public HttpClientFactory`

Initializes a new instance of the `HttpClientFactory` class. This factory maintains an internal registry of `HttpClient` instances, allowing for centralized management of HTTP clients used across the application.

---

### `public HttpClient GetClient()`

Retrieves an existing `HttpClient` instance from the factory's internal registry. If no client exists with the default name, a new one is created and added to the registry.

- **Return value**: An `HttpClient` instance, either newly created or retrieved from the registry.
- **Throws**: `InvalidOperationException` if the internal registry is corrupted or in an invalid state.

---

### `public HttpClient CreateTransientClient()`

Creates a new, isolated `HttpClient` instance that is not tracked or reused by the factory. This method is useful for scenarios requiring short-lived, one-off HTTP requests without the overhead of registry management.

- **Return value**: A new `HttpClient` instance with default configuration.
- **Throws**: `InvalidOperationException` if the underlying `HttpClient` creation fails due to system constraints.

---
### `public void SetClientTimeout(TimeSpan timeout)`

Updates the default timeout for all `HttpClient` instances managed by the factory. This timeout applies to requests made by clients retrieved via `GetClient()` unless overridden per request.

- **Parameters**:
  - `timeout`: The `TimeSpan` representing the maximum duration allowed for HTTP requests. Must be a positive value.
- **Throws**:
  - `ArgumentOutOfRangeException` if `timeout` is negative or zero.
  - `InvalidOperationException` if the timeout cannot be applied to existing clients (e.g., due to disposal).

---
### `public void RemoveClient()`

Removes the default `HttpClient` instance from the factory's registry. Subsequent calls to `GetClient()` will create and return a new instance.

- **Throws**: `InvalidOperationException` if the default client does not exist in the registry.

---
### `public void Clear()`

Removes all `HttpClient` instances from the factory's registry. This includes the default client and any named clients that may have been added.

---
### `public int GetClientCount()`

Returns the number of `HttpClient` instances currently tracked by the factory.

- **Return value**: An integer representing the count of managed clients.

## Usage

### Example 1: Basic Usage with Default Client
