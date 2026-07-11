# RateLimitStoreFactory

The `RateLimitStoreFactory` class serves as the central abstraction for creating and managing instances of rate limit storage backends within the API Gateway. It encapsulates the logic required to instantiate specific `IRateLimitStore` implementations based on configuration or runtime context, ensuring that rate limiting data can be persisted and retrieved consistently across different storage providers. As a disposable resource, it also manages the lifecycle of underlying connections or resources associated with the stores it generates.

## API

### `public RateLimitStoreFactory`
Initializes a new instance of the `RateLimitStoreFactory` class. This constructor typically prepares the internal state required to generate stores, such as loading configuration settings or establishing base connections, depending on the specific implementation details of the gateway's infrastructure.

### `public IRateLimitStore GetStore`
Retrieves a specific instance of a rate limit store.
*   **Purpose**: Provides access to a named or default storage backend for enforcing rate limits.
*   **Parameters**: Accepts a single string parameter representing the identifier or name of the desired store configuration.
*   **Return Value**: Returns an `IRateLimitStore` instance configured according to the specified identifier.
*   **Exceptions**: Throws an exception if the requested store identifier is not recognized, if the underlying provider is unavailable, or if initialization fails.

### `public IEnumerable<IRateLimitStore> GetAllStores`
Enumerates all available rate limit store instances managed by this factory.
*   **Purpose**: Allows iteration over every configured storage backend, useful for global health checks, bulk initialization, or administrative operations.
*   **Parameters**: None.
*   **Return Value**: Returns an `IEnumerable<IRateLimitStore>` containing all active store instances.
*   **Exceptions**: May throw if the enumeration process encounters a critical failure in accessing the configuration or underlying resources.

### `public void Dispose`
Releases all unmanaged resources and disposes of any managed objects held by the factory and its created stores.
*   **Purpose**: Ensures proper cleanup of database connections, file handles, or network sockets associated with the rate limit stores.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: Throws if disposal fails due to locked resources or internal state corruption, though implementations should generally suppress exceptions during disposal to prevent application crashes during shutdown.

## Usage

### Example 1: Retrieving a Specific Store
The following example demonstrates how to inject the factory and retrieve a specific store instance by name to check rate limits for a client.

```csharp
public class RateLimitMiddleware
{
    private readonly IRateLimitStore _store;

    public RateLimitMiddleware(RateLimitStoreFactory factory)
    {
        // Retrieve the store configured for "RedisPrimary"
        _store = factory.GetStore("RedisPrimary");
    }

    public async Task<bool> IsAllowedAsync(string clientId)
    {
        var currentCount = await _store.GetCountAsync(clientId);
        return currentCount < 100;
    }
}
```

### Example 2: Iterating All Stores for Health Checks
This example illustrates how to use `GetAllStores` to perform a connectivity check across all configured backends.

```csharp
public class HealthCheckService
{
    private readonly RateLimitStoreFactory _factory;

    public HealthCheckService(RateLimitStoreFactory factory)
    {
        _factory = factory;
    }

    public async Task<Dictionary<string, bool>> CheckStoreHealthAsync()
    {
        var results = new Dictionary<string, bool>();
        
        foreach (var store in _factory.GetAllStores())
        {
            try
            {
                await store.PingAsync();
                results[store.Name] = true;
            }
            catch
            {
                results[store.Name] = false;
            }
        }

        return results;
    }
}
```

## Notes

*   **Thread Safety**: The `GetStore` and `GetAllStores` methods are expected to be thread-safe, allowing concurrent access from multiple request threads. However, the returned `IRateLimitStore` instances may have their own concurrency constraints; callers should verify the thread safety of the specific store implementation before sharing instances across threads without synchronization.
*   **Resource Management**: Since `RateLimitStoreFactory` implements `Dispose`, it must be registered with a dependency injection container using a scoped or singleton lifetime that respects disposal. Failure to dispose of the factory may result in leaked connections, particularly if the underlying stores maintain persistent network sockets.
*   **Store Identity**: The string identifier passed to `GetStore` must match the configuration keys defined in the gateway's setup. Passing a null or empty string may result in the retrieval of a default store or an immediate exception, depending on the specific configuration policy enforced by the implementation.
*   **Enumeration Validity**: The collection returned by `GetAllStores` represents a snapshot of the stores available at the time of the call. If the factory supports dynamic reconfiguration, the enumerator does not automatically reflect changes made after the enumeration begins.
