#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

/// <summary>
/// Factory for creating and managing pooled HTTP client instances.
/// Reuses HTTP clients for better performance and proper connection pooling.
/// </summary>
public sealed class HttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public HttpClientFactory(ILogger<HttpClientFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get or create HTTP client for specific base URL.
    /// Reuses existing clients to benefit from connection pooling.
    /// </summary>
    public HttpClient GetClient(string baseUrl, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

        _lock.EnterReadLock();
        try
        {
            if (_clients.TryGetValue(baseUrl, out var client))
                return client;
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Create new client
        _lock.EnterWriteLock();
        try
        {
            if (_clients.TryGetValue(baseUrl, out var client))
                return client;

            var newClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = timeout ?? _defaultTimeout
            };

            newClient.DefaultRequestHeaders.Add("User-Agent", "DotNetApiGateway/1.0");

            _clients[baseUrl] = newClient;
            _logger.LogInformation("HTTP client created for {BaseUrl}", baseUrl);

            return newClient;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Create a new HTTP client without pooling for one-off requests.
    /// </summary>
    public HttpClient CreateTransientClient(TimeSpan? timeout = null)
    {
        var client = new HttpClient
        {
            Timeout = timeout ?? _defaultTimeout
        };

        client.DefaultRequestHeaders.Add("User-Agent", "DotNetApiGateway/1.0");
        return client;
    }

    /// <summary>
    /// Update client timeout configuration.
    /// </summary>
    public void SetClientTimeout(string baseUrl, TimeSpan timeout)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_clients.TryGetValue(baseUrl, out var client))
            {
                client.Timeout = timeout;
                _logger.LogInformation("Client timeout updated for {BaseUrl}: {TimeoutMs}ms", baseUrl, timeout.TotalMilliseconds);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Remove cached HTTP client for specific base URL.
    /// </summary>
    public void RemoveClient(string baseUrl)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_clients.TryGetValue(baseUrl, out var client))
            {
                client.Dispose();
                _clients.Remove(baseUrl);
                _logger.LogInformation("HTTP client removed for {BaseUrl}", baseUrl);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clear all cached HTTP clients and dispose resources.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }

            _clients.Clear();
            _logger.LogInformation("All HTTP clients cleared");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get count of cached clients.
    /// </summary>
    public int GetClientCount()
    {
        _lock.EnterReadLock();
        try
        {
            return _clients.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
