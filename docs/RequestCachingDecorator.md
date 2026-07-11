# RequestCachingDecorator

A decorator that wraps request execution with an in-memory caching layer, intercepting calls to downstream services and returning cached responses when available. It is designed to reduce latency and offload external dependencies by storing previously fetched data keyed by request identity, while providing explicit invalidation control and optional fallback to stale cache entries when live retrieval fails.

## API

### `public RequestCachingDecorator`

Constructs a new instance of the decorator. The underlying cache store and request execution pipeline are injected at creation time and remain fixed for the lifetime of the object.

### `public async Task<T> GetOrFetchAsync<T>`

Retrieves a value of type `T` from the cache if present; otherwise executes the request, stores the result in the cache, and returns it.

- **Type parameter `T`**: The expected type of the cached or fetched response.
- **Returns**: A `Task<T>` that resolves to the cached value or the freshly fetched value.
- **Throws**: Propagates any exception thrown by the underlying fetch operation when a cache miss occurs and the value cannot be obtained. May throw if the cache infrastructure itself raises an error during storage or retrieval.

### `public async Task<T?> GetOrFetchWithFallbackAsync<T>`

Retrieves a value of type `T` from the cache if present; otherwise executes the request. If the fetch operation fails, it falls back to a previously cached value for the same key, even if that entry has been marked as expired or stale.

- **Type parameter `T`**: The expected type of the cached or fetched response.
- **Returns**: A `Task<T?>` that resolves to the cached value, the freshly fetched value, or a stale cached value when the fetch fails. Returns `null` if no cached value exists and the fetch fails.
- **Throws**: Does not throw when the fetch operation fails and a stale cache entry is available. Throws only when no fallback entry exists and the fetch operation fails, or when the cache infrastructure itself raises an unrecoverable error.

### `public async Task InvalidateAsync`

Removes the cached entry associated with the current request key, forcing the next call to `GetOrFetchAsync` or `GetOrFetchWithFallbackAsync` to perform a fresh fetch.

- **Returns**: A `Task` that completes when the invalidation operation has been executed.
- **Throws**: May throw if the cache infrastructure encounters an error during removal.

## Usage

### Example 1: Basic cache-aside with explicit invalidation

```csharp
var decorator = new RequestCachingDecorator(cache, requestExecutor);

// First call fetches from downstream and caches the result.
var product = await decorator.GetOrFetchAsync<Product>(productRequest);

// Subsequent calls return the cached value without hitting the downstream service.
var sameProduct = await decorator.GetOrFetchAsync<Product>(productRequest);

// After an update, invalidate the cache so the next read fetches fresh data.
await decorator.InvalidateAsync(productRequest);
var updatedProduct = await decorator.GetOrFetchAsync<Product>(productRequest);
```

### Example 2: Resilient reads with stale fallback

```csharp
var decorator = new RequestCachingDecorator(cache, requestExecutor);

// Populate the cache with an initial successful fetch.
var config = await decorator.GetOrFetchAsync<AppConfig>(configRequest);

// Later, if the downstream service is unavailable, fall back to the stale cached value.
var fallbackConfig = await decorator.GetOrFetchWithFallbackAsync<AppConfig>(configRequest);
if (fallbackConfig is null)
{
    // No cache entry existed at all; handle total unavailability.
    logger.LogError("Configuration unavailable and no fallback present.");
}
else
{
    // Operate with the stale configuration until the downstream recovers.
    ApplyConfiguration(fallbackConfig);
}
```

## Notes

- **Cache key derivation**: The decorator derives cache keys from the request object passed to each method. Two requests that compare as equal by the decorator’s key-generation logic will share the same cache entry. Ensure request objects implement value equality if custom keying is not provided.
- **Stale entry semantics**: `GetOrFetchWithFallbackAsync` treats entries that have exceeded their configured time-to-live as valid fallback sources. This means a call may return data that is technically expired, which is intentional for resilience scenarios.
- **Thread safety**: The decorator itself does not guarantee atomicity across multiple callers. Concurrent invocations of `GetOrFetchAsync` for the same uncached key may both execute the fetch operation. If exactly-once fetch semantics are required, additional synchronization must be layered externally.
- **Null as a legitimate cached value**: If `T` is a reference type and the downstream service returns `null`, that `null` may be stored in the cache. Subsequent calls to `GetOrFetchAsync` will return `null` without re-fetching. `GetOrFetchWithFallbackAsync` may return `null` either from a cached `null` or from a total absence of any cache entry when the fetch fails; the caller must distinguish these cases if needed.
- **Invalidation scope**: `InvalidateAsync` removes only the entry matching the exact request key provided. It does not affect other cached entries, even if they are logically related.
- **Exception propagation**: When `GetOrFetchAsync` throws, no value is cached for that request. The next call will attempt a fresh fetch. When `GetOrFetchWithFallbackAsync` throws (because no fallback exists), the same behavior applies.
