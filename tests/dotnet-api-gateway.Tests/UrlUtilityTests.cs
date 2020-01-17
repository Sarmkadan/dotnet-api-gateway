#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the UrlUtility class.
/// </summary>
public sealed class UrlUtilityTests
{
    /// <summary>
    /// Tests that combining two URLs with slashes produces a URL with a single slash.
    /// </summary>
    [Fact]
    public void CombineUrl_BothPartsHaveSlashes_ProducesNoDoubleSlash()
    {
        // Arrange / Act
        var result = UrlUtility.CombineUrl("https://api.example.com/", "/v1/users");

        // Assert
        result.Should().Be("https://api.example.com/v1/users");
    }

    /// <summary>
    /// Tests that combining two URLs without slashes joins them with a single slash.
    /// </summary>
    [Fact]
    public void CombineUrl_NeitherPartHasSlash_JoinsWithSingleSlash()
    {
        var result = UrlUtility.CombineUrl("https://api.example.com", "v1/users");
        result.Should().Be("https://api.example.com/v1/users");
    }

    /// <summary>
    /// Tests that combining a URL with an empty path returns the base URL.
    /// </summary>
    [Fact]
    public void CombineUrl_EmptyPath_ReturnsBaseUrl()
    {
        var result = UrlUtility.CombineUrl("https://api.example.com", "");
        result.Should().Be("https://api.example.com");
    }

    /// <summary>
    /// Tests that combining a URL with a null base returns the path.
    /// </summary>
    [Fact]
    public void CombineUrl_NullBase_ReturnsPath()
    {
        var result = UrlUtility.CombineUrl(null!, "/v1/health");
        result.Should().Be("/v1/health");
    }

    /// <summary>
    /// Tests that parsing a query string with encoded values decodes correctly.
    /// </summary>
    [Fact]
    public void ParseQueryString_WithEncodedValues_DecodesCorrectly()
    {
        // Arrange / Act
        var result = UrlUtility.ParseQueryString("?name=John%20Doe&city=New%20York");

        // Assert
        result.Should().ContainKey("name").WhoseValue.Should().Be("John Doe");
        result.Should().ContainKey("city").WhoseValue.Should().Be("New York");
    }

    /// <summary>
    /// Tests that parsing a query string with duplicate keys keeps the first value.
    /// </summary>
    [Fact]
    public void ParseQueryString_WithDuplicateKeys_KeepsFirstValue()
    {
        var result = UrlUtility.ParseQueryString("?color=red&color=blue");
        result["color"].Should().Be("red");
    }

