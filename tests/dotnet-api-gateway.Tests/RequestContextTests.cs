#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class RequestContextTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var context = new RequestContext();

        // Assert
        context.RequestId.Should().NotBeEmpty();
        context.Path.Should().Be(string.Empty);
        context.Method.Should().Be(string.Empty);
        context.Headers.Should().BeEmpty();
        context.QueryParameters.Should().BeEmpty();
        context.Body.Should().BeNull();
        context.ClientIp.Should().Be(string.Empty);
        context.AuthToken.Should().BeNull();
        context.ReceivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.CustomData.Should().BeEmpty();
        context.MatchedRoute.Should().BeNull();
        context.SelectedTarget.Should().BeNull();
        context.ClientIdentity.Should().BeNull();
    }

    [Fact]
    public void RequestId_GeneratesUniqueId()
    {
        // Act
        var context1 = new RequestContext();
        var context2 = new RequestContext();

        // Assert
        context1.RequestId.Should().NotBe(context2.RequestId);
        Guid.TryParse(context1.RequestId, out _).Should().BeTrue();
    }

    [Fact]
    public void GetClientIdentifier_WithClientIdentity_ReturnsIdentityId()
    {
        // Arrange
        var context = new RequestContext
        {
            ClientIp = "192.168.1.1",
            ClientIdentity = new ClientIdentity { Id = "user-123" }
        };

        // Act
        var identifier = context.GetClientIdentifier();

        // Assert
        identifier.Should().Be("user-123");
    }

    [Fact]
    public void GetClientIdentifier_NoClientIdentity_ReturnsClientIp()
    {
        // Arrange
        var context = new RequestContext { ClientIp = "192.168.1.1" };

        // Act
        var identifier = context.GetClientIdentifier();

        // Assert
        identifier.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIdentifier_ClientIdentityIdNull_ReturnsClientIp()
    {
        // Arrange
        var context = new RequestContext
        {
            ClientIp = "10.0.0.1",
            ClientIdentity = new ClientIdentity { Id = null }
        };

        // Act
        var identifier = context.GetClientIdentifier();

        // Assert
        identifier.Should().Be("10.0.0.1");
    }

    [Fact]
    public void HasAuthToken_WithToken_ReturnsTrue()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "Bearer xyz123" };

        // Act
        var hasToken = context.HasAuthToken();

        // Assert
        hasToken.Should().BeTrue();
    }

    [Fact]
    public void HasAuthToken_WithoutToken_ReturnsFalse()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        var hasToken = context.HasAuthToken();

        // Assert
        hasToken.Should().BeFalse();
    }

    [Fact]
    public void HasAuthToken_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "" };

        // Act
        var hasToken = context.HasAuthToken();

        // Assert
        hasToken.Should().BeFalse();
    }

    [Fact]
    public void HasAuthToken_WithWhitespaceToken_ReturnsFalse()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "   " };

        // Act
        var hasToken = context.HasAuthToken();

        // Assert
        hasToken.Should().BeFalse();
    }

    [Fact]
    public void ExtractBearerToken_WithBearerPrefix_RemovesPrefix()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "Bearer xyz123token" };

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be("xyz123token");
    }

    [Fact]
    public void ExtractBearerToken_WithLowercaseBearerPrefix_RemovesPrefix()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "bearer xyz123token" };

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be("xyz123token");
    }

    [Fact]
    public void ExtractBearerToken_WithoutBearerPrefix_ReturnsFull()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "xyz123token" };

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be("xyz123token");
    }

    [Fact]
    public void ExtractBearerToken_NoAuthToken_ReturnsEmpty()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be(string.Empty);
    }

    [Fact]
    public void ExtractBearerToken_EmptyAuthToken_ReturnsEmpty()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "" };

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be(string.Empty);
    }

    [Fact]
    public void ElapsedTime_CalculatesTimeDifference()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddSeconds(-5);
        var context = new RequestContext { ReceivedAt = pastTime };

        // Act
        var elapsed = context.ElapsedTime();

        // Assert
        elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(5));
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void ElapsedTime_JustCreated_ReturnsSmallDuration()
    {
        // Arrange
        var context = new RequestContext { ReceivedAt = DateTime.UtcNow };

        // Act
        var elapsed = context.ElapsedTime();

        // Assert
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ElapsedTime_FutureReceivedAt_ReturnsNegativeDuration()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddSeconds(5);
        var context = new RequestContext { ReceivedAt = futureTime };

        // Act
        var elapsed = context.ElapsedTime();

        // Assert
        elapsed.Should().BeLessThan(TimeSpan.Zero);
    }

    [Fact]
    public void Headers_CanBeModified()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.Headers["Content-Type"] = "application/json";
        context.Headers["Authorization"] = "Bearer token";

        // Assert
        context.Headers.Should().HaveCount(2);
        context.Headers["Content-Type"].Should().Be("application/json");
    }

    [Fact]
    public void QueryParameters_CanBeModified()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.QueryParameters["page"] = "1";
        context.QueryParameters["limit"] = "10";

        // Assert
        context.QueryParameters.Should().HaveCount(2);
        context.QueryParameters["page"].Should().Be("1");
    }

    [Fact]
    public void CustomData_CanStoreArbitraryData()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.CustomData["user_id"] = "user-123";
        context.CustomData["transaction_id"] = Guid.NewGuid();
        context.CustomData["metadata"] = new { key = "value" };

        // Assert
        context.CustomData.Should().HaveCount(3);
        context.CustomData["user_id"].Should().Be("user-123");
    }

    [Fact]
    public void MatchedRoute_CanBeSet()
    {
        // Arrange
        var context = new RequestContext();
        var route = new GatewayRoute
        {
            Name = "TestRoute",
            PathPattern = "/api/test",
            AllowedMethods = ["GET"]
        };

        // Act
        context.MatchedRoute = route;

        // Assert
        context.MatchedRoute.Should().Be(route);
        context.MatchedRoute.Name.Should().Be("TestRoute");
    }

    [Fact]
    public void SelectedTarget_CanBeSet()
    {
        // Arrange
        var context = new RequestContext();
        var target = new RouteTarget { Name = "backend", BaseUrl = "http://localhost:8080" };

        // Act
        context.SelectedTarget = target;

        // Assert
        context.SelectedTarget.Should().Be(target);
        context.SelectedTarget.Name.Should().Be("backend");
    }

    [Fact]
    public void ClientIdentity_CanBeSet()
    {
        // Arrange
        var context = new RequestContext();
        var identity = new ClientIdentity
        {
            Id = "user-123",
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        context.ClientIdentity = identity;

        // Assert
        context.ClientIdentity.Should().Be(identity);
        context.ClientIdentity.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void ExtractBearerToken_CaseSensitive_MultipleBearerWords_OnlyRemovesFirstOccurrence()
    {
        // Arrange
        var context = new RequestContext { AuthToken = "Bearer Bearer xyz" };

        // Act
        var token = context.ExtractBearerToken();

        // Assert
        token.Should().Be("Bearer xyz");
    }

    [Fact]
    public void GetClientIdentifier_PrefersClientIdentityOverIp()
    {
        // Arrange
        var context = new RequestContext
        {
            ClientIp = "192.168.1.1",
            ClientIdentity = new ClientIdentity { Id = "user-456" }
        };

        // Act
        var identifier = context.GetClientIdentifier();

        // Assert
        identifier.Should().Be("user-456");
    }

    [Fact]
    public void RequestContext_AllPropertiesMutable()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.Path = "/api/v1/users";
        context.Method = "POST";
        context.Body = "{\"name\":\"John\"}";
        context.ClientIp = "127.0.0.1";
        context.AuthToken = "token123";

        // Assert
        context.Path.Should().Be("/api/v1/users");
        context.Method.Should().Be("POST");
        context.Body.Should().Be("{\"name\":\"John\"}");
        context.ClientIp.Should().Be("127.0.0.1");
        context.AuthToken.Should().Be("token123");
    }
}
