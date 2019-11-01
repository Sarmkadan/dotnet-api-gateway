#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using DotNetApiGateway.Models;

/// <summary>
/// Service for intercepting and processing requests before forwarding.
/// Allows adding custom headers, transforming body, and modifying query strings.
/// </summary>
public sealed class RequestInterceptor
{
    private readonly Dictionary<string, RequestTransformer> _transformers = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<RequestInterceptor> _logger;

    public RequestInterceptor(ILogger<RequestInterceptor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a request transformer for a specific route.
    /// </summary>
    public void RegisterTransformer(string routeId, RequestTransformer transformer)
    {
        if (string.IsNullOrWhiteSpace(routeId) || transformer is null)
            return;

        _lock.EnterWriteLock();
        try
        {
            _transformers[routeId] = transformer;
            _logger.LogInformation("Request transformer registered for route: {RouteId}", routeId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get transformer for route, if registered.
    /// </summary>
    public RequestTransformer? GetTransformer(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
            return null;

        _lock.EnterReadLock();
        try
        {
            _transformers.TryGetValue(routeId, out var transformer);
            return transformer;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Intercept and process HTTP request.
    /// Applies registered transformations before forwarding.
    /// </summary>
    public async Task<HttpRequestMessage> InterceptAsync(
        string routeId,
        HttpRequestMessage request,
        RequestContext context)
    {
        if (request is null || context is null)
            return request;

        var transformer = GetTransformer(routeId);
        if (transformer is null)
            return request;

        _logger.LogDebug("Intercepting request for route: {RouteId}", routeId);

        // Add custom headers
        if (transformer.HeadersToAdd is not null)
        {
            foreach (var header in transformer.HeadersToAdd)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        // Remove headers
        if (transformer.HeadersToRemove is not null)
        {
            foreach (var headerName in transformer.HeadersToRemove)
            {
                request.Headers.Remove(headerName);
            }
        }

        // Transform body if applicable
        if (request.Content is not null && !string.IsNullOrWhiteSpace(transformer.BodyTemplate))
        {
            try
            {
                var bodyContent = await request.Content.ReadAsStringAsync();
                var transformedBody = ApplyBodyTemplate(bodyContent, transformer.BodyTemplate);
                request.Content = new StringContent(transformedBody, System.Text.Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transform request body for route: {RouteId}", routeId);
            }
        }

        return request;
    }

    /// <summary>
    /// Unregister transformer for route.
    /// </summary>
    public void UnregisterTransformer(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
            return;

        _lock.EnterWriteLock();
        try
        {
            _transformers.Remove(routeId);
            _logger.LogInformation("Request transformer unregistered for route: {RouteId}", routeId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Apply body template transformation.
    /// Simple variable substitution - production systems should use more sophisticated templating.
    /// </summary>
    private string ApplyBodyTemplate(string originalBody, string template)
    {
        var result = template;

        // Simple substitution - replace {body} with original content
        result = result.Replace("{body}", originalBody);

        // Add timestamp
        result = result.Replace("{timestamp}", DateTime.UtcNow.ToString("O"));

        // Add request ID
        result = result.Replace("{requestId}", Guid.NewGuid().ToString());

        return result;
    }
}

/// <summary>
/// Configuration for request transformation.
/// </summary>
public sealed class RequestTransformer
{
    /// <summary>
    /// Headers to add to request.
    /// </summary>
    public Dictionary<string, string>? HeadersToAdd { get; set; }

    /// <summary>
    /// Headers to remove from request.
    /// </summary>
    public List<string>? HeadersToRemove { get; set; }

    /// <summary>
    /// Template for body transformation.
    /// </summary>
    public string? BodyTemplate { get; set; }

    /// <summary>
    /// Query parameter mappings (source -> target).
    /// </summary>
    public Dictionary<string, string>? QueryParamMappings { get; set; }

    /// <summary>
    /// Whether to use this transformer for this route.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
