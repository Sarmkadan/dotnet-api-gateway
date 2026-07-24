#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Tests for HeaderSanitizationUtility - validates header sanitization during
// request/response forwarding to ensure proper header hygiene.
// =============================================================================

namespace DotNetApiGateway.Tests;

using System.Net.Http;
using System.Net.Http.Headers;
using DotNetApiGateway.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

/// <summary>
/// Tests for HeaderSanitizationUtility class.
/// Validates that hop-by-hop headers are removed, sensitive auth headers are blocked,
/// and forwarding headers are properly set.
/// </summary>
public sealed class HeaderSanitizationUtilityTests
{
    /// <summary>
    /// Tests that hop-by-hop headers are properly identified.
    /// </summary>
    [Fact]
    public void GetHopByHopHeaders_ReturnsExpectedHeaders()
    {
        // Arrange / Act
        var hopByHopHeaders = HeaderSanitizationUtility.GetHopByHopHeaders();

        // Assert
        hopByHopHeaders.Should().NotBeEmpty();
        hopByHopHeaders.Should().Contain("Connection");
        hopByHopHeaders.Should().Contain("Keep-Alive");
        hopByHopHeaders.Should().Contain("Proxy-Authorization");
        hopByHopHeaders.Should().Contain("Transfer-Encoding");
        hopByHopHeaders.Should().Contain("Upgrade");
        hopByHopHeaders.Should().Contain("TE");
    }

    /// <summary>
    /// Tests that sensitive auth headers are properly identified.
    /// </summary>
    [Fact]
    public void GetSensitiveAuthHeaders_ReturnsExpectedHeaders()
    {
        // Arrange / Act
        var sensitiveHeaders = HeaderSanitizationUtility.GetSensitiveAuthHeaders();

        // Assert
        sensitiveHeaders.Should().NotBeEmpty();
        sensitiveHeaders.Should().Contain("Authorization");
        sensitiveHeaders.Should().Contain("Cookie");
        sensitiveHeaders.Should().Contain("Set-Cookie");
    }

    /// <summary>
    /// Tests that gateway-internal headers are properly identified.
    /// </summary>
    [Fact]
    public void GetGatewayInternalHeaders_ReturnsExpectedHeaders()
    {
        // Arrange / Act
        var internalHeaders = HeaderSanitizationUtility.GetGatewayInternalHeaders();

        // Assert
        internalHeaders.Should().NotBeEmpty();
        internalHeaders.Should().Contain("X-Forwarded-For");
        internalHeaders.Should().Contain("X-Forwarded-Proto");
        internalHeaders.Should().Contain("X-Forwarded-Host");
        internalHeaders.Should().Contain("Forwarded");
        internalHeaders.Should().Contain("Via");
    }

