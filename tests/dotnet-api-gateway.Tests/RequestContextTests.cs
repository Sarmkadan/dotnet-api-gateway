// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using FluentAssertions;

namespace DotNetApiGateway.Tests;

public class RequestContextTests
{
    [Fact]
    public void Constructor_GeneratesUniqueRequestId()
    {
        var ctx1 = new RequestContext();
        var ctx2 = new RequestContext();
        ctx1.RequestId.Should().NotBe(ctx2.RequestId);
    }

    [Fact]
    public void Constructor_ReceivedAtIsSetToNow()
    {
        var before = DateTime.UtcNow;
        var ctx = new RequestContext();
        ctx.ReceivedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void GetClientIdentifier_WithClientIdentity_ReturnsIdentityId()
    {
        var ctx = new RequestContext
        {
            ClientIp = "10.0.0.1",
            ClientIdentity = new ClientIdentity { Id = "api-client-1" }
        };
        ctx.GetClientIdentifier().Should().Be("api-client-1");
    }

    [Fact]
    public void GetClientIdentifier_WithoutClientIdentity_ReturnsClientIp()
    {
        var ctx = new RequestContext { ClientIp = "10.0.0.1" };
        ctx.GetClientIdentifier().Should().Be("10.0.0.1");
    }

    [Fact]
    public void HasAuthToken_NullToken_ReturnsFalse()
    {
        var ctx = new RequestContext { AuthToken = null };
        ctx.HasAuthToken().Should().BeFalse();
    }

    [Fact]
    public void HasAuthToken_EmptyToken_ReturnsFalse()
    {
        var ctx = new RequestContext { AuthToken = "" };
        ctx.HasAuthToken().Should().BeFalse();
    }

    [Fact]
    public void HasAuthToken_WhitespaceToken_ReturnsFalse()
    {
        var ctx = new RequestContext { AuthToken = "   " };
        ctx.HasAuthToken().Should().BeFalse();
    }

    [Fact]
    public void HasAuthToken_ValidToken_ReturnsTrue()
    {
        var ctx = new RequestContext { AuthToken = "Bearer abc123" };
        ctx.HasAuthToken().Should().BeTrue();
    }

    [Fact]
    public void ExtractBearerToken_BearerPrefixed_ReturnsTokenWithoutPrefix()
    {
        var ctx = new RequestContext { AuthToken = "Bearer eyJhbGc" };
        ctx.ExtractBearerToken().Should().Be("eyJhbGc");
    }

    [Fact]
    public void ExtractBearerToken_NoBearerPrefix_ReturnsFullToken()
    {
        var ctx = new RequestContext { AuthToken = "some-api-key" };
        ctx.ExtractBearerToken().Should().Be("some-api-key");
    }

    [Fact]
    public void ExtractBearerToken_NoToken_ReturnsEmpty()
    {
        var ctx = new RequestContext { AuthToken = null };
        ctx.ExtractBearerToken().Should().BeEmpty();
    }

    [Fact]
    public void ExtractBearerToken_CaseInsensitivePrefix()
    {
        var ctx = new RequestContext { AuthToken = "bearer abc" };
        ctx.ExtractBearerToken().Should().Be("abc");
    }

    [Fact]
    public void ElapsedTime_ReturnsPositiveTimeSpan()
    {
        var ctx = new RequestContext();
        Thread.Sleep(10);
        ctx.ElapsedTime().TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CustomData_DefaultsToEmptyDictionary()
    {
        var ctx = new RequestContext();
        ctx.CustomData.Should().BeEmpty();
    }

    [Fact]
    public void Headers_DefaultsToEmptyDictionary()
    {
        var ctx = new RequestContext();
        ctx.Headers.Should().BeEmpty();
    }
}
