#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

using System.Net;
using System.Net.Http;

/// <summary>
/// Implements retry logic for HTTP requests with exponential backoff.
/// Configurable for different failure scenarios and retry strategies.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(
        int maxRetries = 3,
        int initialDelayMs = 100,
        int maxDelayMs = 30000,
        double backoffMultiplier = 2.0,
        ILogger<RetryPolicy>? logger = null)
    {
        _maxRetries = Math.Max(0, maxRetries);
        _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
        _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        _backoffMultiplier = Math.Max(1.0, backoffMultiplier);
        _logger = logger ?? new NullLogger<RetryPolicy>();
    }

    /// <summary>
    /// Execute HTTP request with retry logic on transient failures.
    /// Retries on timeout, connection errors, and 5xx status codes.
    /// </summary>
    public async Task<HttpResponseMessage> ExecuteAsync(
        HttpClient client,
        HttpRequestMessage request,
        Func<HttpStatusCode, bool>? shouldRetry = null)
    {
        shouldRetry ??= IsTransientStatusCode;
        var currentDelay = _initialDelay;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            HttpRequestMessage requestToSend = attempt == 0 ? request : CloneRequest(request);

            try
            {
                var response = await client.SendAsync(requestToSend);

                if (!shouldRetry(response.StatusCode))
                    return response;

                if (attempt < _maxRetries)
                {
                    _logger.LogWarning(
                        "Request failed with {StatusCode}, retrying in {DelayMs}ms (attempt {Attempt})",
                        response.StatusCode,
                        (long)currentDelay.TotalMilliseconds,
                        attempt + 1);

                    response.Dispose();
                    await Task.Delay(currentDelay);
                    currentDelay = CalculateNextDelay(currentDelay);
                }
                else
                {
                    return response;
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request timeout, retrying (attempt {Attempt})", attempt + 1);

                if (attempt >= _maxRetries)
                    throw;

                await Task.Delay(currentDelay);
                currentDelay = CalculateNextDelay(currentDelay);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Request failed with error, retrying (attempt {Attempt})", attempt + 1);

                if (attempt >= _maxRetries)
                    throw;

                await Task.Delay(currentDelay);
                currentDelay = CalculateNextDelay(currentDelay);
            }
        }

        throw new InvalidOperationException("Should not reach this point");
    }

    /// <summary>
    /// Execute async function with retry logic.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool>? shouldRetry = null)
    {
        shouldRetry ??= IsTransientException;
        var currentDelay = _initialDelay;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                if (!shouldRetry(ex) || attempt >= _maxRetries)
                    throw;

                _logger.LogWarning(
                    ex,
                    "Operation failed, retrying in {DelayMs}ms (attempt {Attempt})",
                    (long)currentDelay.TotalMilliseconds,
                    attempt + 1);

                await Task.Delay(currentDelay);
                currentDelay = CalculateNextDelay(currentDelay);
            }
        }

        throw new InvalidOperationException("Should not reach this point");
    }

    /// <summary>
    /// Check if HTTP status code is transient (can be retried).
    /// </summary>
    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.TooManyRequests ||
               statusCode == HttpStatusCode.InternalServerError ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    /// <summary>
    /// Check if exception is transient (can be retried).
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        return ex is TaskCanceledException ||
               ex is HttpRequestException ||
               ex is TimeoutException ||
               (ex is InvalidOperationException && ex.Message.Contains("retry", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculate next delay with exponential backoff.
    /// </summary>
    private TimeSpan CalculateNextDelay(TimeSpan currentDelay)
    {
        var nextDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * _backoffMultiplier);
        return nextDelay > _maxDelay ? _maxDelay : nextDelay;
    }

    /// <summary>
    /// Clone an HttpRequestMessage to allow retrying with a fresh request object.
    /// </summary>
    private static HttpRequestMessage CloneRequest(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
        };

        if (source.Content != null)
        {
            if (source.Content is StringContent stringContent)
            {
                var content = stringContent.ReadAsStringAsync().GetAwaiter().GetResult();
                var encoding = source.Content.Headers.ContentEncoding.FirstOrDefault();
                var mediaType = source.Content.Headers.ContentType;
                clone.Content = new StringContent(content, encoding != null ? System.Text.Encoding.GetEncoding(encoding) : null, mediaType?.MediaType);
            }
            else
            {
                clone.Content = source.Content;
            }
        }

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var property in source.Properties)
        {
            clone.Properties[property.Key] = property.Value;
        }

        return clone;
    }
}

/// <summary>
/// Null logger implementation for cases where logging is not needed.
/// </summary>
public class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
