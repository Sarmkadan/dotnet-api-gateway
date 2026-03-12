#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class RateLimitingServiceTests
{
    private static RateLimitPolicy ValidPolicy(int requestsPerMinute = 10) => new()
    {
        Enabled = true,
        RequestsPerMinute = requestsPerMinute,
        Strategy = RateLimitStrategy.SlidingWindow
    };

    [Fact]
    public async Task IsAllowedAsync_DisabledPolicy_ReturnsTrue()
    {
        // Arrange
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = new RateLimitPolicy { Enabled = false };

        // Act
        var result = await service.IsAllowedAsync("client-1", policy);

        // Assert
        result.Should().BeTrue();
        mockFactory.Verify(f => f.GetStore(It.IsAny<RateLimitPolicy>()), Times.Never);
    }

    [Fact]
    public async Task IsAllowedAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);
        mockStore.Setup(s => s.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(true);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = ValidPolicy();

        // Act
        var result = await service.IsAllowedAsync("client-1", policy);

        // Assert
        result.Should().BeTrue();
        mockStore.Verify(s => s.IsRequestAllowedAsync("client-1", policy), Times.Once);
    }

    [Fact]
    public async Task IsAllowedAsync_RateLimitExceeded_ReturnsFalse()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);
        mockStore.Setup(s => s.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(false);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = ValidPolicy();

        // Act
        var result = await service.IsAllowedAsync("client-1", policy);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRateLimitInfoAsync_SlidingWindowStrategy_CalculatesRemaining()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var entry = new RateLimitEntry { Count = 5, RemainingTimeSeconds = 30 };
        mockStore.Setup(s => s.GetEntryAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(entry);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = new RateLimitPolicy { Enabled = true, RequestsPerMinute = 10, Strategy = RateLimitStrategy.SlidingWindow };

        // Act
        var info = await service.GetRateLimitInfoAsync("client-1", policy);

        // Assert
        info.Limit.Should().Be(10);
        info.Remaining.Should().Be(5); // 10 - 5
        info.Reset.Should().Be(30);
    }

    [Fact]
    public async Task GetRateLimitInfoAsync_TokenBucketStrategy_UsesTokensRemaining()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var entry = new RateLimitEntry { Tokens = 7.5, RemainingTimeSeconds = 45 };
        mockStore.Setup(s => s.GetEntryAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(entry);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = new RateLimitPolicy
        {
            Enabled = true,
            BurstSize = 20,
            Strategy = RateLimitStrategy.TokenBucket
        };

        // Act
        var info = await service.GetRateLimitInfoAsync("client-1", policy);

        // Assert
        info.Limit.Should().Be(20);
        info.Remaining.Should().Be(7);
        info.Reset.Should().Be(45);
    }

    [Fact]
    public async Task ResetKeyLimitsAsync_CallsResetOnAllStores()
    {
        // Arrange
        var mockStore1 = new Mock<IRateLimitStore>();
        var mockStore2 = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetAllStores()).Returns(new[] { mockStore1.Object, mockStore2.Object });

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);

        // Act
        await service.ResetKeyLimitsAsync("client-1");

        // Assert
        mockStore1.Verify(s => s.ResetKeyAsync("client-1"), Times.Once);
        mockStore2.Verify(s => s.ResetKeyAsync("client-1"), Times.Once);
    }

    [Fact]
    public async Task ResetAllLimitsAsync_CallsResetOnAllStores()
    {
        // Arrange
        var mockStore1 = new Mock<IRateLimitStore>();
        var mockStore2 = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetAllStores()).Returns(new[] { mockStore1.Object, mockStore2.Object });

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);

        // Act
        await service.ResetAllLimitsAsync();

        // Assert
        mockStore1.Verify(s => s.ResetAllAsync(), Times.Once);
        mockStore2.Verify(s => s.ResetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task IsAllowedAsync_MultipleClients_TracksIndividually()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        mockStore.Setup(s => s.IsRequestAllowedAsync("client-1", It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(true);
        mockStore.Setup(s => s.IsRequestAllowedAsync("client-2", It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(false);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = ValidPolicy();

        // Act
        var result1 = await service.IsAllowedAsync("client-1", policy);
        var result2 = await service.IsAllowedAsync("client-2", policy);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public void Dispose_DisposesFactory()
    {
        // Arrange
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetAllStores()).Returns(Array.Empty<IRateLimitStore>());
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);

        // Act
        service.Dispose();

        // Assert
        mockFactory.Verify(f => f.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetRateLimitInfoAsync_ZeroRequestCount_HasFullRemaining()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var entry = new RateLimitEntry { Count = 0, RemainingTimeSeconds = 60 };
        mockStore.Setup(s => s.GetEntryAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(entry);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = ValidPolicy(requestsPerMinute: 5);

        // Act
        var info = await service.GetRateLimitInfoAsync("client-1", policy);

        // Assert
        info.Limit.Should().Be(5);
        info.Remaining.Should().Be(5);
    }

    [Fact]
    public async Task GetRateLimitInfoAsync_MaxedOutRequests_ZeroRemaining()
    {
        // Arrange
        var mockStore = new Mock<IRateLimitStore>();
        var mockFactory = new Mock<IRateLimitStoreFactory>();
        mockFactory.Setup(f => f.GetStore(It.IsAny<RateLimitPolicy>())).Returns(mockStore.Object);

        var entry = new RateLimitEntry { Count = 10, RemainingTimeSeconds = 15 };
        mockStore.Setup(s => s.GetEntryAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>()))
            .ReturnsAsync(entry);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RateLimitingService>>();
        var service = new RateLimitingService(mockFactory.Object, logger.Object);
        var policy = ValidPolicy(requestsPerMinute: 10);

        // Act
        var info = await service.GetRateLimitInfoAsync("client-1", policy);

        // Assert
        info.Remaining.Should().Be(0);
    }
}
