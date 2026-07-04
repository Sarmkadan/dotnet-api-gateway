#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: Request/Response Caching Strategy
/// Demonstrates caching configuration and performance benefits.
/// </summary>
public sealed class CachingExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - Caching Example ===\n");

        // Step 1: Caching benefits
        Console.WriteLine("Step 1: Caching Benefits");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Performance Improvement Example:

Without Caching:
  Request 1 → Backend (100ms) → Response
  Request 2 → Backend (100ms) → Response  (same endpoint)
  Request 3 → Backend (100ms) → Response  (same endpoint)
  Total: 300ms for 3 identical requests

With Caching (TTL=5 minutes):
  Request 1 → Backend (100ms) → Cache stored → Response
  Request 2 → Cache (1ms) → Response       (instant!)
  Request 3 → Cache (1ms) → Response       (instant!)
  Total: 102ms for 3 requests = 99% latency improvement

Cost: Only need to talk to backend once per 5 minutes!
");

        // Step 2: Configuration
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 2: Caching Configuration");
        Console.WriteLine(new string('-', 60));

        var config = @"
{
  ""DotnetApiGateway"": {
    ""Routes"": [
      {
        ""name"": ""public-data"",
        ""pattern"": ""^/api/data(/.*)?$"",
        ""method"": ""GET"",
        ""cachingPolicy"": {
          ""enabled"": true,
          ""ttlSeconds"": 600,
          ""cacheKeyPattern"": ""{method}:{path}:{querystring}"",
          ""conditionalCache"": true
        },
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      },
      {
        ""name"": ""user-profile"",
        ""pattern"": ""^/api/users/[0-9]+/profile$"",
        ""method"": ""GET"",
        ""cachingPolicy"": {
          ""enabled"": true,
          ""ttlSeconds"": 300
        },
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      },
      {
        ""name"": ""real-time-data"",
        ""pattern"": ""^/api/realtime(/.*)?$"",
        ""cachingPolicy"": {
          ""enabled"": false
        },
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      }
    ]
  }
}";

        Console.WriteLine(config);
        Console.WriteLine();

        // Step 3: Cache key patterns
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 3: Cache Key Pattern Examples");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Pattern: {method}:{path}:{querystring}

Examples:
  GET /api/users
    → Key: GET:/api/users:

  GET /api/users/123
    → Key: GET:/api/users/123:

  GET /api/users/123?expand=true
    → Key: GET:/api/users/123:expand=true

  POST /api/users
    → Not cached (POST modifies state)

  GET /api/users?page=1&limit=10
    → Key: GET:/api/users:page=1&limit=10
    → Different key for page=2&limit=10

Cache Effectiveness:
  ✓ High: Repeated requests to same URL (user list)
  ✗ Low: Each request to different URL (search queries)
