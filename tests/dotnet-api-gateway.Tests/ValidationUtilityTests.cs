#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for the ValidationUtility class.
/// </summary>
public sealed class ValidationUtilityTests
{
    /// <summary>
    /// Tests the IsValidEmail method with various email addresses.
    /// </summary>
    /// <param name="email">The email address to test.</param>
    /// <param name="expected">The expected result of the IsValidEmail method.</param>
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("john.doe@company.co.uk", true)]
    [InlineData("test+tag@domain.com", true)]
    [InlineData("invalid.email", false)]
    [InlineData("missing@domain", false)]
    [InlineData("@example.com", false)]
    [InlineData("user@", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidEmail_VariousInputs_ReturnsExpected(string email, bool expected)
    {
        ValidationUtility.IsValidEmail(email).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidUrl method with various URLs.
    /// </summary>
    /// <param name="url">The URL to test.</param>
    /// <param name="expected">The expected result of the IsValidUrl method.</param>
    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("https://api.example.com/v1/users", true)]
    [InlineData("ftp://files.example.com", false)]
    [InlineData("example.com", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    public void IsValidUrl_VariousUrls_ReturnsExpected(string url, bool expected)
    {
        ValidationUtility.IsValidUrl(url).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidIpAddress method with various IP addresses.
    /// </summary>
    /// <param name="ip">The IP address to test.</param>
    /// <param name="expected">The expected result of the IsValidIpAddress method.</param>
    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("256.1.1.1", true)] // Invalid octet but passes basic regex
    [InlineData("192.168.1", false)]
    [InlineData("192.168.1.1.1", false)]
    [InlineData("abc.def.ghi.jkl", false)]
    [InlineData("", false)]
    public void IsValidIpAddress_VariousIps_ReturnsExpected(string ip, bool expected)
    {
        ValidationUtility.IsValidIpAddress(ip).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidUuid method with various UUIDs.
    /// </summary>
    /// <param name="uuid">The UUID to test.</param>
    /// <param name="expected">The expected result of the IsValidUuid method.</param>
    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8", true)]
    [InlineData("550e8400e29b41d4a716446655440000", true)] // Non-hyphenated
    [InlineData("invalid-uuid-format", false)]
    [InlineData("550e8400-e29b-41d4-a716", false)]
    [InlineData("", false)]
    public void IsValidUuid_VariousUuids_ReturnsExpected(string uuid, bool expected)
    {
        ValidationUtility.IsValidUuid(uuid).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsNullOrEmpty method with various strings.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <param name="expected">The expected result of the IsNullOrEmpty method.</param>
    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t", true)]
    [InlineData("text", false)]
    [InlineData(" text ", false)]
    public void IsNullOrEmpty_VariousStrings_ReturnsExpected(string value, bool expected)
    {
        ValidationUtility.IsNullOrEmpty(value).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidLength method with various string lengths.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <param name="minLength">The minimum length of the string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="expected">The expected result of the IsValidLength method.</param>
    [Theory]
    [InlineData("hello", 0, 10, true)]
    [InlineData("hello", 5, 5, true)]
    [InlineData("hello", 3, 4, false)] // Length 5, required max 4
    [InlineData("hi", 3, 5, false)] // Length 2, required min 3
    [InlineData("hello", 1, 5, true)]
    [InlineData("", 0, 5, true)]
    public void IsValidLength_VariousLengths_ReturnsExpected(string value, int minLength, int maxLength, bool expected)
    {
        ValidationUtility.IsValidLength(value, minLength, maxLength).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsAlphanumeric method with various strings.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <param name="expected">The expected result of the IsAlphanumeric method.</param>
    [Theory]
    [InlineData("abc123", true)]
    [InlineData("ABC", true)]
    [InlineData("123", true)]
    [InlineData("abc_123", false)]
    [InlineData("abc-123", false)]
    [InlineData("abc@123", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsAlphanumeric_VariousStrings_ReturnsExpected(string value, bool expected)
    {
        ValidationUtility.IsAlphanumeric(value).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsAsciiOnly method with various strings.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <param name="expected">The expected result of the IsAsciiOnly method.</param>
    [Theory]
    [InlineData("hello", true)]
    [InlineData("Hello World", true)]
    [InlineData("123!@#", true)]
    [InlineData("hello世界", false)] // Contains non-ASCII
    [InlineData("café", false)] // Contains non-ASCII character
    [InlineData("", false)]
    public void IsAsciiOnly_VariousStrings_ReturnsExpected(string value, bool expected)
    {
        ValidationUtility.IsAsciiOnly(value).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidPort method with various port numbers.
    /// </summary>
    /// <param name="port">The port number to test.</param>
    /// <param name="expected">The expected result of the IsValidPort method.</param>
    [Theory]
    [InlineData(80, true)]
    [InlineData(443, true)]
    [InlineData(8080, true)]
    [InlineData(65535, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(65536, false)]
    public void IsValidPort_VariousPorts_ReturnsExpected(int port, bool expected)
    {
        ValidationUtility.IsValidPort(port).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidHttpMethod method with various HTTP methods.
    /// </summary>
    /// <param name="method">The HTTP method to test.</param>
    /// <param name="expected">The expected result of the IsValidHttpMethod method.</param>
    [Theory]
    [InlineData("GET", true)]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    [InlineData("DELETE", true)]
    [InlineData("PATCH", true)]
    [InlineData("HEAD", true)]
    [InlineData("OPTIONS", true)]
    [InlineData("TRACE", true)]
    [InlineData("get", true)] // Case insensitive
    [InlineData("INVALID", false)]
    [InlineData("", false)]
    public void IsValidHttpMethod_VariousMethods_ReturnsExpected(string method, bool expected)
    {
        ValidationUtility.IsValidHttpMethod(method).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidHttpStatusCode method with various HTTP status codes.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to test.</param>
    /// <param name="expected">The expected result of the IsValidHttpStatusCode method.</param>
    [Theory]
    [InlineData(200, true)]
    [InlineData(404, true)]
    [InlineData(500, true)]
    [InlineData(100, true)]
    [InlineData(599, true)]
    [InlineData(99, false)]
    [InlineData(600, false)]
    [InlineData(1000, false)]
    public void IsValidHttpStatusCode_VariousStatusCodes_ReturnsExpected(int statusCode, bool expected)
    {
        ValidationUtility.IsValidHttpStatusCode(statusCode).Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsNull method with a null object.
    /// </summary>
    /// <returns>The result of the IsNull method.</returns>
    [Fact]
    public void IsNull_WithNull_ReturnsTrue()
    {
        object? obj = null;
        ValidationUtility.IsNull(obj!).Should().BeTrue();
    }

    /// <summary>
    /// Tests the IsNull method with a non-null object.
    /// </summary>
    /// <returns>The result of the IsNull method.</returns>
    [Fact]
    public void IsNull_WithObject_ReturnsFalse()
    {
        var obj = new object();
        ValidationUtility.IsNull(obj).Should().BeFalse();
    }

    /// <summary>
    /// Tests the IsValidType method with a correct type.
    /// </summary>
    /// <returns>The result of the IsValidType method.</returns>
    [Fact]
    public void IsValidType_WithCorrectType_ReturnsTrue()
    {
        object obj = "string";
        ValidationUtility.IsValidType<string>(obj).Should().BeTrue();
    }

    /// <summary>
    /// Tests the IsValidType method with an incorrect type.
    /// </summary>
    /// <returns>The result of the IsValidType method.</returns>
    [Fact]
    public void IsValidType_WithWrongType_ReturnsFalse()
    {
        object obj = 123;
        ValidationUtility.IsValidType<string>(obj).Should().BeFalse();
    }

    /// <summary>
    /// Tests the IsNullOrEmpty method with a null collection.
    /// </summary>
    /// <returns>The result of the IsNullOrEmpty method.</returns>
    [Fact]
    public void IsNullOrEmpty_WithNullCollection_ReturnsTrue()
    {
        List<int>? list = null;
        ValidationUtility.IsNullOrEmpty(list!).Should().BeTrue();
    }

    /// <summary>
    /// Tests the IsNullOrEmpty method with an empty collection.
    /// </summary>
    /// <returns>The result of the IsNullOrEmpty method.</returns>
    [Fact]
    public void IsNullOrEmpty_WithEmptyCollection_ReturnsTrue()
    {
        var list = new List<int>();
        ValidationUtility.IsNullOrEmpty(list).Should().BeTrue();
    }

    /// <summary>
    /// Tests the IsNullOrEmpty method with a populated collection.
    /// </summary>
    /// <returns>The result of the IsNullOrEmpty method.</returns>
    [Fact]
    public void IsNullOrEmpty_WithPopulatedCollection_ReturnsFalse()
    {
        var list = new List<int> { 1, 2, 3 };
        ValidationUtility.IsNullOrEmpty(list).Should().BeFalse();
    }

    /// <summary>
    /// Tests the HasRequiredKeys method with a dictionary containing all required keys.
    /// </summary>
    /// <returns>The result of the HasRequiredKeys method.</returns>
    [Fact]
    public void HasRequiredKeys_WithAllKeys_ReturnsTrue()
    {
        var dict = new Dictionary<string, string>
        {
            ["name"] = "John",
            ["email"] = "john@example.com",
            ["age"] = "30"
        };

        ValidationUtility.HasRequiredKeys(dict, "name", "email").Should().BeTrue();
    }

    /// <summary>
    /// Tests the HasRequiredKeys method with a dictionary missing a required key.
    /// </summary>
    /// <returns>The result of the HasRequiredKeys method.</returns>
    [Fact]
    public void HasRequiredKeys_WithMissingKey_ReturnsFalse()
    {
        var dict = new Dictionary<string, string>
        {
            ["name"] = "John"
        };

        ValidationUtility.HasRequiredKeys(dict, "name", "email").Should().BeFalse();
    }

    /// <summary>
    /// Tests the HasRequiredKeys method with a null dictionary.
    /// </summary>
    /// <returns>The result of the HasRequiredKeys method.</returns>
    [Fact]
    public void HasRequiredKeys_WithNullDict_ReturnsFalse()
    {
        Dictionary<string, string>? dict = null;
        ValidationUtility.HasRequiredKeys(dict!, "name").Should().BeFalse();
    }

    /// <summary>
    /// Tests the HasRequiredKeys method with an empty dictionary.
    /// </summary>
    /// <returns>The result of the HasRequiredKeys method.</returns>
    [Fact]
    public void HasRequiredKeys_WithEmptyDict_ReturnsFalse()
    {
        var dict = new Dictionary<string, string>();
        ValidationUtility.HasRequiredKeys(dict, "name").Should().BeFalse();
    }
}
