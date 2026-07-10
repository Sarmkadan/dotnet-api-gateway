#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static async Task<T> GetOrFetchAsync<T>(
        this RequestCachingDecorator decorator,
        Func<object[], string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null,
        params object[] keyParts) where T : class
    {
        var cacheKey = keyGenerator(keyParts);
        return await decorator.GetOrFetchAsync(cacheKey, fetchFunc, cacheDuration);
    }

    /// <summary>
    /// Get or fetch with fallback, automatically generating cache keys from request parameters.
    /// </summary>
    public static async Task<T?> GetOrFetchWithFallbackAsync<T>(
        this RequestCachingDecorator decorator,
        Func<object[], string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null,
        TimeSpan? staleCacheTtl = null,
        params object[] keyParts) where T : class
    {
        var cacheKey = keyGenerator(keyParts);
        return await decorator.GetOrFetchWithFallbackAsync(
            cacheKey, fetchFunc, cacheDuration, staleCacheTtl);
    }

    /// <summary>
    /// Invalidate multiple cache keys by pattern with support for wildcard patterns.
    /// </summary>
    public static async Task InvalidateMultipleAsync(
        this RequestCachingDecorator decorator,
        params string[] keyPatterns)
    {
        if (keyPatterns == null || keyPatterns.Length == 0)
            return;

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
    public static async Task<T> GetOrFetchAsync<T, TKey>(
        this RequestCachingDecorator decorator,
        TKey key,
        Func<TKey, string> keyGenerator,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null) where T : class
    {
        var cacheKey = keyGenerator(key);
        return await decorator.GetOrFetchAsync(cacheKey, fetchFunc, cacheDuration);
    }
}