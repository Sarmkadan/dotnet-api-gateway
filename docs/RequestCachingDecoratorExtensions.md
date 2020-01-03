# RequestCachingDecoratorExtensions
The `RequestCachingDecoratorExtensions` class provides a set of extension methods for caching and fetching requests in a .NET API gateway application. It enables developers to implement caching mechanisms that can improve the performance and responsiveness of their applications by reducing the number of requests made to external services.

## API
The `RequestCachingDecoratorExtensions` class includes the following public members:
- `GetOrFetchAsync<T>`: Retrieves a cached response of type `T` if available, or fetches the response from the underlying service if not cached. Returns a `Task` that resolves to the response of type `T`.
- `GetOrFetchWithFallbackAsync<T>`: Similar to `GetOrFetchAsync<T>`, but returns a nullable `T` (`T?`) to indicate that the response might not be available.
- `InvalidateMultipleAsync`: Invalidates multiple cached responses. Returns a `Task` that completes when the invalidation is done.
- `GetOrFetchAsync<T, TKey>`: Retrieves a cached response of type `T` if available, or fetches the response from the underlying service if not cached, using a custom key of type `TKey`. Returns a `Task` that resolves to the response of type `T`.

These methods can throw exceptions if there are issues with the underlying caching or fetching mechanisms, such as network errors or cache store failures.

## Usage
Here are examples of how to use the `RequestCachingDecoratorExtensions` class:
```csharp
// Example 1: Simple caching
var cachedResponse = await RequestCachingDecoratorExtensions.GetOrFetchAsync<string>("https://example.com/api/data");
Console.WriteLine(cachedResponse);

// Example 2: Custom key caching with fallback
var customKey = new CustomCacheKey("example", "data");
var response = await RequestCachingDecoratorExtensions.GetOrFetchWithFallbackAsync<string>(customKey);
if (response != null)
{
    Console.WriteLine(response);
}
else
{
    Console.WriteLine("No cached response available.");
}
```

## Notes
When using the `RequestCachingDecoratorExtensions` class, consider the following:
- The caching mechanisms used by these extension methods are designed to be thread-safe, allowing them to be safely used in concurrent environments.
- If the underlying service returns an error response, it will be cached and returned on subsequent requests until the cache is invalidated.
- Custom cache keys (like `TKey` in `GetOrFetchAsync<T, TKey>`) should be implemented to properly override `Equals` and `GetHashCode` methods to ensure correct cache key comparison.
- The `InvalidateMultipleAsync` method can be used to clear cached responses for multiple keys at once, which can be useful for handling cache invalidation in response to external events or updates.
