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

public class InMemoryRateLimitStoreTests
{
    private readonly Mock<ILogger<InMemoryRateLimitStore>> _loggerMock;
    private readonly InMemoryRateLimitStore _store;
    private readonly RateLimitPolicy _tokenBucketPolicy;
    private readonly RateLimitPolicy _fixedWindowPolicy;
    private readonly RateLimitPolicy _slidingWindowPolicy;

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

    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "test-client-1";

        // Act & Assert - First 10 requests should be allowed (burst size)
        for (int i = 0; i < 10; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under burst limit");
        }
    }

    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "test-client-2";

        // Fill the token bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        }

        // Act - 11th request should be blocked
        var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);

        // Assert
        result.Should().BeFalse("Token bucket should block requests after burst size is exceeded");
    }

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

        // Fill the bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, policy);
        }

        // Act - Wait for tokens to refill (simulate time passing)
        await Task.Delay(1100); // Wait 1.1 seconds

        // Assert - Should have 1 token refilled
        var result = await _store.IsRequestAllowedAsync(key, policy);
        result.Should().BeTrue("Should allow request after token refill");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_TokenBucket_IndependentKeys_DoNotInterfere()
    {
        // Arrange
        var key1 = "client-1";
        var key2 = "client-2";

        // Fill key1
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        }

        // key2 should still be able to make requests
        var result = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result.Should().BeTrue("Different keys should have independent rate limits");

        // key1 should be blocked
        result = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        result.Should().BeFalse("Same key should be blocked after burst size exceeded");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "fixed-window-client-1";

        // Act & Assert - First 5 requests should be allowed
        for (int i = 0; i < 5; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under fixed window limit");
        }
    }

    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "fixed-window-client-2";

        // Fill the fixed window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Act - 6th request should be blocked
        var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);

        // Assert
        result.Should().BeFalse("Fixed window should block requests after limit is exceeded");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_FixedWindow_WindowReset_RestoresAllowance()
    {
        // Arrange
        var key = "fixed-window-reset-client";

        // Fill the window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Act - Wait for window to reset (61 seconds for minute window)
        await Task.Delay(61000);

        // Assert - Should be able to make requests again
        var result = await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        result.Should().BeTrue("Fixed window should restore allowance after reset");
    }

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

        // Fill the hour window
        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key, hourPolicy);
        }

        // Act - 6th request should be blocked
        var result = await _store.IsRequestAllowedAsync(key, hourPolicy);
        result.Should().BeFalse("Hour window should block requests after limit exceeded");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_UnderLimit_AllowsRequest()
    {
        // Arrange
        var key = "sliding-client-1";

        // Act & Assert - First 3 requests should be allowed
        for (int i = 0; i < 3; i++)
        {
            var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
            result.Should().BeTrue($"Request {i + 1} should be allowed under sliding window limit");
        }
    }

    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_OverLimit_BlocksRequest()
    {
        // Arrange
        var key = "sliding-client-2";

        // Fill the sliding window
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        }

        // Act - 4th request should be blocked
        var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);

        // Assert
        result.Should().BeFalse("Sliding window should block requests after limit is exceeded");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_OldRequestsExpire_AllowsAfterWait()
    {
        // Arrange
        var key = "sliding-expiry-client";

        // Make 3 requests
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        }

        // Wait for oldest request to expire (61 seconds)
        await Task.Delay(61000);

        // Act - Should be able to make another request
        var result = await _store.IsRequestAllowedAsync(key, _slidingWindowPolicy);
        result.Should().BeTrue("Sliding window should allow request after oldest request expires");
    }

    [Fact]
    public async Task IsRequestAllowedAsync_SlidingWindow_IndependentKeys_DoNotInterfere()
    {
        // Arrange
        var key1 = "sliding-key1";
        var key2 = "sliding-key2";

        // Fill key1
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _slidingWindowPolicy);
        }

        // key2 should still be able to make requests
        var result = await _store.IsRequestAllowedAsync(key2, _slidingWindowPolicy);
        result.Should().BeTrue("Different keys should have independent sliding window limits");

        // key1 should be blocked
        result = await _store.IsRequestAllowedAsync(key1, _slidingWindowPolicy);
        result.Should().BeFalse("Same key should be blocked after sliding window limit exceeded");
    }

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
    }

    [Fact]
    public async Task GetEntryAsync_FixedWindow_ReturnsCorrectEntry()
    {
        // Arrange
        var key = "get-entry-fixed-window-test";

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
    }

    [Fact]
    public async Task GetEntryAsync_SlidingWindow_ReturnsCorrectEntry()
    {
        // Arrange
        var key = "get-entry-sliding-window";

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
    }

    [Fact]
    public async Task GetEntryAsync_NonExistentKey_ReturnsNewEntry()
    {
        // Arrange
        var key = "non-existent-key-test";

        // Act
        var entry = await _store.GetEntryAsync(key, _tokenBucketPolicy);

        // Assert
        entry.Should().NotBeNull();
        entry.Key.Should().Be(key);
        entry.Count.Should().Be(0);
        entry.Tokens.Should().BeGreaterThan(-1);
        entry.RemainingTimeSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ResetKeyAsync_ClearsRateLimitForSpecificKey()
    {
        // Arrange
        var key = "reset-key";

        // Fill the bucket
        for (int i = 0; i < 10; i++)
        {
            await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        }

        // Verify it's blocked
        var result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        result.Should().BeFalse("Should be blocked before reset");

        // Act
        await _store.ResetKeyAsync(key);

        // Assert - Should be allowed again after reset
        result = await _store.IsRequestAllowedAsync(key, _tokenBucketPolicy);
        result.Should().BeTrue("Should be allowed after key reset");
    }

    [Fact]
    public async Task ResetAllAsync_ClearsAllRateLimits()
    {
        // Arrange - Create multiple keys with rate limits
        var key1 = "all-reset-key1";
        var key2 = "all-reset-key2";

        for (int i = 0; i < 5; i++)
        {
            await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
            await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        }

        // Verify both are blocked
        var result1 = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        var result2 = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result1.Should().BeFalse("Key1 should be blocked before reset");
        result2.Should().BeFalse("Key2 should be blocked before reset");

        // Act
        await _store.ResetAllAsync();

        // Assert - Both should be allowed again
        result1 = await _store.IsRequestAllowedAsync(key1, _tokenBucketPolicy);
        result2 = await _store.IsRequestAllowedAsync(key2, _tokenBucketPolicy);
        result1.Should().BeTrue("Key1 should be allowed after all reset");
        result2.Should().BeTrue("Key2 should be allowed after all reset");
    }

    [Fact]
    public async Task GetAllEntriesAsync_ReturnsAllActiveEntries()
    {
        // Arrange - Create multiple keys
        var key1 = "all-entries-key1";
        var key2 = "all-entries-key2";

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
    }

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
        await Task.Delay(2500);

        // Act - Should have refilled ~2.5 tokens, so ~7.5 total (capped at 10)
        var result = await _store.IsRequestAllowedAsync(key, policy);
        result.Should().BeTrue("Should allow request after partial refill");
    }

    [Fact]
    public async Task FixedWindow_WindowBoundary_CorrectlyCalculated()
    {
        // Arrange
        var key = "window-boundary";

        // Make requests spanning multiple windows
        for (int i = 0; i < 3; i++)
        {
            await _store.IsRequestAllowedAsync(key, _fixedWindowPolicy);
        }

        // Get current entry
        var entry1 = await _store.GetEntryAsync(key, _fixedWindowPolicy);

        // Wait for window to advance
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
    }
}