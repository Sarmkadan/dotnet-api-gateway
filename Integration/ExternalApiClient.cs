#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

using System.Net.Http.Json;

/// <summary>
/// Generic HTTP client wrapper for calling external APIs with error handling and retry logic.
/// Provides convenient methods for common HTTP operations with built-in resilience.
/// </summary>
public sealed class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(
        HttpClient httpClient,
        RetryPolicy? retryPolicy = null,
        ILogger<ExternalApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _retryPolicy = retryPolicy ?? new RetryPolicy();
        _logger = logger ?? new NullLogger<ExternalApiClient>();
    }

    /// <summary>
    /// Make GET request to external API endpoint.
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            _logger.LogInformation("GET request to {Endpoint}", endpoint);
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            var response = await _retryPolicy.ExecuteAsync(_httpClient, request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<T>();
            }

            _logger.LogWarning("GET request failed with status {StatusCode}: {Endpoint}", response.StatusCode, endpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Make POST request to external API endpoint.
    /// </summary>
    public async Task<T?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("POST request to {Endpoint}", endpoint);
            var content = JsonContent.Create(data);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            var response = await _retryPolicy.ExecuteAsync(_httpClient, request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<TResponse>();
            }

            _logger.LogWarning("POST request failed with status {StatusCode}: {Endpoint}", response.StatusCode, endpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Make PUT request to external API endpoint.
    /// </summary>
    public async Task<T?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("PUT request to {Endpoint}", endpoint);
            var content = JsonContent.Create(data);
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content };
            var response = await _retryPolicy.ExecuteAsync(_httpClient, request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<TResponse>();
            }

            _logger.LogWarning("PUT request failed with status {StatusCode}: {Endpoint}", response.StatusCode, endpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Make DELETE request to external API endpoint.
    /// </summary>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("DELETE request to {Endpoint}", endpoint);
            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            var response = await _retryPolicy.ExecuteAsync(_httpClient, request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("DELETE request succeeded: {Endpoint}", endpoint);
                return true;
            }

            _logger.LogWarning("DELETE request failed with status {StatusCode}: {Endpoint}", response.StatusCode, endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Make raw HTTP request with custom headers and content.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(
        string endpoint,
        HttpMethod method,
        string? contentType = null,
        string? content = null,
        Dictionary<string, string>? headers = null)
    {
        try
        {
            _logger.LogInformation("{Method} request to {Endpoint}", method, endpoint);

            var request = new HttpRequestMessage(method, endpoint);

            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content, System.Text.Encoding.UTF8, contentType ?? "application/json");
            }

            if (headers is not null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            return await _retryPolicy.ExecuteAsync(_httpClient, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} request failed: {Endpoint}", method, endpoint);
            throw;
        }
    }
}