");

        // Step 4: Simulation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 4: Simulating Cache Behavior");
        Console.WriteLine(new string('-', 60));

        var cache = new SimpleResponseCache(ttlSeconds: 10);

        Console.WriteLine("\nSimulating requests with caching (TTL=10s):");
        Console.WriteLine();

        var requests = new[]
        {
            ("GET", "/api/data", ""),
            ("GET", "/api/data", ""),      // Same as previous - cache hit
            ("GET", "/api/data/1", ""),
            ("GET", "/api/data", ""),      // Same as first - cache hit
            ("GET", "/api/data", "page=2"),
            ("GET", "/api/data", "page=2") // Same as previous - cache hit
        };

        int hitCount = 0;
        for (int i = 0; i < requests.Length; i++)
        {
            var (method, path, query) = requests[i];
            var key = GenerateCacheKey(method, path, query);
            var stopwatch = Stopwatch.StartNew();

            var result = cache.TryGetCached(key, out var cachedResponse);
            stopwatch.Stop();

            if (result)
            {
                hitCount++;
                Console.WriteLine($"Request {i + 1}: {method} {path}?{query}");
                Console.WriteLine($"  ✓ Cache HIT ({stopwatch.Elapsed.TotalMilliseconds:F2}ms)");
                Console.WriteLine($"  Key: {key}");
            }
            else
            {
                // Simulate backend call
                Task.Delay(100).Wait();
                cache.Cache(key, new object());

                Console.WriteLine($"Request {i + 1}: {method} {path}?{query}");
                Console.WriteLine($"  ✗ Cache MISS (100ms backend)");
                Console.WriteLine($"  → Cached for TTL");
                Console.WriteLine($"  Key: {key}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Cache Statistics: {hitCount} hits / {requests.Length} total = {(hitCount * 100 / requests.Length)}% hit rate");
        Console.WriteLine();

        // Step 5: TTL expiration
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 5: TTL Expiration Behavior");
        Console.WriteLine(new string('-', 60));

        var expireCache = new SimpleResponseCache(ttlSeconds: 3);

        Console.WriteLine("\nSimulating cache expiration (TTL=3s):");
        Console.WriteLine();

        var key = GenerateCacheKey("GET", "/api/data", "");
        Console.WriteLine($"T=0s:   Request → Backend (100ms) → Cached");
        expireCache.Cache(key, new object());

        Console.WriteLine($"T=1s:   Request → Cache hit (instant)");
        _ = expireCache.TryGetCached(key, out _);

        Console.WriteLine($"T=2s:   Request → Cache hit (instant)");
        _ = expireCache.TryGetCached(key, out _);

        Console.WriteLine($"T=3.5s: Request → Cache expired → Backend (100ms) → Cached again");
        Console.WriteLine();

        // Step 6: When to cache
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 6: When to Cache - Decision Matrix");
        Console.WriteLine(new string('-', 60));

        var cacheDecisions = new[]
        {
            ("GET /api/users", "✓ YES", "Stable data, frequently accessed"),
            ("GET /api/search?q=test", "⚠ MAYBE", "Results may be ephemeral"),
            ("GET /api/realtime-prices", "✗ NO", "Changes frequently"),
            ("POST /api/users", "✗ NO", "Creates/modifies state"),
            ("PUT /api/users/1", "✗ NO", "Modifies state"),
            ("DELETE /api/users/1", "✗ NO", "Deletes data"),
            ("GET /health", "⚠ MAYBE", "For load balancer checks"),
            ("GET /api/config", "✓ YES", "Usually stable"),
        };

        Console.WriteLine();
        foreach (var (endpoint, shouldCache, reason) in cacheDecisions)
        {
            Console.WriteLine($"{shouldCache:8} {endpoint:30} → {reason}");
        }

        Console.WriteLine();

        // Step 7: Cache invalidation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 7: Cache Invalidation Strategies");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
1. Time-Based Expiration (TTL) - Default
   - Cache expires after fixed duration
   - Simple, no coordination needed
   - Risk: Serving stale data for up to TTL duration

2. Event-Based Invalidation
   - Invalidate when related data changes
   - E.g., POST /users → invalidate user list cache
   - More complex, requires tracking dependencies

3. Manual Invalidation
   - Admin triggers cache clear
   - Use: After data migrations, urgent fixes
   - Command: curl -X POST http://localhost:5000/api/cache/clear

4. Conditional Caching
   - Use HTTP cache headers (ETag, Last-Modified)
   - Return 304 Not Modified if unchanged
   - Reduces bandwidth but still requires backend call

Best Practice:
  - Use TTL for most data (simple, works well)
  - Use shorter TTL for changing data
  - Use longer TTL for stable data
  - Combine with event-based for critical updates
");

        // Step 8: Monitoring cache
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 8: Monitoring Cache Performance");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Check cache metrics:

  curl http://localhost:5000/api/metrics | jq '.cacheMetrics'

Response:
{
  ""totalCacheHits"": 1000,
  ""totalCacheMisses"": 200,
  ""cacheHitRate"": 0.833,
  ""cachedResponses"": 45,
  ""cacheMemoryMB"": 2.5,
  ""cacheEvictions"": 10
}

Optimize cache by:
  - Increase TTL for stable data
  - Decrease TTL for changing data
  - Monitor hit rate (target: >80%)
  - Watch memory usage
  - Adjust cache size limits
");

        // Step 9: Best practices
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 9: Caching Best Practices");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
✓ Do's:
  - Cache GET requests only
  - Use appropriate TTL (not too short, not too long)
  - Monitor cache hit rates
  - Plan cache invalidation strategy
  - Cache stable/reference data
  - Use cache headers for client-side caching

✗ Don'ts:
  - Cache requests with side effects (POST, PUT, DELETE)
  - Cache user-specific data without key differentiation
  - Set TTL too high (serve very stale data)
  - Set TTL too low (cache becomes pointless)
  - Ignore cache size limits (memory exhaustion)
  - Cache sensitive data
");

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private static string GenerateCacheKey(string method, string path, string query)
        => $"{method}:{path}:{query}";

    private class SimpleResponseCache
    {
        private readonly int ttlSeconds;
        private readonly Dictionary<string, (object response, DateTime expireAt)> cache;

        public SimpleResponseCache(int ttlSeconds)
        {
            this.ttlSeconds = ttlSeconds;
            cache = new Dictionary<string, (object, DateTime)>();
        }

        public bool TryGetCached(string key, out object? response)
        {
            lock (cache)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    if (DateTime.UtcNow < entry.expireAt)
                    {
                        response = entry.response;
                        return true;
                    }
                    cache.Remove(key);
                }
                response = null;
                return false;
            }
        }

        public void Cache(string key, object response)
        {
            lock (cache)
            {
                cache[key] = (response, DateTime.UtcNow.AddSeconds(ttlSeconds));
            }
        }
    }
}

/// <summary>
/// To run this example:
///
/// 1. Enable caching in route configuration:
///
///    "cachingPolicy": {
///      "enabled": true,
///      "ttlSeconds": 300
///    }
///
/// 2. Test caching:
///
///    # First request (cache miss)
///    time curl http://localhost:5000/api/data
///
///    # Subsequent requests (cache hit)
///    for i in {1..5}; do
///      time curl http://localhost:5000/api/data
///    done
///
/// 3. Monitor cache performance:
///
///    curl http://localhost:5000/api/metrics | jq '.cacheMetrics'
///
/// Expected Results:
/// - First request: ~100ms (backend latency)
/// - Cached requests: ~1-5ms (cache lookup)
/// - Hit rate should be >80% for stable endpoints
/// </summary>