    /// <summary>
    /// Tests SanitizeForForwarding removes hop-by-hop headers from outgoing request.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_RemovesHopByHopHeaders()
    {
        // Arrange
        var incomingHeaders = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Connection"] = "keep-alive",
            ["Keep-Alive"] = "timeout=5",
            ["Transfer-Encoding"] = "chunked",
            ["Upgrade"] = "websocket",
            ["TE"] = "trailers",
            ["Authorization"] = "Bearer secret-token-123",
            ["X-Custom-Header"] = "custom-value"
        };

        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        // Act
        HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            "192.168.1.100",
            "https",
            outgoingRequest,
            removeHostHeader: false);

        // Assert - incoming headers should have hop-by-hop headers removed
        incomingHeaders.Should().NotContainKey("Connection");
        incomingHeaders.Should().NotContainKey("Keep-Alive");
        incomingHeaders.Should().NotContainKey("Transfer-Encoding");
        incomingHeaders.Should().NotContainKey("Upgrade");
        incomingHeaders.Should().NotContainKey("TE");

        // Assert - Authorization should be removed from incoming headers
        incomingHeaders.Should().NotContainKey("Authorization");

        // Assert - X-Custom-Header should remain in incoming headers
        incomingHeaders.Should().ContainKey("X-Custom-Header");

        // Assert - Outgoing request should have forwarding headers set
        outgoingRequest.Headers.Should().Contain(h => h.Key == "X-Forwarded-For");
        outgoingRequest.Headers.Should().Contain(h => h.Key == "X-Forwarded-Proto");
        outgoingRequest.Headers.Should().Contain(h => h.Key == "Forwarded");

        // Assert - Outgoing request should have X-Forwarded-For with client IP
        var xForwardedFor = outgoingRequest.Headers.GetValues("X-Forwarded-For").First();
        xForwardedFor.Should().Contain("192.168.1.100");

        // Assert - Outgoing request should have X-Forwarded-Proto set to https
        var xForwardedProto = outgoingRequest.Headers.GetValues("X-Forwarded-Proto").First();
        xForwardedProto.Should().Be("https");
    }

    /// <summary>
    /// Tests SanitizeForForwarding sets proper forwarding headers with client IP.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_SetsForwardingHeaders()
    {
        // Arrange
        var incomingHeaders = new HeaderDictionary
        {
            ["Host"] = "gateway.example.com",
            ["X-Forwarded-For"] = "10.0.0.1, 192.168.1.1"
        };

        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://backend.example.com");

        // Act
        HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            "203.0.113.45",
            "https",
            outgoingRequest,
            removeHostHeader: false);

        // Assert - Outgoing request should have X-Forwarded-For with client IP appended
        var xForwardedFor = outgoingRequest.Headers.GetValues("X-Forwarded-For").First();
        xForwardedFor.Should().Be("10.0.0.1, 192.168.1.1, 203.0.113.45");

        // Assert - Outgoing request should have X-Forwarded-Proto set
        var xForwardedProto = outgoingRequest.Headers.GetValues("X-Forwarded-Proto").First();
        xForwardedProto.Should().Be("https");

        // Assert - Outgoing request should have Forwarded header
        var forwarded = outgoingRequest.Headers.GetValues("Forwarded").First();
        forwarded.Should().Contain("for=203.0.113.45");
        forwarded.Should().Contain("proto=https");
        forwarded.Should().Contain("host=gateway.example.com");
    }

    /// <summary>
    /// Tests SanitizeForForwarding prevents Authorization header from reaching backend.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_BlocksAuthorizationHeader()
    {
        // Arrange
        var incomingHeaders = new HeaderDictionary
        {
            ["Authorization"] = "Bearer malicious-token-should-not-reach-backend",
            ["Content-Type"] = "application/json"
        };

        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://backend.example.com");

        // Act
        HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            "192.168.1.100",
            "https",
            outgoingRequest,
            removeHostHeader: false);

        // Assert - Authorization should be removed from incoming headers
        incomingHeaders.Should().NotContainKey("Authorization");

        // Assert - Outgoing request should NOT have Authorization header
        outgoingRequest.Headers.Should().NotContain(h => h.Key == "Authorization");
    }

    /// <summary>
    /// Tests SanitizeForForwarding handles null client IP gracefully.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_NullClientIp_UsesDefault()
    {
        // Arrange
        var incomingHeaders = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        // Act - Should not throw
        Action act = () => HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            null!, // null client IP
            "https",
            outgoingRequest,
            removeHostHeader: false);

        // Assert
        act.Should().NotThrow();
        outgoingRequest.Headers.Should().Contain(h => h.Key == "X-Forwarded-For");
    }

    /// <summary>
    /// Tests SanitizeResponseHeaders removes hop-by-hop headers from response.
    /// </summary>
    [Fact]
    public void SanitizeResponseHeaders_RemovesHopByHopHeaders()
    {
        // Arrange
        var responseHeaders = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Connection"] = "keep-alive",
            ["Keep-Alive"] = "timeout=5",
            ["Transfer-Encoding"] = "chunked",
            ["X-Custom-Security-Header"] = "secure-value"
        };

        // Act
        HeaderSanitizationUtility.SanitizeResponseHeaders(responseHeaders);

        // Assert
        responseHeaders.Should().NotContainKey("Connection");
        responseHeaders.Should().NotContainKey("Keep-Alive");
        responseHeaders.Should().NotContainKey("Transfer-Encoding");
        responseHeaders.Should().ContainKey("Content-Type");
        responseHeaders.Should().ContainKey("X-Custom-Security-Header");
    }

    /// <summary>
    /// Tests SanitizeResponseHeaders removes sensitive auth headers from response.
    /// </summary>
    [Fact]
    public void SanitizeResponseHeaders_RemovesSensitiveHeaders()
    {
        // Arrange
        var responseHeaders = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Authorization"] = "Bearer token",
            ["Set-Cookie"] = "session=abc123"
        };

        // Act
        HeaderSanitizationUtility.SanitizeResponseHeaders(responseHeaders);

        // Assert
        responseHeaders.Should().NotContainKey("Authorization");
        responseHeaders.Should().NotContainKey("Set-Cookie");
        responseHeaders.Should().ContainKey("Content-Type");
    }

    /// <summary>
    /// Tests SanitizeForForwarding with malicious X-Forwarded-For spoofing.
    /// Ensures the gateway's client IP is appended to prevent spoofing.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_PreventsXForwardedForSpoofing()
    {
        // Arrange - malicious client tries to fake their IP
        var incomingHeaders = new HeaderDictionary
        {
            ["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1, 172.16.0.1",
            ["Content-Type"] = "application/json"
        };

        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://backend.example.com");

        // Act - Gateway receives request from actual client IP 203.0.113.45
        HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            "203.0.113.45",
            "https",
            outgoingRequest,
            removeHostHeader: false);

        // Assert - X-Forwarded-For should include the trusted chain plus actual client
        var xForwardedFor = outgoingRequest.Headers.GetValues("X-Forwarded-For").First();

        // Should contain the last IP from the spoofed chain (most recent proxy)
        xForwardedFor.Should().Contain("172.16.0.1");

        // Should contain the actual client IP (this prevents spoofing)
        xForwardedFor.Should().Contain("203.0.113.45");

        // Verify the spoofed IPs are not the only ones present
        // The gateway's actual client IP must be appended
        var ips = xForwardedFor.Split(',').Select(v => v.Trim()).ToList();
        ips.Should().Contain("203.0.113.45");
    }

    /// <summary>
    /// Tests SanitizeForForwarding with empty incoming headers.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_EmptyHeaders_StillSetsForwardingHeaders()
    {
        // Arrange
        var incomingHeaders = new HeaderDictionary();
        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        // Act
        HeaderSanitizationUtility.SanitizeForForwarding(
            incomingHeaders,
            "192.168.1.1",
            "http",
            outgoingRequest,
            removeHostHeader: false);

        // Assert - Forwarding headers should still be set even with empty incoming headers
        outgoingRequest.Headers.Should().Contain(h => h.Key == "X-Forwarded-For");
        outgoingRequest.Headers.Should().Contain(h => h.Key == "X-Forwarded-Proto");
        outgoingRequest.Headers.Should().Contain(h => h.Key == "Forwarded");
    }

    /// <summary>
    /// Tests SanitizeForForwarding handles null arguments with proper exceptions.
    /// </summary>
    [Fact]
    public void SanitizeForForwarding_NullArguments_Throws()
    {
        // Arrange
        var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            HeaderSanitizationUtility.SanitizeForForwarding(null!, "192.168.1.1", "https", outgoingRequest, false));

        Assert.Throws<ArgumentNullException>(() =>
            HeaderSanitizationUtility.SanitizeForForwarding(new HeaderDictionary(), "192.168.1.1", "https", null!, false));
    }

    /// <summary>
    /// Tests SanitizeResponseHeaders handles null arguments with proper exceptions.
    /// </summary>
    [Fact]
    public void SanitizeResponseHeaders_NullArguments_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            HeaderSanitizationUtility.SanitizeResponseHeaders(null!));
    }
}