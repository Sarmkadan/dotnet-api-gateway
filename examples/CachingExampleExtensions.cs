#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Extension methods for CachingExample that provide practical caching utilities
/// for API gateway scenarios.
/// </summary>
public static class CachingExampleExtensions
{
    /// <summary>
    /// Creates a cache key from HTTP request components for consistent caching.
    /// </summary>
    /// <param name="method">HTTP method (GET, POST, etc.)</param>
    /// <param name="path">Request path</param>
    /// <param name="queryString">Query string parameters</param>
    /// <param name="headers">Optional request headers to include in cache key</param>
    /// <returns>Cache key string</returns>
    public static string CreateCacheKey(
        this CachingExample _,
        string method,
        string path,
        string? queryString = null,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Method cannot be null or empty", nameof(method));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var key = $"{method.ToUpperInvariant()}:{path.TrimEnd('/')}:{queryString?.Trim() ?? string.Empty}";

        // Include headers in cache key if provided
        if (headers != null && headers.Count > 0)
        {
            foreach (var header in headers)
            {
                key += $":{header.Key}={header.Value}";
            }
        }

        return key;
    }

    /// <summary>
    /// Creates a cache key from an HTTP request message for API gateway scenarios.
    /// </summary>
    /// <param name="request">HTTP request message</param>
    /// <param name="includeHeaders">Optional headers to include in cache key</param>
    /// <returns>Cache key string</returns>
    public static string CreateCacheKey(
        this CachingExample _,
        System.Net.Http.HttpRequestMessage request,
        bool includeHeaders = false)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var method = request.Method.ToString().ToUpperInvariant();
        var path = request.RequestUri?.AbsolutePath ?? "/";
        var query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;

        var key = $"{method}:{path}:{query}";

        if (includeHeaders)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
            {
                if (header.Value != null)
                {
                    headers[header.Key] = string.Join(",", header.Value);
                }
            }

            foreach (var header in headers)
            {
                key += $":{header.Key}={header.Value}";
            }
        }

        return key;
    }

    /// <summary>
    /// Gets a cached value or executes a fallback function if not cached.
    /// Uses the cache's configured TTL.
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="cache">Cache instance</param>
    /// <param name="key">Cache key</param>
    /// <param name="fallback">Function to execute if value not in cache</param>
    /// <returns>Cached or newly computed value</returns>
    public static async Task<T> GetOrCreateAsync<T>(
        this CachingExample _,
        SimpleResponseCache cache,
        string key,
        Func<Task<T>> fallback)
    {
        if (cache.TryGetCached(key, out var cachedValue) && cachedValue is T cachedResult)
        {
            return cachedResult;
        }

        var newValue = await fallback().ConfigureAwait(false);
        cache.Cache(key, newValue);

        return newValue;
    }

    /// <summary>
    /// Gets a cached value or executes a fallback function if not cached.
    /// Uses the cache's configured TTL.
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="cache">Cache instance</param>
    /// <param name="key">Cache key</param>
    /// <param name="fallback">Function to execute if value not in cache</param>
    /// <returns>Cached or newly computed value</returns>
    public static T GetOrCreate<T>(
        this CachingExample _,
        SimpleResponseCache cache,
        string key,
        Func<T> fallback)
    {
        if (cache.TryGetCached(key, out var cachedValue) && cachedValue is T cachedResult)
        {
            return cachedResult;
        }

        var newValue = fallback();
        cache.Cache(key, newValue);

        return newValue;
    }

    /// <summary>
    /// Creates a new SimpleResponseCache instance with specified TTL.
    /// </summary>
    /// <param name="ttlSeconds">Time to live in seconds</param>
    /// <returns>New cache instance</returns>
    public static SimpleResponseCache CreateCache(this CachingExample _, int ttlSeconds)
    {
        return new SimpleResponseCache(ttlSeconds);
    }

    /// <summary>
    /// Clears all cached entries from the cache.
    /// Note: Uses reflection to access internal cache dictionary.
    /// </summary>
    /// <param name="cache">Cache instance</param>
    /// <returns>Number of entries removed</returns>
    public static int ClearAll(this CachingExample _, SimpleResponseCache cache)
    {
        // Use reflection to access internal cache dictionary
        var cacheField = typeof(SimpleResponseCache).GetField(
            "cache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (cacheField != null)
        {
            var cacheDict = cacheField.GetValue(cache) as System.Collections.IDictionary;
            if (cacheDict != null)
            {
                int count = cacheDict.Count;
                cacheDict.Clear();
                return count;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets cache statistics for monitoring purposes.
    /// Note: Uses reflection to access internal cache state.
    /// </summary>
    /// <param name="cache">Cache instance</param>
    /// <returns>Cache statistics</returns>
    public static CacheStatistics GetStatistics(this CachingExample _, SimpleResponseCache cache)
    {
        var stats = new CacheStatistics();

        // Use reflection to access cache internals
        var cacheField = typeof(SimpleResponseCache).GetField(
            "cache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (cacheField != null)
        {
            var cacheDict = cacheField.GetValue(cache) as System.Collections.IDictionary;
            if (cacheDict != null)
            {
                stats.EntryCount = cacheDict.Count;
            }
        }

        return stats;
    }

    /// <summary>
    /// Cache statistics container.
    /// </summary>
    public class CacheStatistics
    {
        public int EntryCount { get; set; }
    }
}