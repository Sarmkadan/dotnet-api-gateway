#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetApiGateway.Constants;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetApiGateway.Tests.Repositories;

/// <summary>
/// Unit tests for <see cref="InMemoryRateLimitStore"/> covering the TokenBucket,
/// FixedWindow, and SlidingWindow rate limiting strategies, as well as entry
/// retrieval and reset operations.
/// </summary>
public class InMemoryRateLimitStoreTests
{
    private readonly Mock<ILogger<InMemoryRateLimitStore>> _loggerMock;
    private readonly InMemoryRateLimitStore _store;
    private readonly RateLimitPolicy _tokenBucketPolicy;
    private readonly RateLimitPolicy _fixedWindowPolicy;
    private readonly RateLimitPolicy _slidingWindowPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRateLimitStoreTests"/> class,
    /// creating the store under test with a mocked logger and the shared token bucket
    /// (10 rpm, burst 10), fixed window (5 rpm), and sliding window (3 rpm) policies.
    /// </summary>
    public InMemoryRateLimitStoreTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryRateLimitStore>>();
        _store = new InMemoryRateLimitStore(_loggerMock.Object);

        // Token Bucket policy: 10 requests per minute, burst size 10
        _tokenBucketPolicy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.TokenBucket,
            RequestsPerMinute = 10,
            BurstSize = 10
        };

        // Fixed Window policy: 5 requests per minute
        _fixedWindowPolicy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.FixedWindow,
            RequestsPerMinute = 5,
            RequestsPerHour = 100
        };

        // Sliding Window policy: 3 requests per minute
        _slidingWindowPolicy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.SlidingWindow,
            RequestsPerMinute = 3
        };
    }

    /// <summary>
    /// Verifies that the token bucket strategy allows each of the first 10 requests,
    /// up to the configured burst size.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "test-client-1";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_TokenBucket_UnderLimit_AllowsRequest),
            key,
            RateLimitStrategy.TokenBucket);

        // Act & Assert - First 10 requests should be allowed (burst size)
        for (int i = 0; i < 10; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under burst limit");
        }

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: all {RequestCount} requests allowed within burst size",
            nameof(IsRequestAllowedAsync_TokenBucket_UnderLimit_AllowsRequest),
            10);
    }

    /// <summary>
    /// Verifies that the token bucket strategy blocks the 11th request once the
    /// burst size of 10 tokens has been exhausted.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "test-client-2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_TokenBucket_OverLimit_BlocksRequest),
            key,
            RateLimitStrategy.TokenBucket);

        // Fill the token bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        }

        // Act - 11th request should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: burst size {BurstSize} exhausted, next request should be rejected",
            key,
            10);
        var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);

        // Assert
        result.Should().BeFalse("Token bucket should block requests after burst size is exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: request over limit was blocked as expected",
            nameof(IsRequestAllowedAsync_TokenBucket_OverLimit_BlocksRequest));
    }

    /// <summary>
    /// Verifies that the token bucket strategy refills tokens over time: after draining
    /// a bucket with a 60-requests-per-minute policy (1 token per second), waiting
    /// 1.1 seconds allows one more request.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_TokensRefillOverTime_AllowsAfterWait()
    {
        // Arrange
        var key = "test-client-3";
        var policy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.TokenBucket,
            RequestsPerMinute = 60, // 1 token per second
            BurstSize = 10
        };
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy with {RequestsPerMinute} rpm and burst {BurstSize}",
            nameof(IsRequestAllowedAsync_TokenBucket_TokensRefillOverTime_AllowsAfterWait),
            key,
            RateLimitStrategy.TokenBucket,
            policy.RequestsPerMinute,
            policy.BurstSize);

        // Fill the bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, policy);
        }

        // Act - Wait for tokens to refill (simulate time passing)
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMs} ms for token refill on key {Key}",
            1100,
            key);
        await Task.Delay(1100); // Wait 1.1 seconds

        // Assert - Should have 1 token refilled
        var result = await _store.IsRequestAllowedAsync(key, policy);
        result.Should().BeTrue("Should allow request after token refill");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: token refill allowed a new request after {DelayMs} ms",
            nameof(IsRequestAllowedAsync_TokenBucket_TokensRefillOverTime_AllowsAfterWait),
            1100);
    }

    /// <summary>
    /// Verifies that token bucket rate limits are tracked independently per key:
    /// exhausting one key's bucket does not affect another key, while the exhausted
    /// key remains blocked.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_IndependentKeys_DoNotInterfere()
    {
        // Arrange
        var key1 = "client-1";
        var key2 = "client-2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for keys {Key1} and {Key2} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_TokenBucket_IndependentKeys_DoNotInterfere),
            key1,
            key2,
            RateLimitStrategy.TokenBucket);

        // Fill key1
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        }

        // key2 should still be able to make requests
        var result = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result.Should().BeTrue("Different keys should have independent rate limits");

        // key1 should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: burst size {BurstSize} exhausted, request should be rejected",
            key1,
            10);
        result = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        result.Should().BeFalse("Same key should be blocked after burst size exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: keys {Key1} and {Key2} enforced independent limits",
            nameof(IsRequestAllowedAsync_TokenBucket_IndependentKeys_DoNotInterfere),
            key1,
            key2);
    }

    /// <summary>
    /// Verifies that the fixed window strategy allows each of the first 5 requests,
    /// up to the configured per-minute limit.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "fixed-window-client-1";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_FixedWindow_UnderLimit_AllowsRequest),
            key,
            RateLimitStrategy.FixedWindow);

        // Act & Assert - First 5 requests should be allowed
        for (int i = 0; i < 5; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under fixed window limit");
        }

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: all {RequestCount} requests allowed within fixed window limit",
            nameof(IsRequestAllowedAsync_FixedWindow_UnderLimit_AllowsRequest),
            5);
    }

    /// <summary>
    /// Verifies that the fixed window strategy blocks the 6th request once the
    /// per-minute limit of 5 requests has been reached.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "fixed-window-client-2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_FixedWindow_OverLimit_BlocksRequest),
            key,
            RateLimitStrategy.FixedWindow);

        // Fill the fixed window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Act - 6th request should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: fixed window limit of {RequestCount} reached, next request should be rejected",
            key,
            5);
        var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);

        // Assert
        result.Should().BeFalse("Fixed window should block requests after limit is exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: request over limit was blocked as expected",
            nameof(IsRequestAllowedAsync_FixedWindow_OverLimit_BlocksRequest));
    }

    /// <summary>
    /// Verifies that the fixed window strategy restores the request allowance after
    /// the current one-minute window elapses (simulated by waiting 61 seconds).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_WindowReset_RestoresAllowance()
    {
        // Arrange
        var key = "fixed-window-reset-client";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_FixedWindow_WindowReset_RestoresAllowance),
            key,
            RateLimitStrategy.FixedWindow);

        // Fill the window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Act - Wait for window to reset (61 seconds for minute window)
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMs} ms for fixed window reset on key {Key}",
            61000,
            key);
        await Task.Delay(61000);

        // Assert - Should be able to make requests again
        var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        result.Should().BeTrue("Fixed window should restore allowance after reset");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: allowance restored after window reset of {DelayMs} ms",
            nameof(IsRequestAllowedAsync_FixedWindow_WindowReset_RestoresAllowance),
            61000);
    }

    /// <summary>
    /// Verifies that a fixed window policy configured with only an hourly limit
    /// (no per-minute limit) blocks the 6th request after 5 requests have been made.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_HourWindow_ResetsAfterHour()
    {
        // Arrange
        var hourPolicy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.FixedWindow,
            RequestsPerMinute = 0, // Disable minute limit
            RequestsPerHour = 5
        };

        var key = "hour-window-client";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy with {RequestsPerHour} requests per hour",
            nameof(IsRequestAllowedAsync_FixedWindow_HourWindow_ResetsAfterHour),
            key,
            RateLimitStrategy.FixedWindow,
            hourPolicy.RequestsPerHour);

        // Fill the hour window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, hourPolicy);
        }

        // Act - 6th request should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: hour window limit of {RequestCount} reached, next request should be rejected",
            key,
            5);
        var result = await _store.IsRequestAllowedAsync(key, hourPolicy);
        result.Should().BeFalse("Hour window should block requests after limit exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: request over hourly limit was blocked as expected",
            nameof(IsRequestAllowedAsync_FixedWindow_HourWindow_ResetsAfterHour));
    }

    /// <summary>
    /// Verifies that the sliding window strategy allows each of the first 3 requests,
    /// up to the configured per-minute limit.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "sliding-client-1";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_SlidingWindow_UnderLimit_AllowsRequest),
            key,
            RateLimitStrategy.SlidingWindow);

        // Act & Assert - First 3 requests should be allowed
        for (int i = 0; i < 3; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under sliding window limit");
        }

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: all {RequestCount} requests allowed within sliding window limit",
            nameof(IsRequestAllowedAsync_SlidingWindow_UnderLimit_AllowsRequest),
            3);
    }

    /// <summary>
    /// Verifies that the sliding window strategy blocks the 4th request once the
    /// per-minute limit of 3 requests has been reached.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "sliding-client-2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_SlidingWindow_OverLimit_BlocksRequest),
            key,
            RateLimitStrategy.SlidingWindow);

        // Fill the sliding window
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        }

        // Act - 4th request should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: sliding window limit of {RequestCount} reached, next request should be rejected",
            key,
            3);
        var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);

        // Assert
        result.Should().BeFalse("Sliding window should block requests after limit is exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: request over limit was blocked as expected",
            nameof(IsRequestAllowedAsync_SlidingWindow_OverLimit_BlocksRequest));
    }

    /// <summary>
    /// Verifies that the sliding window strategy allows a new request once the oldest
    /// tracked request falls outside the trailing one-minute window (simulated by
    /// waiting 61 seconds).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_OldRequestsExpire_AllowsAfterWait()
    {
        // Arrange
        var key = "sliding-expiry-client";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_SlidingWindow_OldRequestsExpire_AllowsAfterWait),
            key,
            RateLimitStrategy.SlidingWindow);

        // Make 3 requests
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        }

        // Wait for oldest request to expire (61 seconds)
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMs} ms for oldest request to expire on key {Key}",
            61000,
            key);
        await Task.Delay(61000);

        // Act - Should be able to make another request
        var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        result.Should().BeTrue("Sliding window should allow request after oldest request expires");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: request allowed after expiry wait of {DelayMs} ms",
            nameof(IsRequestAllowedAsync_SlidingWindow_OldRequestsExpire_AllowsAfterWait),
            61000);
    }

    /// <summary>
    /// Verifies that sliding window rate limits are tracked independently per key:
    /// exhausting one key's window does not affect another key, while the exhausted
    /// key remains blocked.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_IndependentKeys_DoNotInterfere()
    {
        // Arrange
        var key1 = "sliding-key1";
        var key2 = "sliding-key2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for keys {Key1} and {Key2} using {Strategy} policy",
            nameof(IsRequestAllowedAsync_SlidingWindow_IndependentKeys_DoNotInterfere),
            key1,
            key2,
            RateLimitStrategy.SlidingWindow);

        // Fill key1
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _slidingWindowPolicy);
        }

        // key2 should still be able to make requests
        var result = await _store.IsRequestAllowedAsync(key2, _slidingWindowPolicy);
        result.Should().BeTrue("Different keys should have independent sliding window limits");

        // key1 should be blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: sliding window limit of {RequestCount} reached, request should be rejected",
            key1,
            3);
        result = await _store.IsRequestAllowedAsync(key1, _slidingWindowPolicy);
        result.Should().BeFalse("Same key should be blocked after sliding window limit exceeded");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: keys {Key1} and {Key2} enforced independent limits",
            nameof(IsRequestAllowedAsync_SlidingWindow_IndependentKeys_DoNotInterfere),
            key1,
            key2);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.GetEntryAsync"/> returns an entry
    /// for the token bucket strategy whose key matches, whose count reflects the 5 requests
    /// made, and which reports non-negative tokens and a positive remaining time.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetEntryAsync_TokenBucket_ReturnsCorrectEntry()
    {
        // Arrange
        var key = "get-entry-token-bucket-test";
        var policy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.TokenBucket,
            RequestsPerMinute = 10,
            BurstSize = 10
        };
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(GetEntryAsync_TokenBucket_ReturnsCorrectEntry),
            key,
            RateLimitStrategy.TokenBucket);

        // Make some requests
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, policy);
        }

        // Act
        var entry = await _store.GetEntryAsync(key, policy);

        // Assert
        entry.Should().NotBeNull();
        entry.Key.Should().Be(key);
        entry.Count.Should().Be(5);
        entry.Tokens.Should().BeGreaterThan(-1);
        entry.RemainingTimeSeconds.Should().BeGreaterThan(0);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: retrieved entry for key {Key} with Count={Count}, Tokens={Tokens}, RemainingTimeSeconds={RemainingTimeSeconds}",
            nameof(GetEntryAsync_TokenBucket_ReturnsCorrectEntry),
            entry.Key,
            entry.Count,
            entry.Tokens,
            entry.RemainingTimeSeconds);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.GetEntryAsync"/> returns an entry
    /// for the fixed window strategy whose key matches, whose count reflects the 3 requests
    /// made, and whose remaining time falls within the one-minute window.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetEntryAsync_FixedWindow_ReturnsCorrectEntry()
    {
        // Arrange
        var key = "get-entry-fixed-window-test";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(GetEntryAsync_FixedWindow_ReturnsCorrectEntry),
            key,
            RateLimitStrategy.FixedWindow);

        // Make some requests
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Act
        var entry = await _store.GetEntryAsync(key, _fixedWindowPolicy);

        // Assert
        entry.Should().NotBeNull();
        entry.Key.Should().Be(key);
        entry.Count.Should().Be(3);
        entry.Tokens.Should().BeGreaterThan(-1);
        entry.RemainingTimeSeconds.Should().BeInRange(0, 60);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: retrieved entry for key {Key} with Count={Count}, Tokens={Tokens}, RemainingTimeSeconds={RemainingTimeSeconds}",
            nameof(GetEntryAsync_FixedWindow_ReturnsCorrectEntry),
            entry.Key,
            entry.Count,
            entry.Tokens,
            entry.RemainingTimeSeconds);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.GetEntryAsync"/> returns an entry
    /// for the sliding window strategy whose key matches, whose count reflects the 2 requests
    /// made, and which reports a positive remaining time.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetEntryAsync_SlidingWindow_ReturnsCorrectEntry()
    {
        // Arrange
        var key = "get-entry-sliding-window";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(GetEntryAsync_SlidingWindow_ReturnsCorrectEntry),
            key,
            RateLimitStrategy.SlidingWindow);

        // Make some requests
        for (int i = 0; i < 2; i++)
        {
            await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        }

        // Act
        var entry = await _store.GetEntryAsync(key, _slidingWindowPolicy);

        // Assert
        entry.Should().NotBeNull();
        entry.Key.Should().Be(key);
        entry.Count.Should().Be(2);
        entry.Tokens.Should().BeGreaterThan(-1);
        entry.RemainingTimeSeconds.Should().BeGreaterThan(0);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: retrieved entry for key {Key} with Count={Count}, Tokens={Tokens}, RemainingTimeSeconds={RemainingTimeSeconds}",
            nameof(GetEntryAsync_SlidingWindow_ReturnsCorrectEntry),
            entry.Key,
            entry.Count,
            entry.Tokens,
            entry.RemainingTimeSeconds);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.GetEntryAsync"/> returns a fresh
    /// entry with a zero count for a key that has never made a request.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetEntryAsync_NonExistentKey_ReturnsNewEntry()
    {
        // Arrange
        var key = "non-existent-key-test";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for unknown key {Key} using {Strategy} policy",
            nameof(GetEntryAsync_NonExistentKey_ReturnsNewEntry),
            key,
            RateLimitStrategy.TokenBucket);

        // Act
        var entry = await _store.GetEntryAsync(key, _tokenBucketPolicy);

        // Assert
        entry.Should().NotBeNull();
        entry.Key.Should().Be(key);
        entry.Count.Should().Be(0);
        entry.Tokens.Should().BeGreaterThan(-1);
        entry.RemainingTimeSeconds.Should().BeGreaterThan(0);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: fresh entry returned for key {Key} with Count={Count}, Tokens={Tokens}, RemainingTimeSeconds={RemainingTimeSeconds}",
            nameof(GetEntryAsync_NonExistentKey_ReturnsNewEntry),
            entry.Key,
            entry.Count,
            entry.Tokens,
            entry.RemainingTimeSeconds);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.ResetKeyAsync"/> clears the rate
    /// limit for a single blocked key so that subsequent requests are allowed again.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ResetKeyAsync_ClearsRateLimitForSpecificKey()
    {
        // Arrange
        var key = "reset-key";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(ResetKeyAsync_ClearsRateLimitForSpecificKey),
            key,
            RateLimitStrategy.TokenBucket);

        // Fill the bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        }

        // Verify it's blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for key {Key}: burst size {BurstSize} exhausted before reset",
            key,
            10);
        var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        result.Should().BeFalse("Should be blocked before reset");

        // Act
        _loggerMock.Object.LogInformation(
            "Resetting rate limit for key {Key}",
            key);
        await _store.ResetKeyAsync(key);

        // Assert - Should be allowed again after reset
        result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        result.Should().BeTrue("Should be allowed after key reset");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: key {Key} allowed again after reset",
            nameof(ResetKeyAsync_ClearsRateLimitForSpecificKey),
            key);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.ResetAllAsync"/> clears the rate
    /// limits for all keys so that previously blocked keys are allowed again.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ResetAllAsync_ClearsAllRateLimits()
    {
        // Arrange - Create multiple keys with rate limits
        var key1 = "all-reset-key1";
        var key2 = "all-reset-key2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for keys {Key1} and {Key2} using {Strategy} policy",
            nameof(ResetAllAsync_ClearsAllRateLimits),
            key1,
            key2,
            RateLimitStrategy.TokenBucket);

        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
            await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        }

        // Verify both are blocked
        _loggerMock.Object.LogWarning(
            "Exercising throttled path for keys {Key1} and {Key2}: burst size {BurstSize} exhausted before reset",
            key1,
            key2,
            5);
        var result1 = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        var result2 = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result1.Should().BeFalse("Key1 should be blocked before reset");
        result2.Should().BeFalse("Key2 should be blocked before reset");

        // Act
        _loggerMock.Object.LogInformation(
            "Resetting rate limits for all keys",
            Array.Empty<object>());
        await _store.ResetAllAsync();

        // Assert - Both should be allowed again
        result1 = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        result2 = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result1.Should().BeTrue("Key1 should be allowed after all reset");
        result2.Should().BeTrue("Key2 should be allowed after all reset");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: keys {Key1} and {Key2} allowed again after global reset",
            nameof(ResetAllAsync_ClearsAllRateLimits),
            key1,
            key2);
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryRateLimitStore.GetAllEntriesAsync"/> returns an
    /// entry for each of the two keys that have made requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetAllEntriesAsync_ReturnsAllActiveEntries()
    {
        // Arrange - Create multiple keys
        var key1 = "all-entries-key1";
        var key2 = "all-entries-key2";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for keys {Key1} and {Key2} using {Strategy} policy",
            nameof(GetAllEntriesAsync_ReturnsAllActiveEntries),
            key1,
            key2,
            RateLimitStrategy.TokenBucket);

        await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);

        // Act
        var entries = await _store.GetAllEntriesAsync();

        // Assert
        entries.Should().NotBeNull();
        var entryList = entries.ToList();
        entryList.Should().HaveCount(2);
        entryList.Should().Contain(e => e.Key == key1);
        entryList.Should().Contain(e => e.Key == key2);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: retrieved {EntryCount} active entries",
            nameof(GetAllEntriesAsync_ReturnsAllActiveEntries),
            entryList.Count);
    }

    /// <summary>
    /// Verifies that token bucket refills accumulate over time: after draining the bucket
    /// and waiting 2.5 seconds with a 60-requests-per-minute policy (1 token per second),
    /// enough tokens have refilled to allow another request.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TokenBucket_RefillAccumulatesOverMultipleCalls()
    {
        // Arrange
        var key = "refill-test";
        var policy = new RateLimitPolicy
        {
            Strategy = RateLimitStrategy.TokenBucket,
            RequestsPerMinute = 60, // 1 token per second
            BurstSize = 10
        };
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy with {RequestsPerMinute} rpm and burst {BurstSize}",
            nameof(TokenBucket_RefillAccumulatesOverMultipleCalls),
            key,
            RateLimitStrategy.TokenBucket,
            policy.RequestsPerMinute,
            policy.BurstSize);

        // Fill the bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, policy);
        }

        // Use 5 tokens
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, policy);
        }

        // Wait for partial refill (2.5 seconds worth)
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMs} ms for partial token refill on key {Key}",
            2500,
            key);
        await Task.Delay(2500);

        // Act - Should have refilled ~2.5 tokens, so ~7.5 total (capped at 10)
        var result = await _store.IsRequestAllowedAsync(key, policy);
        result.Should().BeTrue("Should allow request after partial refill");

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: accumulated refill allowed a new request after {DelayMs} ms",
            nameof(TokenBucket_RefillAccumulatesOverMultipleCalls),
            2500);
    }

    /// <summary>
    /// Verifies that the fixed window count resets when the window boundary is crossed:
    /// after waiting 61 seconds and making 3 more requests, the entry count reflects
    /// only the 3 requests made in the new window.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FixedWindow_WindowBoundary_CorrectlyCalculated()
    {
        // Arrange
        var key = "window-boundary";
        _loggerMock.Object.LogInformation(
            "Starting {TestName} for key {Key} using {Strategy} policy",
            nameof(FixedWindow_WindowBoundary_CorrectlyCalculated),
            key,
            RateLimitStrategy.FixedWindow);

        // Make requests spanning multiple windows
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Get current entry
        var entry1 = await _store.GetEntryAsync(key, _fixedWindowPolicy);

        // Wait for window to advance
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMs} ms for fixed window boundary to advance on key {Key}",
            61000,
            key);
        await Task.Delay(61000);

        // Make more requests
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Get new entry
        var entry2 = await _store.GetEntryAsync(key, _fixedWindowPolicy);

        // Assert - Count should be reset to 3 after window advance
        entry2.Count.Should().Be(3);

        _loggerMock.Object.LogInformation(
            "Completed {TestName}: count before window advance was {PreviousCount}, after advance is {CurrentCount}",
            nameof(FixedWindow_WindowBoundary_CorrectlyCalculated),
            entry1?.Count,
            entry2.Count);
    }
}
