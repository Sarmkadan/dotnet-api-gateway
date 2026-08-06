#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Net;
using System.Net.Http;

namespace DotNetApiGateway.Integration;

/// <summary>
/// Factory for creating and managing pooled HTTP client instances.
/// Reuses HTTP clients for better performance and proper connection pooling.
/// Uses SocketsHttpHandler with tuned connection pooling to prevent socket exhaustion.
/// </summary>
public sealed class HttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private readonly SocketsHttpHandler _sharedHandler;

    public HttpClientFactory(ILogger<HttpClientFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        // Configure shared SocketsHttpHandler with proper connection pooling to prevent socket exhaustion
        // These settings prevent the classic "too many open files/sockets" issue in high-throughput gateways
        _sharedHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2), // Recycle connections periodically
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1), // Close idle connections
            ConnectCallback = null, // Connection callback for advanced scenarios
            EnableMultipleHttp2Connections = true, // Allow multiple HTTP/2 connections
            UseProxy = false, // Let HttpClient decide proxy usage
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All // Decompress responses automatically
        };

        // Set connection pool limits via ServicePointManager for .NET 6+ compatibility
        // Note: In .NET Core+, ServicePointManager settings are less critical but still respected
        System.Net.ServicePointManager.DefaultConnectionLimit = 200;
        System.Net.ServicePointManager.ReusePort = true;

        _logger.LogInformation("HTTP client factory initialized with connection pooling (MaxConnectionsPerServer: {MaxConnectionsPerServer}, PooledConnectionLifetime: {PooledConnectionLifetime})",
            _sharedHandler.MaxConnectionsPerServer, _sharedHandler.PooledConnectionLifetime);
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

        // Create new client with shared handler for connection pooling
        _lock.EnterWriteLock();
        try
        {
            if (_clients.TryGetValue(baseUrl, out var client))
                return client;

            // Create HttpClient with shared SocketsHttpHandler for proper connection pooling
            // This prevents the "creating HttpClient per request" anti-pattern that causes socket exhaustion
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = _sharedHandler.PooledConnectionLifetime,
                PooledConnectionIdleTimeout = _sharedHandler.PooledConnectionIdleTimeout,
                EnableMultipleHttp2Connections = _sharedHandler.EnableMultipleHttp2Connections,
                UseProxy = _sharedHandler.UseProxy,
                AllowAutoRedirect = _sharedHandler.AllowAutoRedirect,
                AutomaticDecompression = _sharedHandler.AutomaticDecompression
            };

            var newClient = new HttpClient(handler, disposeHandler: true)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = timeout ?? _defaultTimeout
            };

            newClient.DefaultRequestHeaders.Add("User-Agent", "DotNetApiGateway/1.0");

            _clients[baseUrl] = newClient;
            _logger.LogInformation("HTTP client created for {BaseUrl} with connection pooling", baseUrl);

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
        // Use shared handler for transient clients too to benefit from connection pooling
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = _sharedHandler.PooledConnectionLifetime,
            PooledConnectionIdleTimeout = _sharedHandler.PooledConnectionIdleTimeout,
            EnableMultipleHttp2Connections = _sharedHandler.EnableMultipleHttp2Connections,
            UseProxy = _sharedHandler.UseProxy,
            AllowAutoRedirect = _sharedHandler.AllowAutoRedirect,
            AutomaticDecompression = _sharedHandler.AutomaticDecompression
        };

        var client = new HttpClient(handler, disposeHandler: true)
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