// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Decorator that wraps request handling with caching layer.
/// Intercepts requests and returns cached responses when available.
/// </summary>
public class RequestCachingDecorator
{
    private readonly CacheService _cacheService;
    private readonly ILogger<RequestCachingDecorator> _logger;

    public RequestCachingDecorator(CacheService cacheService, ILogger<RequestCachingDecorator> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Try to get cached response for request, executing handler if cache miss.
    /// </summary>
    public async Task<T> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return await fetchFunc();

        // Try cache first
        var cached = await _cacheService.GetAsync<T>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}, fetching from source", cacheKey);

        // Fetch from source
        var result = await fetchFunc();

        // Cache result
        if (result != null && cacheDuration.HasValue)
        {
            await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
            _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
        }

        return result;
    }

    /// <summary>
    /// Get or fetch with fallback to stale cache if fetch fails.
    /// Provides resilience by serving stale data when source is unavailable.
    /// </summary>
    public async Task<T?> GetOrFetchWithFallbackAsync<T>(
        string cacheKey,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null,
        TimeSpan? staleCacheTtl = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return await fetchFunc();

        // Try fresh cache
        var cached = await _cacheService.GetAsync<T>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Fresh cache hit for key: {CacheKey}", cacheKey);
            return cached;
        }

        try
        {
            // Fetch from source
            var result = await fetchFunc();

            // Cache result
            if (result != null && cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
                _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Source fetch failed, trying stale cache for key: {CacheKey}", cacheKey);

            // Fall back to stale cache
            var staleKey = $"{cacheKey}:stale";
            var staleCache = await _cacheService.GetAsync<T>(staleKey);

            if (staleCache != null)
            {
                _logger.LogInformation("Returning stale cache for key: {CacheKey}", cacheKey);

                // Refresh stale cache timeout
                if (staleCacheTtl.HasValue)
                {
                    await _cacheService.SetAsync(staleKey, staleCache, staleCacheTtl.Value);
                }

                return staleCache;
            }

            // No cache available, rethrow
            throw;
        }
    }

    /// <summary>
    /// Invalidate cache by key or pattern.
    /// </summary>
    public async Task InvalidateAsync(string keyPattern)
    {
        if (string.IsNullOrWhiteSpace(keyPattern))
            return;

        await _cacheService.InvalidatePrefixAsync(keyPattern);
        _logger.LogInformation("Invalidated cache for pattern: {Pattern}", keyPattern);
    }
}
