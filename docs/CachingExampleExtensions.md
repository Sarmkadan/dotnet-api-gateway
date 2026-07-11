# CachingExampleExtensions

Provides extension methods and utilities for caching responses in ASP.NET Core applications, including key generation, cache retrieval, and statistics tracking.

## API

### `CreateCacheKey(params object?[] parts)`
Generates a consistent cache key from the provided parts. The parts are combined using a delimiter to form a unique string key.

**Parameters:**
- `parts`: Variable number of objects used to construct the cache key. Any `null` parts are treated as empty strings.

**Returns:**
- A string representing the combined cache key.

**Throws:**
- `ArgumentNullException`: If `parts` is `null`.

---

### `CreateCacheKey(IEnumerable<object?> parts)`
Generates a consistent cache key from the provided parts. The parts are combined using a delimiter to form a unique string key.

**Parameters:**
- `parts`: An enumerable of objects used to construct the cache key. Any `null` parts are treated as empty strings.

**Returns:**
- A string representing the combined cache key.

**Throws:**
- `ArgumentNullException`: If `parts` is `null`.

---

### `GetOrCreateAsync<T>(this IMemoryCache cache, string key, Func<ICacheEntry, Task<T>> factory)`
Retrieves a value from the cache or creates and caches it if it does not exist. The factory is invoked only when the key is missing.

**Parameters:**
- `cache`: The `IMemoryCache` instance to use for caching.
- `key`: The cache key to look up.
- `factory`: An asynchronous function that creates the value to cache if the key is missing.

**Returns:**
- A `Task<T>` representing the cached or newly created value.

**Throws:**
- `ArgumentNullException`: If `cache`, `key`, or `factory` is `null`.

---

### `GetOrCreate<T>(this IMemoryCache cache, string key, Func<ICacheEntry, T> factory)`
Retrieves a value from the cache or creates and caches it if it does not exist. The factory is invoked only when the key is missing.

**Parameters:**
- `cache`: The `IMemoryCache` instance to use for caching.
- `key`: The cache key to look up.
- `factory`: A synchronous function that creates the value to cache if the key is missing.

**Returns:**
- The cached or newly created value.

**Throws:**
- `ArgumentNullException`: If `cache`, `key`, or `factory` is `null`.

---
### `CreateCache(this IServiceProvider services)`
Creates and configures a new `SimpleResponseCache` instance using the provided service provider.

**Parameters:**
- `services`: The `IServiceProvider` used to resolve required services.

**Returns:**
- A new `SimpleResponseCache` instance.

**Throws:**
- `ArgumentNullException`: If `services` is `null`.
- `InvalidOperationException`: If required services are not registered.

---
### `ClearAll(this SimpleResponseCache cache)`
Removes all entries from the cache.

**Parameters:**
- `cache`: The `SimpleResponseCache` instance to clear.

**Returns:**
- The number of entries removed.

**Throws:**
- `ArgumentNullException`: If `cache` is `null`.

---
### `GetStatistics(this SimpleResponseCache cache)`
Retrieves statistics about the cache, including entry count and hit/miss metrics.

**Parameters:**
- `cache`: The `SimpleResponseCache` instance to inspect.

**Returns:**
- A `CacheStatistics` object containing cache metrics.

**Throws:**
- `ArgumentNullException`: If `cache` is `null`.

---
### `EntryCount` (property of `SimpleResponseCache`)
Gets the current number of entries in the cache.

**Returns:**
- The number of cached entries.

---
### `SimpleResponseCache`
A simple in-memory cache implementation for storing responses with statistics tracking.

## Usage

### Basic Caching with Key Generation
