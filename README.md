// ... (rest of the file remains the same)

## ConditionalAggregationTargetExtensions

The `ConditionalAggregationTargetExtensions` class provides a set of extension methods for creating and customizing conditional aggregation targets. These targets are used to dynamically route requests to different backend services based on specific conditions, such as headers, JSON body, HTTP method, and timeout.

The following example demonstrates how to use these extensions to create a conditional aggregation target:

```csharp
var target = ConditionalAggregationTarget.WithHeader("*", "accept", "application/json")
    .WithJsonBody("product", "electronics")
    .WithMethod(HttpMethod.Get)
    .WithTimeout(TimeSpan.FromSeconds(10));

bool shouldUse = ConditionalAggregationTarget.ShouldUse(target, new HttpContext());

ConditionalAggregationTarget validatedTarget = ConditionalAggregationTarget.ValidateWithDetails(target);

// Clone the target for reuse
var clonedTarget = validatedTarget.Clone();
```

## CachingExampleExtensions

`CachingExampleExtensions` offers a set of helper methods for working with the in‑memory `SimpleResponseCache` used in the examples. It simplifies creating cache keys, retrieving or creating cached values (both synchronously and asynchronously), clearing the cache, and inspecting cache statistics.

```csharp
using System;
using System.Threading.Tasks;

// Create a cache instance that can be shared across calls
var cache = CachingExampleExtensions.CreateCache();

// Build a deterministic cache key for a request
string userKey = CachingExampleExtensions.CreateCacheKey("user", "123");

// Synchronously get a cached value or create it if it does not exist
var user = CachingExampleExtensions.GetOrCreate(
    cache,
    userKey,
    () => new User { Id = 123, Name = "Alice" });

// Asynchronously get a cached value or create it if it does not exist
var product = await CachingExampleExtensions.GetOrCreateAsync<Product>(
    cache,
    CachingExampleExtensions.CreateCacheKey("product", "456"),
    async () =>
    {
        // Simulate an async operation, e.g., a database call
        await Task.Delay(50);
        return new Product { Id = 456, Name = "Gadget" };
    });

// Retrieve cache statistics (hits, misses, entry count, etc.)
CacheStatistics stats = CachingExampleExtensions.GetStatistics(cache);
Console.WriteLine($"Cache entries: {stats.EntryCount}, Hits: {stats.HitCount}");

// Clear all cached entries; the method returns the number of removed items
int removed = CachingExampleExtensions.ClearAll(cache);
Console.WriteLine($"{removed} cache entries were cleared");

// The underlying cache also exposes the current entry count directly
Console.WriteLine($"Current entry count via property: {cache.EntryCount}");
```

## ... (rest of the file remains the same)
