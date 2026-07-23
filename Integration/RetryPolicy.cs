#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

using System.Net;
using System.Net.Http;

/// <summary>
/// Implements retry logic for HTTP requests with jittered exponential backoff and retry budget.
/// Prevents retry storms by limiting total retries across all requests using a token bucket algorithm.
/// Only retries idempotent HTTP methods by default (GET, HEAD, OPTIONS) with opt-in for others.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;
    private readonly ILogger<RetryPolicy> _logger;
    private readonly RetryBudget _retryBudget;
    private readonly bool _allowNonIdempotentRetries;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class with default settings.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds before first retry (default: 100).</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds for any retry (default: 30000).</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0).</param>
    /// <param name="maxRetryBudgetTokens">Maximum retry tokens in the budget bucket (default: 100).</param>
    /// <param name="retryBudgetRefillRatePerSecond">Tokens refilled per second (default: 1.0).</param>
    /// <param name="allowNonIdempotentRetries">Whether to allow retries on non-idempotent methods like POST/PUT/DELETE (default: false).</param>
    /// <param name="logger">Logger instance (default: null).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range.</exception>
    public RetryPolicy(
        int maxRetries = 3,
        int initialDelayMs = 100,
        int maxDelayMs = 30000,
        double backoffMultiplier = 2.0,
        int maxRetryBudgetTokens = 100,
        double retryBudgetRefillRatePerSecond = 1.0,
        bool allowNonIdempotentRetries = false,
        ILogger<RetryPolicy>? logger = null)
    {
        _maxRetries = Math.Max(0, maxRetries);
        _initialDelay = TimeSpan.FromMilliseconds(Math.Max(1, initialDelayMs));
        _maxDelay = TimeSpan.FromMilliseconds(Math.Max(1, maxDelayMs));
        _backoffMultiplier = Math.Max(1.0, backoffMultiplier);
        _allowNonIdempotentRetries = allowNonIdempotentRetries;
        _logger = logger ?? new NullLogger<RetryPolicy>();
        _retryBudget = new RetryBudget(
            maxTokens: Math.Max(1, maxRetryBudgetTokens),
            refillRatePerSecond: Math.Max(0.1, retryBudgetRefillRatePerSecond)
        );
    }

    /// <summary>
    /// Execute HTTP request with retry logic on transient failures.
    /// Retries on timeout, connection errors, and 5xx status codes.
    /// Only retries idempotent HTTP methods (GET, HEAD, OPTIONS) unless configured otherwise.
    /// Respects the retry budget to prevent retry storms.
    /// </summary>
    /// <param name="client">The HTTP client to use for the request.</param>
    /// <param name="request">The HTTP request message to execute.</param>
    /// <param name="shouldRetry">Optional predicate to determine if a status code should be retried.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when client or request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when retry budget is exhausted.</exception>
    public async Task<HttpResponseMessage> ExecuteAsync(
        HttpClient client,
        HttpRequestMessage request,
        Func<HttpStatusCode, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        shouldRetry ??= IsTransientStatusCode;

        // Check if the HTTP method is idempotent (only retry idempotent methods by default)
        if (!_allowNonIdempotentRetries && !IsIdempotentMethod(request.Method))
        {
            _logger.LogDebug("Skipping retry for non-idempotent HTTP method: {Method}", request.Method);
            return await client.SendAsync(request, cancellationToken);
        }

        // Check retry budget before attempting first request
        if (!_retryBudget.TryConsumeToken())
        {
            _logger.LogWarning("Retry budget exhausted, skipping request execution to prevent overload");
            throw new InvalidOperationException("Retry budget exhausted. Cannot process request due to system overload.");
        }

        var currentDelay = _initialDelay;
        var attempt = 0;

        for (; attempt <= _maxRetries; attempt++)
        {
            HttpRequestMessage requestToSend = attempt == 0 ? request : CloneRequest(request);

            try
            {
                var response = await client.SendAsync(requestToSend, cancellationToken);

                if (!shouldRetry(response.StatusCode))
                {
                    return response;
                }

                // Dispose the response before retrying
                response.Dispose();

                if (attempt < _maxRetries)
                {
                    // Check retry budget before retrying
                    if (!_retryBudget.TryConsumeToken())
                    {
                        _logger.LogWarning(
                            "Retry budget exhausted during retry attempt {Attempt}, returning last failed response with status {StatusCode}",
                            attempt + 1,
                            response.StatusCode);
                        return response;
                    }

                    var delayWithJitter = ApplyJitter(currentDelay);
                    _logger.LogWarning(
                        "Request failed with {StatusCode}, retrying in {DelayMs}ms (attempt {Attempt})",
                        response.StatusCode,
                        (long)delayWithJitter.TotalMilliseconds,
                        attempt + 1);

                    await Task.Delay(delayWithJitter, cancellationToken);
                    currentDelay = CalculateNextDelay(currentDelay);
                }
                else
                {
                    return response;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is not OperationCanceledException)
            {
                // Timeout exception - retry if we have budget
                if (attempt >= _maxRetries)
                {
                    throw;
                }

                if (!_retryBudget.TryConsumeToken())
                {
                    _logger.LogWarning(ex, "Retry budget exhausted during timeout retry attempt {Attempt}", attempt + 1);
                    throw new InvalidOperationException("Retry budget exhausted during timeout retry", ex);
                }

                var delayWithJitter = ApplyJitter(currentDelay);
                _logger.LogWarning(ex, "Request timeout, retrying in {DelayMs}ms (attempt {Attempt})",
                    (long)delayWithJitter.TotalMilliseconds, attempt + 1);

                await Task.Delay(delayWithJitter, cancellationToken);
                currentDelay = CalculateNextDelay(currentDelay);
            }
            catch (HttpRequestException ex)
            {
                if (attempt >= _maxRetries)
                {
                    throw;
                }

                if (!_retryBudget.TryConsumeToken())
                {
                    _logger.LogWarning(ex, "Retry budget exhausted during connection error retry attempt {Attempt}", attempt + 1);
                    throw new InvalidOperationException("Retry budget exhausted during connection error retry", ex);
                }

                var delayWithJitter = ApplyJitter(currentDelay);
                _logger.LogWarning(ex, "Request failed with error, retrying in {DelayMs}ms (attempt {Attempt})",
                    (long)delayWithJitter.TotalMilliseconds, attempt + 1);

                await Task.Delay(delayWithJitter, cancellationToken);
                currentDelay = CalculateNextDelay(currentDelay);
            }
            catch (OperationCanceledException ex)
            {
                // Cancellation from external source - don't retry
                _logger.LogDebug(ex, "Request cancelled externally, not retrying");
                throw;
            }
        }

        throw new InvalidOperationException("Should not reach this point");
    }

    /// <summary>
    /// Execute async function with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="shouldRetry">Optional predicate to determine if an exception should be retried.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        shouldRetry ??= IsTransientException;

        var currentDelay = _initialDelay;
        var attempt = 0;

        for (; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (shouldRetry(ex) && attempt < _maxRetries)
            {
                if (!_retryBudget.TryConsumeToken())
                {
                    _logger.LogWarning(ex, "Retry budget exhausted during operation retry attempt {Attempt}", attempt + 1);
                    throw new InvalidOperationException("Retry budget exhausted during operation retry", ex);
                }

                var delayWithJitter = ApplyJitter(currentDelay);
                _logger.LogWarning(
                    ex,
                    "Operation failed, retrying in {DelayMs}ms (attempt {Attempt})",
                    (long)delayWithJitter.TotalMilliseconds,
                    attempt + 1);

                await Task.Delay(delayWithJitter, cancellationToken);
                currentDelay = CalculateNextDelay(currentDelay);
            }
        }

        throw new InvalidOperationException("Should not reach this point");
    }

    /// <summary>
    /// Check if HTTP status code is transient (can be retried).
    /// </summary>
    /// <param name="statusCode">The HTTP status code to check.</param>
    /// <returns>True if the status code is transient; otherwise, false.</returns>
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
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception is transient; otherwise, false.</returns>
    private static bool IsTransientException(Exception ex)
    {
        return ex is TaskCanceledException ||
               ex is HttpRequestException ||
               ex is TimeoutException ||
               (ex is InvalidOperationException && ex.Message.Contains("retry", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if HTTP method is idempotent (safe to retry).
    /// </summary>
    /// <param name="method">The HTTP method to check.</param>
    /// <returns>True if the method is idempotent; otherwise, false.</returns>
    private static bool IsIdempotentMethod(HttpMethod method)
    {
        return method == HttpMethod.Get ||
               method == HttpMethod.Head ||
               method == HttpMethod.Options;
    }

    /// <summary>
    /// Calculate next delay with exponential backoff.
    /// </summary>
    /// <param name="currentDelay">The current delay time span.</param>
    /// <returns>The next delay time span.</returns>
    private TimeSpan CalculateNextDelay(TimeSpan currentDelay)
    {
        var nextDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * _backoffMultiplier);
        return nextDelay > _maxDelay ? _maxDelay : nextDelay;
    }

    /// <summary>
    /// Apply full jitter to the delay to prevent thundering herd problems.
    /// </summary>
    /// <param name="delay">The base delay to apply jitter to.</param>
    /// <returns>The delay with jitter applied.</returns>
    private TimeSpan ApplyJitter(TimeSpan delay)
    {
        // Full jitter: random value between 0 and delay
        var jitterFactor = Random.Shared.NextDouble(); // 0.0 to 1.0
        var jitteredDelayMs = delay.TotalMilliseconds * jitterFactor;
        return TimeSpan.FromMilliseconds(jitteredDelayMs);
    }

    /// <summary>
    /// Clone an HttpRequestMessage to allow retrying with a fresh request object.
    /// </summary>
    /// <param name="source">The source request to clone.</param>
    /// <returns>A cloned HttpRequestMessage.</returns>
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
/// Token bucket algorithm for retry budget management.
/// Limits the total number of retries across all requests to prevent retry storms.
/// </summary>
internal sealed class RetryBudget
{
    private readonly double _maxTokens;
    private readonly double _refillRatePerMillisecond;
    private double _availableTokens;
    private DateTime _lastRefillTime;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryBudget"/> class.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens in the bucket.</param>
    /// <param name="refillRatePerSecond">Tokens refilled per second.</param>
    public RetryBudget(int maxTokens, double refillRatePerSecond)
    {
        _maxTokens = maxTokens;
        _availableTokens = maxTokens;
        _refillRatePerMillisecond = refillRatePerSecond / 1000.0;
        _lastRefillTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Attempt to consume a token from the budget.
    /// </summary>
    /// <returns>True if token was consumed; false if budget is exhausted.</returns>
    public bool TryConsumeToken()
    {
        lock (_lock)
        {
            Refill();

            if (_availableTokens >= 1.0)
            {
                _availableTokens -= 1.0;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Refill the token bucket based on elapsed time.
    /// </summary>
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefillTime;
        _lastRefillTime = now;

        if (elapsed.TotalMilliseconds > 0)
        {
            var tokensToAdd = elapsed.TotalMilliseconds * _refillRatePerMillisecond;
            _availableTokens = Math.Min(_maxTokens, _availableTokens + tokensToAdd);
        }
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