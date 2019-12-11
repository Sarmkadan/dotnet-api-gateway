# CachingExample

Demonstrates basic response caching patterns using a simple in-memory cache implementation for API Gateway scenarios.

## API

### `public static async Task Main(string[] args)`

Application entry point that runs the caching demonstration.

**Parameters**
- `args` — Command-line arguments (unused).

**Returns**
- `Task` — Completes when the demonstration finishes.

**Throws**
- `Exception` — Propagates any unhandled exception from the demonstration logic.

---

### `public SimpleResponseCache SimpleResponseCache`

Gets the underlying cache instance used by the example.

**Returns**
- `SimpleResponseCache` — The cache implementation storing keyed responses.

---

### `public bool TryGetCached(string key, out object value)`

Attempts to retrieve a cached value by key.

**Parameters**
- `key` — The cache key to look up.
- `value` — When the method returns `true`, contains the cached object; otherwise `null`.

**Returns**
- `bool` — `true` if the key was found and `value` is set; `false` otherwise.

**Throws**
- `ArgumentNullException` — If `key` is `null`.

---

### `public void Cache(string key, object value)`

Stores a value in the cache under the specified key.

**Parameters**
- `key` — The cache key. Must not be `null`.
- `value` — The object to cache. Can be `null` to represent a cached null value.

**Throws**
- `ArgumentNullException` — If `key` is `null`.

## Usage

### Basic cache interaction

```csharp
var example = new CachingExample();

// Store a response
example.Cache("user:123", new { Id = 123, Name = "Alice" });

// Retrieve it
if (example.TryGetCached("user:123", out var cached))
{
    Console.WriteLine($"Cache hit: {cached}");
}
else
{
    Console.WriteLine("Cache miss");
}
```

### Using the exposed cache instance directly

```csharp
var example = new CachingExample();
var cache = example.SimpleResponseCache;

// The cache instance can be used independently
cache.Set("config:theme", "dark");
var theme = cache.Get<string>("config:theme");
```

## Notes

- **Thread safety**: `SimpleResponseCache` is not documented as thread-safe. Concurrent calls to `Cache` and `TryGetCached` from multiple threads may result in race conditions or corrupted state. Synchronize externally if shared across threads.
- **Null keys**: Both `Cache` and `TryGetCached` throw `ArgumentNullException` for `null` keys. Validate inputs before calling.
- **Null values**: `Cache` accepts `null` as a value, allowing explicit caching of null results. `TryGetCached` returns `true` with `value` set to `null` in this case.
- **Eviction**: The example does not demonstrate expiration or eviction policies. The cache grows indefinitely until the process ends or the instance is discarded.
- **Main method**: The static `Main` is intended for standalone execution of the example. It is not required when using `CachingExample` as a library component.
