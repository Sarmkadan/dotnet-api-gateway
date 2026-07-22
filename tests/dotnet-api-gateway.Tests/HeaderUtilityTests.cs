#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

/// <summary>
/// Tests for the HeaderUtility class.
/// Tests parsing, case-insensitivity, multi-value headers, and all utility methods.
/// </summary>
public sealed class HeaderUtilityTests
{
    /// <summary>
    /// Tests that GetHeader returns null when headers is null.
    /// </summary>
    [Fact]
    public void GetHeader_NullHeaders_ReturnsNull()
    {
        // Arrange / Act
        var result = HeaderUtility.GetHeader(null!, "Content-Type");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetHeader returns null when headerName is null.
    /// </summary>
    [Fact]
    public void GetHeader_NullHeaderName_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.GetHeader(headers, null!);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetHeader returns null when headerName is empty.
    /// </summary>
    [Fact]
    public void GetHeader_EmptyHeaderName_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.GetHeader(headers, "");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetHeader returns null when headerName is whitespace.
    /// </summary>
    [Fact]
    public void GetHeader_WhitespaceHeaderName_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.GetHeader(headers, "   ");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetHeader returns null when header does not exist.
    /// </summary>
    [Fact]
    public void GetHeader_NonExistentHeader_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.GetHeader(headers, "Non-Existent-Header");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetHeader returns the correct value for an existing header.
    /// </summary>
    [Fact]
    public void GetHeader_ExistingHeader_ReturnsValue()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.GetHeader(headers, "Content-Type");

        // Assert
        result.Should().Be("application/json");
    }

    /// <summary>
    /// Tests case-insensitive header lookup (lowercase header name).
    /// </summary>
    [Fact]
    public void GetHeader_CaseInsensitiveLookup_Lowercase_ReturnsValue()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.GetHeader(headers, "content-type");

