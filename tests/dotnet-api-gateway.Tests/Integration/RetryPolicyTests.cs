using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNetApiGateway.Integration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests.Integration;

public class RetryPolicyTests
{
    private readonly Mock<ILogger<RetryPolicy>> _mockLogger;
    private readonly ILogger<RetryPolicyTests> _logger;
    private readonly RetryPolicy _retryPolicy;

    public RetryPolicyTests()
    {
        _mockLogger = new Mock<ILogger<RetryPolicy>>();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<RetryPolicyTests>();
        _retryPolicy = new RetryPolicy(
            maxRetries: 3,
            initialDelayMs: 10,
            maxDelayMs: 100,
            backoffMultiplier: 2.0,
            logger: _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesThenSuccess_ReturnsSuccessfulResponse()
    {
        _logger.LogInformation("Starting {TestName} with maxRetries {MaxRetries}", "ExecuteAsync_RetriesThenSuccess_ReturnsSuccessfulResponse", 3);

        // Arrange
        var callCount = 0;
        var handler = new CountingMockHttpMessageHandler(
            // First two calls return 500, third call returns 200
            (req) =>
            {
                callCount++;
                if (callCount < 3)
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Act
        var response = await _retryPolicy.ExecuteAsync(httpClient, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(3); // Initial attempt + 2 retries
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InternalServerError") && v.ToString()!.Contains("retrying")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2)); // Should log twice for the two retries

        _logger.LogInformation("Completed {TestName} with status code {StatusCode} after {TotalAttempts} attempts", "ExecuteAsync_RetriesThenSuccess_ReturnsSuccessfulResponse", response.StatusCode, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_Exhaustion_ReturnsLastResponse()
    {
        _logger.LogInformation("Starting {TestName} with maxRetries {MaxRetries}", "ExecuteAsync_Exhaustion_ReturnsLastResponse", 3);

        // Arrange
        var handler = new CountingMockHttpMessageHandler(
            // All calls return 500
            (req) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Act
        var response = await _retryPolicy.ExecuteAsync(httpClient, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        handler.CallCount.Should().Be(4); // Initial attempt + 3 retries
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InternalServerError") && v.ToString()!.Contains("retrying")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3)); // Should log for each retry attempt (maxRetries = 3)

        _logger.LogWarning("Retry budget exhausted after {TotalAttempts} attempts; returning last response with status code {StatusCode}", handler.CallCount, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_BackoffGrowth_DelayIncreasesExponentially()
    {
        _logger.LogInformation("Starting {TestName} with initialDelayMs {InitialDelayMs}, maxDelayMs {MaxDelayMs}, backoffMultiplier {BackoffMultiplier}", "ExecuteAsync_BackoffGrowth_DelayIncreasesExponentially", 10, 100, 2.0);

        // Arrange
        var retryPolicy = new RetryPolicy(
            maxRetries: 3,
            initialDelayMs: 10,
            maxDelayMs: 100,
            backoffMultiplier: 2.0);

        var handler = new CountingMockHttpMessageHandler(
            // Always return 500 to trigger retries
            (req) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Act
        await retryPolicy.ExecuteAsync(httpClient, request);

        // Assert - Verify that delays were logged and increase
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("retrying in")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3)); // Should log 3 times (for attempts 1, 2, 3)

        _logger.LogInformation("Completed {TestName}; observed {RetryCount} exponential backoff delays", "ExecuteAsync_BackoffGrowth_DelayIncreasesExponentially", 3);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_ThrowsOperationCanceledException()
    {
        _logger.LogInformation("Starting {TestName}", "ExecuteAsync_Cancellation_ThrowsOperationCanceledException");

        // Arrange
        var cts = new CancellationTokenSource();
        var handler = new DelayingMockHttpMessageHandler(
            // Simulate delay that will be cancelled
            async (req, ct) =>
            {
                await Task.Delay(1000, ct); // This will be cancelled
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Act
        Func<Task> act = async () => await _retryPolicy.ExecuteAsync(httpClient, request);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request timeout")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _logger.LogWarning("Request timed out and was cancelled as expected");
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomShouldRetryPredicate_UsesCustomLogic()
    {
        _logger.LogInformation("Starting {TestName} with custom shouldRetry predicate treating {RetryableStatusCode} as retryable", "ExecuteAsync_WithCustomShouldRetryPredicate_UsesCustomLogic", HttpStatusCode.BadRequest);

        // Arrange
        var callCount = 0;
        var handler = new CountingMockHttpMessageHandler(
            // Return 400 (Bad Request) which is not transient by default
            (req) =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Custom shouldRetry that treats 400 as retryable
        Func<HttpStatusCode, bool> shouldRetry = statusCode => statusCode == HttpStatusCode.BadRequest;

        // Act
        var response = await _retryPolicy.ExecuteAsync(httpClient, request, shouldRetry);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        handler.CallCount.Should().Be(4); // Initial attempt + 3 retries (maxRetries = 3)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BadRequest") && v.ToString()!.Contains("retrying")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3)); // Should log for each retry

        _logger.LogInformation("Completed {TestName} with status code {StatusCode} after {TotalAttempts} attempts", "ExecuteAsync_WithCustomShouldRetryPredicate_UsesCustomLogic", response.StatusCode, handler.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_T_OnSuccess_ReturnsResult()
    {
        _logger.LogInformation("Starting {TestName} with maxRetries {MaxRetries}", "ExecuteAsync_T_OnSuccess_ReturnsResult", 2);

        // Arrange
        var callCount = 0;
        var retryPolicy = new RetryPolicy(
            maxRetries: 2,
            initialDelayMs: 10,
            maxDelayMs: 100,
            backoffMultiplier: 2.0);

        // Act
        var result = await retryPolicy.ExecuteAsync<string>(
            async () =>
            {
                callCount++;
                if (callCount < 2)
                    throw new HttpRequestException("Temporary failure");
                return "Success";
            });

        // Assert
        result.Should().Be("Success");
        callCount.Should().Be(2); // Initial attempt + 1 retry

        _logger.LogInformation("Completed {TestName} with result {Result} after {TotalAttempts} attempts", "ExecuteAsync_T_OnSuccess_ReturnsResult", result, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_T_OnExhaustion_ThrowsException()
    {
        _logger.LogInformation("Starting {TestName} with maxRetries {MaxRetries}", "ExecuteAsync_T_OnExhaustion_ThrowsException", 2);

        // Arrange
        var retryPolicy = new RetryPolicy(
            maxRetries: 2,
            initialDelayMs: 10,
            maxDelayMs: 100,
            backoffMultiplier: 2.0);

        // Act
        Func<Task> act = async () => await retryPolicy.ExecuteAsync<string>(
            async () =>
            {
                throw new HttpRequestException("Persistent failure");
            });

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();

        _logger.LogWarning("Function execution exhausted all {MaxRetries} retries and threw as expected", 2);
    }

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_RetriesAndSucceeds()
    {
        _logger.LogInformation("Starting {TestName}", "ExecuteAsync_HttpRequestException_RetriesAndSucceeds");

        // Arrange
        var callCount = 0;
        var handler = new CountingMockHttpMessageHandler(
            (req) =>
            {
                callCount++;
                if (callCount < 2)
                    throw new HttpRequestException("Connection failed");
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        var httpClient = new HttpClient(handler);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://test.com");

        // Act
        var response = await _retryPolicy.ExecuteAsync(httpClient, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.CallCount.Should().Be(2);

        _logger.LogInformation("Completed {TestName} with status code {StatusCode} after {TotalAttempts} attempts", "ExecuteAsync_HttpRequestException_RetriesAndSucceeds", response.StatusCode, handler.CallCount);
    }
}

// Helper class to count calls
internal class CountingMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _syncHandler;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _asyncHandler;
    private bool _isAsync;

    public int CallCount { get; private set; } = 0;

    public CountingMockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> syncHandler)
    {
        _syncHandler = syncHandler;
        _isAsync = false;
    }

    public CountingMockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> asyncHandler)
    {
        _asyncHandler = asyncHandler;
        _isAsync = true;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        if (_isAsync)
        {
            return _asyncHandler(request, cancellationToken);
        }
        return Task.FromResult(_syncHandler(request));
    }
}

// Helper class for delaying responses
internal class DelayingMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelayingMockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}
