# Response caching

This page describes the caching components in the gateway: the route-level `CachePolicy`, the in-memory `CacheService`, the `RequestCachingDecorator` helper, and the background cleanup. For configuration keys, see [configuration-reference.md](configuration-reference.md#cache-policy). For the request pipeline, see [ARCHITECTURE.md](ARCHITECTURE.md). If this page and the code disagree, the code wins.

## Components

| Component | File | Role |
|---|---|---|
| `CachePolicy` | `Models/CachePolicy.cs` | Per-route settings: enable flag, TTL, strategy, cacheable status codes and methods, cache key variation. |
| `CacheService` | `Services/CacheService.cs` | Singleton in-memory store backed by a `ConcurrentDictionary<string, CacheEntry>`. Registered in `Configuration/ServiceCollectionExtensions.cs`. |
| `CacheEntry` | `Services/CacheService.cs` | Cached value with `ExpiresAt`, hit count, and `LastAccessAt`. `IsExpired()` compares against `DateTime.UtcNow`. |
| `RequestCachingDecorator` | `Services/RequestCachingDecorator.cs` | Get-or-fetch helper over `CacheService`, including a stale-on-error variant and prefix invalidation. |
| `CacheCleanupWorker` | `BackgroundServices/CacheCleanupWorker.cs` | Hosted service that calls `RemoveExpiredEntriesAsync()` on a fixed interval. |

## CachePolicy

`CachePolicy` is set under `"CachePolicy"` inside a `GatewayRoute`. Defaults are taken from the model:

- `Enabled`: `false`
- `Strategy`: `CacheControl`
- `CacheableStatusCodes`: `["200"]`
- `CacheableHttpMethods`: `["GET", "HEAD"]`
- `VaryByQueryString`: `true`
- `VaryByHeaders`: `false`, with `VaryHeaders` as the list of header names used when enabled
- `MaxEntriesInCache`: `1000` (valid range 1 to 10000)

`ConfigurationValidator.ValidateCachePolicy` rejects a TTL of 0 or less.

Only `GET` and `HEAD` are cacheable by default. Do not add methods with side effects.

## CacheService

`CacheService` keeps everything in process memory. Entries are stored with an absolute expiry (`ExpiresAt = UtcNow + durationSeconds`).

- `TryGetCachedResponse(key, out entry)` returns a hit only if the entry has not expired. An expired entry is removed on read. A hit increments `HitCount` and updates `LastAccessAt`.
- `SetCachedResponse(key, statusCode, body, headers, durationSeconds)` stores a raw HTTP-style response (status, body, headers).
- `GetAsync<T>` and `SetAsync<T>` store JSON-serialized values, with `SetAsync` taking a `TimeSpan`.
- `InvalidateCache(key)`, `InvalidateCacheByPrefix(prefix)`, and `InvalidatePrefixAsync(prefix)` remove entries. Prefix invalidation is a simple key-prefix match.
- `GetStatistics()`, `GetSizeBytes()`, `GetHitRate()`, and `GetAverageSizePerEntryBytes()` expose basic metrics.

Concurrency: lookups take an upgradeable read lock on a `ReaderWriterLockSlim`, and take the write lock only when the entry needs to be mutated (hit counters, or removal when expired).

Because the store is in process, each gateway instance has its own cache. Multiple replicas do not share entries, and a restart empties the cache.

## Expiry and cleanup

Two independent timers remove expired entries:

| Mechanism | Interval | Notes |
|---|---|---|
| `System.Threading.Timer` inside `CacheService` | first run after 5 min, then every 5 min | Started in the `CacheService` constructor. |
| `CacheCleanupWorker` (hosted service) | every 10 min | Logs the count removed and elapsed time at Information level when entries are removed. |

Expired entries are also dropped on read, so cleanup timers only bound memory held by entries nobody requests again.

## RequestCachingDecorator

The decorator is a thin helper over `CacheService`:

- `GetOrFetchAsync<T>(key, fetch, ttl)` returns the cached value on a hit. On a miss it calls `fetch`, caches a non-null result if `ttl` is provided, and returns the result. A null or whitespace key bypasses the cache.
- `GetOrFetchWithFallbackAsync<T>(key, fetch, ttl, staleTtl)` works the same way, with one difference: if `fetch` throws, it returns the value stored under `{key}:stale`. If no stale value exists, the exception is rethrown. The stale copy is written by the caller; the decorator does not write it on success, and it refreshes the stale copy's TTL only when it serves one.
- `InvalidateAsync(prefix)` delegates to `InvalidatePrefixAsync`.

Note: in the current code, neither the decorator nor `CachePolicy` is consumed by the request pipeline. `CachePolicy` is referenced only by `Models/GatewayRoute.cs` and `Configuration/ConfigurationValidator.cs`, and the decorator only by its own file and extensions. Setting `CachePolicy.Enabled = true` on a route therefore does not currently change responses. Verify with a grep before relying on route-level caching.

## Operational notes

- Once the pipeline is wired, caching will apply only to responses that match `CacheableStatusCodes` and `CacheableHttpMethods`.
- Keep `DurationSeconds` short for data that changes often. The default is 300 seconds.
- Watch memory: `MaxEntriesInCache` bounds entries per policy, but entry size is not bounded. Check `GetSizeBytes()` on large bodies.
- After a deploy or config change that affects response shape, flush the cache by restarting the instance or invalidating the affected prefix.
