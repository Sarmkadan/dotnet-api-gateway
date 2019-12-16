# CacheService

A thread-safe in-memory caching service for HTTP responses, designed to reduce backend load and latency in the API gateway by storing responses with configurable expiration and automatic cleanup of stale entries.

## API

### `CacheService`

Initializes a new instance of the `CacheService` with default or provided options.

### `bool TryGetCachedResponse(string key, out string? responseBody, out int statusCode, out Dictionary<string, string> headers, out DateTime expiresAt)`

Attempts to retrieve a cached response by its key. Returns `true` if the response exists and has not expired; otherwise, returns `false`.

- **Parameters**
  - `key`: The unique identifier for the cached response.
  - `responseBody`: Output parameter containing the cached response body, if found.
  - `statusCode`: Output parameter containing the HTTP status code of the cached response, if found.
  - `headers`: Output parameter containing the headers of the cached response, if found.
  - `expiresAt`: Output parameter containing the expiration timestamp of the cached response, if found.
- **Returns**
  - `true` if the response is found and valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.

### `void SetCachedResponse(string key, string responseBody, int statusCode, Dictionary<string, string> headers, DateTime expiresAt)`

Stores a response in the cache with the specified key and metadata.

- **Parameters**
  - `key`: The unique identifier for the cached response.
  - `responseBody`: The body of the response to cache.
  - `statusCode`: The HTTP status code of the response.
  - `headers`: The headers of the response.
  - `expiresAt`: The timestamp at which the response should expire.
- **Throws**
  - `ArgumentNullException` if `key`, `responseBody`, or `headers` is `null`.
  - `ArgumentException` if `expiresAt` is in the past.

### `void InvalidateCache(string key)`

Removes a specific cached response by its key.

- **Parameters**
  - `key`: The unique identifier for the cached response to remove.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.

### `void InvalidateCacheByPrefix(string prefix)`

Removes all cached responses whose keys start with the specified prefix.

- **Parameters**
  - `prefix`: The string prefix to match against cached keys.
- **Throws**
  - `ArgumentNullException` if `prefix` is `null`.

### `CacheStatistics GetStatistics()`

Returns a snapshot of cache usage statistics, including total entries, hits, misses, and memory footprint.

- **Returns**
  - A `CacheStatistics` object containing current cache metrics.

### `Task<int> RemoveExpiredEntriesAsync()`

Asynchronously removes all expired entries from the cache and returns the count of removed entries.

- **Returns**
  - The number of entries removed.

### `Task<T?> GetAsync<T>(string key)`

Asynchronously retrieves a cached value of type `T` by its key.

- **Type Parameters**
  - `T`: The type of the cached value.
- **Parameters**
  - `key`: The unique identifier for the cached value.
- **Returns**
  - The cached value if found and valid; otherwise, `null`.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.
  - `InvalidOperationException` if the cached value cannot be deserialized to type `T`.

### `Task SetAsync<T>(string key, T value, DateTime expiresAt)`

Asynchronously stores a value of type `T` in the cache.

- **Type Parameters**
  - `T`: The type of the value to cache.
- **Parameters**
  - `key`: The unique identifier for the cached value.
  - `value`: The value to cache.
  - `expiresAt`: The timestamp at which the value should expire.
- **Throws**
  - `ArgumentNullException` if `key` or `value` is `null`.
  - `ArgumentException` if `expiresAt` is in the past.

### `Task InvalidatePrefixAsync(string prefix)`

Asynchronously removes all cached entries whose keys start with the specified prefix.

- **Parameters**
  - `prefix`: The string prefix to match against cached keys.
- **Throws**
  - `ArgumentNullException` if `prefix` is `null`.

### `void ClearAll()`

Removes all entries from the cache.

### `void Dispose()`

Releases all resources used by the `CacheService`.

### Properties

#### `string Key`

Gets the key of the current cached entry (used internally for tracking).

#### `int StatusCode`

Gets the HTTP status code of the current cached entry (used internally for tracking).

#### `string ResponseBody`

Gets the body of the current cached entry (used internally for tracking).

#### `Dictionary<string, string> Headers`

Gets the headers of the current cached entry (used internally for tracking).

#### `DateTime ExpiresAt`

Gets the expiration timestamp of the current cached entry (used internally for tracking).

#### `DateTime CachedAt`

Gets the timestamp when the current cached entry was stored (used internally for tracking).

#### `DateTime LastAccessAt`

Gets the timestamp when the current cached entry was last accessed (used internally for tracking).

#### `long HitCount`

Gets the number of times the current cached entry has been accessed (used internally for tracking).

## Usage

### Example 1: Caching an HTTP Response