    /// <summary>
    /// Tests that parsing an empty query string returns an empty dictionary.
    /// </summary>
    [Fact]
    public void ParseQueryString_EmptyString_ReturnsEmptyDictionary()
    {
        var result = UrlUtility.ParseQueryString("");
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that parsing a query string with a leading question mark parses correctly.
    /// </summary>
    [Fact]
    public void ParseQueryString_WithLeadingQuestionMark_ParsesCorrectly()
    {
        var result = UrlUtility.ParseQueryString("?page=3&limit=20");
        result.Should().HaveCount(2);
        result["page"].Should().Be("3");
        result["limit"].Should().Be("20");
    }

    /// <summary>
    /// Tests that sanitizing a URL with a sensitive token parameter replaces it with asterisks.
    /// </summary>
    [Fact]
    public void SanitizeUrl_WithSensitiveTokenParam_ReplacesWithAsterisks()
    {
        // Arrange
        var url = "https://api.example.com/data?token=secret123&page=1";

        // Act
        var result = UrlUtility.SanitizeUrl(url);

        // Assert
        result.Should().Contain("token=***");
        result.Should().Contain("page=1");
        result.Should().NotContain("secret123");
    }

    /// <summary>
    /// Tests that sanitizing a URL with non-sensitive parameters preserves all values.
    /// </summary>
    [Fact]
    public void SanitizeUrl_WithNonSensitiveParams_PreservesAllValues()
    {
        var url = "https://api.example.com/data?page=2&limit=50";
        var result = UrlUtility.SanitizeUrl(url);

        result.Should().Contain("page=2");
        result.Should().Contain("limit=50");
        result.Should().NotContain("***");
    }

    /// <summary>
    /// Tests that sanitizing a URL with an API key parameter masks it.
    /// </summary>
    [Fact]
    public void SanitizeUrl_WithApiKeyParam_MasksIt()
    {
        var url = "https://api.example.com/resource?api_key=my-private-key&format=json";
        var result = UrlUtility.SanitizeUrl(url);
        result.Should().Contain("api_key=***");
        result.Should().NotContain("my-private-key");
    }

    /// <summary>
    /// Tests that IsValidUrl returns true for valid URLs and false for invalid ones.
    /// </summary>
    /// <param name="url">The URL to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("https://api.example.com", true)]
    [InlineData("http://localhost:8080/api", true)]
    [InlineData("ftp://files.example.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void IsValidUrl_VariousSchemes_ReturnsExpected(string url, bool expected)
    {
        UrlUtility.IsValidUrl(url).Should().Be(expected);
    }

    /// <summary>
    /// Tests that GetHostname extracts the host from a full URL.
    /// </summary>
    [Fact]
    public void GetHostname_FullUrl_ExtractsHostOnly()
    {
        var result = UrlUtility.GetHostname("https://api.example.com/v1/users?page=1");
        result.Should().Be("api.example.com");
    }

    /// <summary>
    /// Tests that GetHostname returns null for a null URL.
    /// </summary>
    [Fact]
    public void GetHostname_NullUrl_ReturnsNull()
    {
        var result = UrlUtility.GetHostname(null!);
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetPort returns the default port for an HTTPS URL.
    /// </summary>
    [Fact]
    public void GetPort_HttpsUrlWithNoExplicitPort_Returns443()
    {
        var result = UrlUtility.GetPort("https://api.example.com/data");
        result.Should().Be(443);
    }

    /// <summary>
    /// Tests that GetPort returns the default port for an HTTP URL.
    /// </summary>
    [Fact]
    public void GetPort_HttpUrlWithNoExplicitPort_Returns80()
    {
        var result = UrlUtility.GetPort("http://api.example.com/data");
        result.Should().Be(80);
    }

    /// <summary>
    /// Tests that GetPort returns the explicit port for an HTTP URL.
    /// </summary>
    [Fact]
    public void GetPort_HttpUrlWithExplicitPort_ReturnsThatPort()
    {
        var result = UrlUtility.GetPort("http://localhost:3000/api");
        result.Should().Be(3000);
    }

    /// <summary>
    /// Tests that HasQueryParameter returns true for existing query parameters.
    /// </summary>
    [Fact]
    public void HasQueryParameter_ExistingParameter_ReturnsTrue()
    {
        var hasParam = UrlUtility.HasQueryParameter("https://api.example.com/data?page=1&limit=10", "page");
        hasParam.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasQueryParameter returns false for missing query parameters.
    /// </summary>
    [Fact]
    public void HasQueryParameter_MissingParameter_ReturnsFalse()
    {
        var hasParam = UrlUtility.HasQueryParameter("https://api.example.com/data?page=1", "sort");
        hasParam.Should().BeFalse();
    }

    /// <summary>
    /// Tests that BuildQueryString returns an empty string for an empty dictionary.
    /// </summary>
    [Fact]
    public void BuildQueryString_EmptyDictionary_ReturnsEmptyString()
    {
        var result = UrlUtility.BuildQueryString([]);
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that BuildQueryString returns the correct format for a single parameter.
    /// </summary>
    [Fact]
    public void BuildQueryString_SingleParam_ProducesCorrectFormat()
    {
        var result = UrlUtility.BuildQueryString(new Dictionary<string, string> { ["page"] = "1" });
        result.Should().Be("?page=1");
    }
}
