#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Extension methods for <see cref="RequestCachingDecorator"/> providing convenient
/// caching operations with common patterns and batch operations.
/// </summary>
public static class RequestCachingDecoratorExtensions
{
    /// <summary>
    /// Get or fetch with automatic cache key generation from request parameters.
    /// </summary>
    /// <typeparam name="T">The type of value being cached.</typeparam>
    /// <param name="keyGenerator">Function that generates cache key from key parts.</param>
    /// <param name="fetchFunc">Function to fetch value if not in cache.</param>
    /// <param name="cacheDuration">Optional cache duration. If null, result is not cached.</param>
    /// <param name="keyParts">Parts used to generate the cache key.</param>
    /// <returns>The cached or fetched value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keyGenerator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="fetchFunc"/> is <see langword="null"/>.</exception>
    public static async Task<T> GetOrFetchAsync<T>(
        this RequestCachingDecorator decorator,
        Func<object[], string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null,
        params object[] keyParts) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(fetchFunc);

        var cacheKey = keyGenerator(keyParts);
        return await decorator.GetOrFetchAsync(cacheKey, fetchFunc, cacheDuration);
    }

    /// <summary>
    /// Get or fetch with fallback, automatically generating cache keys from request parameters.
    /// </summary>
    /// <typeparam name="T">The type of value being cached.</typeparam>
    /// <param name="keyGenerator">Function that generates cache key from key parts.</param>
    /// <param name="fetchFunc">Function to fetch value if not in cache.</param>
    /// <param name="cacheDuration">Optional cache duration for fresh data. If null, result is not cached.</param>
    /// <param name="staleCacheTtl">Optional TTL for stale cache fallback.</param>
    /// <param name="keyParts">Parts used to generate the cache key.</param>
    /// <returns>The cached, fetched, or stale cached value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keyGenerator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="fetchFunc"/> is <see langword="null"/>.</exception>
    public static async Task<T?> GetOrFetchWithFallbackAsync<T>(
        this RequestCachingDecorator decorator,
        Func<object[], string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null,
        TimeSpan? staleCacheTtl = null,
        params object[] keyParts) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(fetchFunc);

        var cacheKey = keyGenerator(keyParts);
        return await decorator.GetOrFetchWithFallbackAsync(
            cacheKey,
            fetchFunc,
            cacheDuration,
            staleCacheTtl);
    }

    /// <summary>
    /// Invalidate multiple cache keys by pattern with support for wildcard patterns.
    /// </summary>
    /// <param name="keyPatterns">Patterns identifying cache entries to invalidate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keyPatterns"/> is <see langword="null"/>.</exception>
    public static async Task InvalidateMultipleAsync(
        this RequestCachingDecorator decorator,
        params string[] keyPatterns)
    {
        ArgumentNullException.ThrowIfNull(keyPatterns);

        foreach (var pattern in keyPatterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                await decorator.InvalidateAsync(pattern);
            }
        }
    }

    /// <summary>
    /// Get or fetch with typed cache key generation using a strongly-typed key object.
    /// </summary>
    /// <typeparam name="T">The type of value being cached.</typeparam>
    /// <typeparam name="TKey">The type of the key object.</typeparam>
    /// <param name="key">The key object used to generate cache key.</param>
    /// <param name="keyGenerator">Function that generates cache key from the key object.</param>
    /// <param name="fetchFunc">Function to fetch value if not in cache.</param>
    /// <param name="cacheDuration">Optional cache duration. If null, result is not cached.</param>
    /// <returns>The cached or fetched value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="keyGenerator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="fetchFunc"/> is <see langword="null"/>.</exception>
    public static async Task<T> GetOrFetchAsync<T, TKey>(
        this RequestCachingDecorator decorator,
        TKey key,
        Func<TKey, string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(fetchFunc);

        var cacheKey = keyGenerator(key);
        return await decorator.GetOrFetchAsync(cacheKey, fetchFunc, cacheDuration);
    }
}