        // Assert
        result.Should().Be("application/json");
    }

    /// <summary>
    /// Tests case-insensitive header lookup (mixed case header name).
    /// </summary>
    [Fact]
    public void GetHeader_CaseInsensitiveLookup_MixedCase_ReturnsValue()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.GetHeader(headers, "CoNtEnT-TyPe");

        // Assert
        result.Should().Be("application/json");
    }

    /// <summary>
    /// Tests case-insensitive header lookup (uppercase header name).
    /// </summary>
    [Fact]
    public void GetHeader_CaseInsensitiveLookup_Uppercase_ReturnsValue()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["content-type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.GetHeader(headers, "CONTENT-TYPE");

        // Assert
        result.Should().Be("application/json");
    }

    /// <summary>
    /// Tests that SetHeader sets a new header.
    /// </summary>
    [Fact]
    public void SetHeader_SetsNewHeader()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        HeaderUtility.SetHeader(headers, "X-Custom-Header", "custom-value");

        // Assert
        headers.Should().ContainKey("X-Custom-Header");
        headers["X-Custom-Header"].ToString().Should().Be("custom-value");
    }

    /// <summary>
    /// Tests that SetHeader replaces an existing header value.
    /// </summary>
    [Fact]
    public void SetHeader_ReplacesExistingHeader()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["X-Custom-Header"] = "old-value"
        };

        // Act
        HeaderUtility.SetHeader(headers, "X-Custom-Header", "new-value");

        // Assert
        headers["X-Custom-Header"].ToString().Should().Be("new-value");
    }

    /// <summary>
    /// Tests that SetHeader handles null headers gracefully.
    /// </summary>
    [Fact]
    public void SetHeader_NullHeaders_DoesNotThrow()
    {
        // Arrange / Act
        Action act = () => HeaderUtility.SetHeader(null!, "X-Header", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that SetHeader handles null header name gracefully.
    /// </summary>
    [Fact]
    public void SetHeader_NullHeaderName_DoesNotThrow()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        Action act = () => HeaderUtility.SetHeader(headers, null!, "value");

        // Assert
        act.Should().NotThrow();
        headers.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that SetHeader handles empty header name gracefully.
    /// </summary>
    [Fact]
    public void SetHeader_EmptyHeaderName_DoesNotThrow()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        Action act = () => HeaderUtility.SetHeader(headers, "", "value");

        // Assert
        act.Should().NotThrow();
        headers.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that AddHeader adds a new header.
    /// </summary>
    [Fact]
    public void AddHeader_AddsNewHeader()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        HeaderUtility.AddHeader(headers, "X-Custom-Header", "value1");

        // Assert
        headers.Should().ContainKey("X-Custom-Header");
        headers["X-Custom-Header"].ToString().Should().Be("value1");
    }

    /// <summary>
    /// Tests that AddHeader preserves existing headers (multi-value support).
    /// </summary>
    [Fact]
    public void AddHeader_PreservesExistingHeaders()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["X-Custom-Header"] = "value1"
        };

        // Act
        HeaderUtility.AddHeader(headers, "X-Custom-Header", "value2");

        // Assert
        var values = headers["X-Custom-Header"].ToString();
        values.Should().Contain("value1");
        values.Should().Contain("value2");
    }

    /// <summary>
    /// Tests that AddHeader handles null headers gracefully.
    /// </summary>
    [Fact]
    public void AddHeader_NullHeaders_DoesNotThrow()
    {
        // Arrange / Act
        Action act = () => HeaderUtility.AddHeader(null!, "X-Header", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RemoveHeader removes an existing header.
    /// </summary>
    [Fact]
    public void RemoveHeader_RemovesExistingHeader()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Authorization"] = "Bearer token"
        };

        // Act
        HeaderUtility.RemoveHeader(headers, "Content-Type");

        // Assert
        headers.Should().NotContainKey("Content-Type");
        headers.Should().ContainKey("Authorization");
    }

    /// <summary>
    /// Tests case-insensitive header removal.
    /// </summary>
    [Fact]
    public void RemoveHeader_CaseInsensitiveRemoval()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        HeaderUtility.RemoveHeader(headers, "content-type");

        // Assert
        headers.Should().NotContainKey("Content-Type");
    }

    /// <summary>
    /// Tests that RemoveHeader handles null headers gracefully.
    /// </summary>
    [Fact]
    public void RemoveHeader_NullHeaders_DoesNotThrow()
    {
        // Arrange / Act
        Action act = () => HeaderUtility.RemoveHeader(null!, "X-Header");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RemoveHeader handles null header name gracefully.
    /// </summary>
    [Fact]
    public void RemoveHeader_NullHeaderName_DoesNotThrow()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        Action act = () => HeaderUtility.RemoveHeader(headers, null!);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that HasHeader returns false when header does not exist.
    /// </summary>
    [Fact]
    public void HasHeader_NonExistentHeader_ReturnsFalse()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.HasHeader(headers, "Non-Existent-Header");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasHeader returns true when header exists.
    /// </summary>
    [Fact]
    public void HasHeader_ExistingHeader_ReturnsTrue()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.HasHeader(headers, "Content-Type");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests case-insensitive header existence check.
    /// </summary>
    [Fact]
    public void HasHeader_CaseInsensitiveCheck()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = HeaderUtility.HasHeader(headers, "CONTENT-TYPE");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasHeader handles null headers gracefully.
    /// </summary>
    [Fact]
    public void HasHeader_NullHeaders_ReturnsFalse()
    {
        // Arrange / Act
        var result = HeaderUtility.HasHeader(null!, "X-Header");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasHeader handles null header name gracefully.
    /// </summary>
    [Fact]
    public void HasHeader_NullHeaderName_ReturnsFalse()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.HasHeader(headers, null!);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests ExtractBearerToken with valid Bearer token.
    /// </summary>
    [Fact]
    public void ExtractBearerToken_ValidBearerToken_ReturnsToken()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Authorization"] = "Bearer my-secret-token-123"
        };

        // Act
        var result = HeaderUtility.ExtractBearerToken(headers);

        // Assert
        result.Should().Be("my-secret-token-123");
    }

    /// <summary>
    /// Tests ExtractBearerToken with missing Authorization header.
    /// </summary>
    [Fact]
    public void ExtractBearerToken_MissingAuthorization_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.ExtractBearerToken(headers);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests ExtractBearerToken with non-Bearer authorization.
    /// </summary>
    [Fact]
    public void ExtractBearerToken_NonBearerScheme_ReturnsNull()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Authorization"] = "Basic dXNlcjpwYXNzd29yZA=="
        };

        // Act
        var result = HeaderUtility.ExtractBearerToken(headers);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests ExtractBearerToken with case-insensitive Bearer scheme.
    /// </summary>
    [Fact]
    public void ExtractBearerToken_CaseInsensitiveBearerScheme_ReturnsToken()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Authorization"] = "bearer my-token-456"
        };

        // Act
        var result = HeaderUtility.ExtractBearerToken(headers);

        // Assert
        result.Should().Be("my-token-456");
    }

    /// <summary>
    /// Tests ParseAuthenticationChallenge with valid WWW-Authenticate header.
    /// </summary>
    [Fact]
    public void ParseAuthenticationChallenge_ValidHeader_ReturnsParsedChallenge()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["WWW-Authenticate"] = "Bearer realm=\"example.com\", error=\"invalid_token\""
        };

        // Act
        var result = HeaderUtility.ParseAuthenticationChallenge(headers);

        // Assert
        result.Should().HaveCount(3);
        result["scheme"].Should().Be("Bearer");
        result["realm"].Should().Be("example.com");
        result["error"].Should().Be("invalid_token");
    }

    /// <summary>
    /// Tests ParseAuthenticationChallenge with missing WWW-Authenticate header.
    /// </summary>
    [Fact]
    public void ParseAuthenticationChallenge_MissingHeader_ReturnsEmptyDictionary()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.ParseAuthenticationChallenge(headers);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests ParseAuthenticationChallenge with simple scheme only.
    /// </summary>
    [Fact]
    public void ParseAuthenticationChallenge_SimpleScheme_ReturnsSchemeOnly()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["WWW-Authenticate"] = "Bearer"
        };

        // Act
        var result = HeaderUtility.ParseAuthenticationChallenge(headers);

        // Assert
        result.Should().HaveCount(1);
        result["scheme"].Should().Be("Bearer");
    }

    /// <summary>
    /// Tests ParseAuthenticationChallenge with multiple parameters.
    /// </summary>
    [Fact]
    public void ParseAuthenticationChallenge_MultipleParameters_ReturnsAllParameters()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["WWW-Authenticate"] = "Digest realm=\"testrealm@host.com\", qop=\"auth,auth-int\", nonce=\"dcd98b7102dd2f0e866b1e15425e6b1\", opaque=\"5ccc069c403ebaf9f0171e9517f40e41\""
        };

        // Act
        var result = HeaderUtility.ParseAuthenticationChallenge(headers);

        // Assert
        result.Should().HaveCount(5);
        result["scheme"].Should().Be("Digest");
        result["realm"].Should().Be("testrealm@host.com");
        result["qop"].Should().Be("auth,auth-int");
        result["nonce"].Should().Be("dcd98b7102dd2f0e866b1e15425e6b1");
        result["opaque"].Should().Be("5ccc069c403ebaf9f0171e9517f40e41");
    }

    /// <summary>
    /// Tests GetCustomHeaders filters out standard headers.
    /// </summary>
    [Fact]
    public void GetCustomHeaders_FiltersStandardHeaders_ReturnsOnlyCustomHeaders()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Authorization"] = "Bearer token",
            ["X-Custom-Header"] = "custom-value",
            ["User-Agent"] = "Test-Agent"
        };

        // Act
        var result = HeaderUtility.GetCustomHeaders(headers);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("X-Custom-Header");
        result.Should().ContainKey("Authorization");
        result.Should().NotContainKey("Content-Type");
        result.Should().NotContainKey("User-Agent");
    }

    /// <summary>
    /// Tests GetCustomHeaders with empty headers.
    /// </summary>
    [Fact]
    public void GetCustomHeaders_EmptyHeaders_ReturnsEmptyDictionary()
    {
        // Arrange
        var headers = new HeaderDictionary();

        // Act
        var result = HeaderUtility.GetCustomHeaders(headers);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests GetCustomHeaders with only standard headers.
    /// </summary>
    [Fact]
    public void GetCustomHeaders_OnlyStandardHeaders_ReturnsEmptyDictionary()
    {
        // Arrange
        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json",
            ["Host"] = "example.com"
        };

        // Act
        var result = HeaderUtility.GetCustomHeaders(headers);

        // Assert
        result.Should().BeEmpty();
    }
}
