#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Configuration;
using DotNetApiGateway.Integration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests.Integration;

/// <summary>
/// Tests for WebhookRegistry class to verify registration, unregistration, and event routing functionality.
/// </summary>
public sealed class WebhookRegistryTests
{
    private readonly Mock<ILogger<WebhookRegistry>> _mockLogger;
    private readonly WebhookRegistry _registry;

    public WebhookRegistryTests()
    {
        _mockLogger = new Mock<ILogger<WebhookRegistry>>();
        var urlValidator = new WebhookCallbackUrlValidator(
            Options.Create(new WebhookSecurityOptions()),
            new Mock<ILogger<WebhookCallbackUrlValidator>>().Object);
        _registry = new WebhookRegistry(_mockLogger.Object, urlValidator);
    }

    /// <summary>
    /// Tests that a webhook can be registered successfully.
    /// </summary>
    [Fact]
    public void Register_ValidSubscription_AddsToRegistry()
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created", "user.updated"],
            CurrentSecret = "secret123"
        };

        // Act
        _registry.Register(subscription);

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().HaveCount(1);
        allSubscriptions[0].Id.Should().Be(subscription.Id);
        allSubscriptions[0].CallbackUrl.Should().Be("https://example.com/webhook");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook registered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that registering a null subscription throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Register_NullSubscription_ThrowsArgumentNullException()
    {
        // Arrange
        global::DotNetApiGateway.Integration.WebhookSubscription? subscription = null;

        // Act
        Action act = () => _registry.Register(subscription!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that multiple subscriptions can be registered.
    /// </summary>
    [Fact]
    public void Register_MultipleSubscriptions_AllAdded()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example1.com/webhook",
            EventTypes = ["user.created"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example2.com/webhook",
            EventTypes = ["user.updated"]
        };
        var subscription3 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example3.com/webhook",
            EventTypes = ["*"]
        };

        // Act
        _registry.Register(subscription1);
        _registry.Register(subscription2);
        _registry.Register(subscription3);

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().HaveCount(3);
    }

    /// <summary>
    /// Tests that duplicate registration is allowed (adds multiple subscriptions with same callback).
    /// </summary>
    [Fact]
    public void Register_DuplicateSubscription_Allowed()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.updated"]
        };

        // Act
        _registry.Register(subscription1);
        _registry.Register(subscription2);

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().HaveCount(2);
        allSubscriptions[0].Id.Should().NotBe(allSubscriptions[1].Id);
    }

    /// <summary>
    /// Tests that a webhook can be unregistered successfully.
    /// </summary>
    [Fact]
    public void Unregister_ExistingSubscription_RemovesFromRegistry()
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created"]
        };
        _registry.Register(subscription);

        // Act
        _registry.Unregister(subscription.Id);

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().BeEmpty();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook unregistered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that unregistering with null or empty subscriptionId does nothing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unregister_InvalidSubscriptionId_DoesNothing(string? subscriptionId)
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created"]
        };
        _registry.Register(subscription);

        // Act
        _registry.Unregister(subscriptionId!);

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that unregistering a non-existent subscription does nothing.
    /// </summary>
    [Fact]
    public void Unregister_NonExistentSubscription_DoesNothing()
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created"]
        };
        _registry.Register(subscription);

        // Act
        _registry.Unregister("non-existent-id");

        // Assert
        var allSubscriptions = _registry.GetAllSubscriptions();
        allSubscriptions.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that GetSubscriptionsForEvent returns subscriptions matching specific event type.
    /// </summary>
    [Fact]
    public void GetSubscriptionsForEvent_SpecificEventType_ReturnsMatchingSubscriptions()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example1.com/webhook",
            EventTypes = ["user.created", "user.updated"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example2.com/webhook",
            EventTypes = ["user.deleted"]
        };
        var subscription3 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example3.com/webhook",
            EventTypes = ["*"]
        };
        var subscription4 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example4.com/webhook",
            EventTypes = ["order.created", "order.updated"]
        };

        _registry.Register(subscription1);
        _registry.Register(subscription2);
        _registry.Register(subscription3);
        _registry.Register(subscription4);

        // Act
        var result = _registry.GetSubscriptionsForEvent("user.created");

        // Assert
        result.Should().HaveCount(2); // subscription1, subscription3 (wildcard)
        result.Should().Contain(subscription1);
        result.Should().Contain(subscription3);
        result.Should().NotContain(subscription2); // Doesn't match event type
        result.Should().NotContain(subscription4); // Doesn't match event type
    }

    /// <summary>
    /// Tests that GetSubscriptionsForEvent returns subscriptions matching wildcard event type.
    /// </summary>
    [Fact]
    public void GetSubscriptionsForEvent_WildcardEventType_ReturnsAllActiveSubscriptions()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example1.com/webhook",
            EventTypes = ["user.created"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example2.com/webhook",
            EventTypes = ["order.created"]
        };
        var subscription3 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example3.com/webhook",
            EventTypes = ["*"]
        };

        _registry.Register(subscription1);
        _registry.Register(subscription2);
        _registry.Register(subscription3);

        // Act
        var result = _registry.GetSubscriptionsForEvent("user.created");

        // Assert
        result.Should().HaveCount(2); // subscription1 and subscription3 (wildcard), subscription2 doesn't match
    }

    /// <summary>
    /// Tests that GetSubscriptionsForEvent returns empty list for null or empty event type.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSubscriptionsForEvent_InvalidEventType_ReturnsEmptyList(string? eventType)
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created"]
        };
        _registry.Register(subscription);

        // Act
        var result = _registry.GetSubscriptionsForEvent(eventType!);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetSubscriptionsForEvent returns empty list when no subscriptions exist.
    /// </summary>
    [Fact]
    public void GetSubscriptionsForEvent_NoSubscriptions_ReturnsEmptyList()
    {
        // Act
        var result = _registry.GetSubscriptionsForEvent("user.created");

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetAllSubscriptions returns all registered subscriptions.
    /// </summary>
    [Fact]
    public void GetAllSubscriptions_ReturnsAllSubscriptions()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example1.com/webhook",
            EventTypes = ["user.created"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example2.com/webhook",
            EventTypes = ["order.created"]
        };

        _registry.Register(subscription1);
        _registry.Register(subscription2);

        // Act
        var result = _registry.GetAllSubscriptions();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(subscription1);
        result.Should().Contain(subscription2);
    }

    /// <summary>
    /// Tests that GetAllSubscriptions returns empty list when no subscriptions exist.
    /// </summary>
    [Fact]
    public void GetAllSubscriptions_NoSubscriptions_ReturnsEmptyList()
    {
        // Act
        var result = _registry.GetAllSubscriptions();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that subscriptions maintain their state after multiple operations.
    /// </summary>
    [Fact]
    public void MultipleOperations_MaintainsCorrectState()
    {
        // Arrange
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example1.com/webhook",
            EventTypes = ["user.created"]
        };
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example2.com/webhook",
            EventTypes = ["user.updated"]
        };

        // Act & Assert - Chain multiple operations
        _registry.Register(subscription1);
        var all1 = _registry.GetAllSubscriptions();
        all1.Should().HaveCount(1);

        _registry.Register(subscription2);
        var all2 = _registry.GetAllSubscriptions();
        all2.Should().HaveCount(2);

        _registry.Unregister(subscription1.Id);
        var all3 = _registry.GetAllSubscriptions();
        all3.Should().HaveCount(1);
        all3[0].Id.Should().Be(subscription2.Id);

        var forEvent = _registry.GetSubscriptionsForEvent("user.updated");
        forEvent.Should().HaveCount(1);
        forEvent[0].Id.Should().Be(subscription2.Id);
    }

    /// <summary>
    /// Tests that WebhookSubscription properties are preserved correctly.
    /// </summary>
    [Fact]
    public void WebhookSubscription_PropertiesPreserved()
    {
        // Arrange
        var retryPolicy = new global::DotNetApiGateway.Integration.WebhookRetryPolicy
        {
            MaxRetries = 5,
            InitialDelayMs = 2000,
            MaxDelayMs = 120000
        };

        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription
        {
            CallbackUrl = "https://example.com/webhook",
            EventTypes = ["user.created", "user.updated", "order.*"],
            CurrentSecret = "my-secret-key",
            Active = false,
            RetryPolicy = retryPolicy
        };

        // Act
        _registry.Register(subscription);

        // Assert
        var retrieved = _registry.GetAllSubscriptions()[0];
        retrieved.Id.Should().NotBeEmpty();
        retrieved.CallbackUrl.Should().Be("https://example.com/webhook");
        retrieved.EventTypes.Should().BeEquivalentTo(["user.created", "user.updated", "order.*"]);
        retrieved.CurrentSecret.Should().Be("my-secret-key");
        retrieved.Active.Should().BeFalse();
        retrieved.RetryPolicy.MaxRetries.Should().Be(5);
        retrieved.RetryPolicy.InitialDelayMs.Should().Be(2000);
        retrieved.RetryPolicy.MaxDelayMs.Should().Be(120000);
        retrieved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that each subscription gets a unique ID on creation.
    /// </summary>
    [Fact]
    public void WebhookSubscription_UniqueIdsGenerated()
    {
        // Arrange & Act
        var subscription1 = new global::DotNetApiGateway.Integration.WebhookSubscription();
        var subscription2 = new global::DotNetApiGateway.Integration.WebhookSubscription();
        var subscription3 = new global::DotNetApiGateway.Integration.WebhookSubscription();

        // Assert
        subscription1.Id.Should().NotBe(subscription2.Id);
        subscription1.Id.Should().NotBe(subscription3.Id);
        subscription2.Id.Should().NotBe(subscription3.Id);

        Guid.Parse(subscription1.Id).Should().NotBe(Guid.Empty);
        Guid.Parse(subscription2.Id).Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Tests that default WebhookRetryPolicy values are correct.
    /// </summary>
    [Fact]
    public void WebhookRetryPolicy_DefaultValues()
    {
        // Arrange
        var subscription = new global::DotNetApiGateway.Integration.WebhookSubscription();

        // Act
        _registry.Register(subscription);

        // Assert
        var retrieved = _registry.GetAllSubscriptions()[0];
        retrieved.RetryPolicy.MaxRetries.Should().Be(3);
        retrieved.RetryPolicy.InitialDelayMs.Should().Be(1000);
        retrieved.RetryPolicy.MaxDelayMs.Should().Be(60000);
    }
}
