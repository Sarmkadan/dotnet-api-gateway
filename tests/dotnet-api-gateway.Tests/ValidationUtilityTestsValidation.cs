#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation extension methods for <see cref="ValidationUtilityTests"/> instances.
/// </summary>
public static class ValidationUtilityTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="ValidationUtilityTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ValidationUtilityTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Test IsValidEmail with various inputs
        if (!ValidationUtility.IsValidEmail("user@example.com"))
            problems.Add("IsValidEmail failed for valid email 'user@example.com'");
        if (!ValidationUtility.IsValidEmail("john.doe@company.co.uk"))
            problems.Add("IsValidEmail failed for valid email 'john.doe@company.co.uk'");
        if (ValidationUtility.IsValidEmail("invalid.email"))
            problems.Add("IsValidEmail returned true for invalid email 'invalid.email'");
        if (ValidationUtility.IsValidEmail(""))
            problems.Add("IsValidEmail returned true for empty string");

        // Test IsValidUrl with various inputs
        if (!ValidationUtility.IsValidUrl("https://example.com"))
            problems.Add("IsValidUrl failed for valid URL 'https://example.com'");
        if (!ValidationUtility.IsValidUrl("http://localhost:8080"))
            problems.Add("IsValidUrl failed for valid URL 'http://localhost:8080'");
        if (ValidationUtility.IsValidUrl("example.com"))
            problems.Add("IsValidUrl returned true for invalid URL 'example.com'");
        if (ValidationUtility.IsValidUrl(""))
            problems.Add("IsValidUrl returned true for empty string");

        // Test IsValidIpAddress with various inputs
        if (!ValidationUtility.IsValidIpAddress("192.168.1.1"))
            problems.Add("IsValidIpAddress failed for valid IP '192.168.1.1'");
        if (!ValidationUtility.IsValidIpAddress("10.0.0.1"))
            problems.Add("IsValidIpAddress failed for valid IP '10.0.0.1'");
        if (ValidationUtility.IsValidIpAddress("256.1.1.1"))
            problems.Add("IsValidIpAddress returned true for invalid IP '256.1.1.1'");
        if (ValidationUtility.IsValidIpAddress(""))
            problems.Add("IsValidIpAddress returned true for empty string");

        // Test IsValidUuid with various inputs
        if (!ValidationUtility.IsValidUuid("550e8400-e29b-41d4-a716-446655440000"))
            problems.Add("IsValidUuid failed for valid UUID '550e8400-e29b-41d4-a716-446655440000'");
        if (!ValidationUtility.IsValidUuid("6ba7b810-9dad-11d1-80b4-00c04fd430c8"))
            problems.Add("IsValidUuid failed for valid UUID '6ba7b810-9dad-11d1-80b4-00c04fd430c8'");
        if (ValidationUtility.IsValidUuid("invalid-uuid-format"))
            problems.Add("IsValidUuid returned true for invalid UUID 'invalid-uuid-format'");
        if (ValidationUtility.IsValidUuid(""))
            problems.Add("IsValidUuid returned true for empty string");

        // Test IsNullOrEmpty with various inputs
        if (!ValidationUtility.IsNullOrEmpty(""))
            problems.Add("IsNullOrEmpty returned false for empty string");
        if (!ValidationUtility.IsNullOrEmpty(" "))
            problems.Add("IsNullOrEmpty returned false for whitespace string");
        if (ValidationUtility.IsNullOrEmpty("text"))
            problems.Add("IsNullOrEmpty returned true for non-empty string 'text'");

        // Test IsValidLength with various inputs
        if (!ValidationUtility.IsValidLength("hello", 0, 10))
            problems.Add("IsValidLength failed for valid length 'hello' with range 0-10");
        if (!ValidationUtility.IsValidLength("hello", 5, 5))
            problems.Add("IsValidLength failed for valid length 'hello' with range 5-5");
        if (ValidationUtility.IsValidLength("hello", 3, 4))
            problems.Add("IsValidLength returned true for invalid length 'hello' with range 3-4");
        if (ValidationUtility.IsValidLength("hi", 3, 5))
            problems.Add("IsValidLength returned true for invalid length 'hi' with range 3-5");

        // Test IsAlphanumeric with various inputs
        if (!ValidationUtility.IsAlphanumeric("abc123"))
            problems.Add("IsAlphanumeric failed for valid alphanumeric string 'abc123'");
        if (!ValidationUtility.IsAlphanumeric("ABC"))
            problems.Add("IsAlphanumeric failed for valid alphanumeric string 'ABC'");
        if (ValidationUtility.IsAlphanumeric("abc_123"))
            problems.Add("IsAlphanumeric returned true for non-alphanumeric string 'abc_123'");
        if (ValidationUtility.IsAlphanumeric(""))
            problems.Add("IsAlphanumeric returned true for empty string");

        // Test IsAsciiOnly with various inputs
        if (!ValidationUtility.IsAsciiOnly("hello"))
            problems.Add("IsAsciiOnly failed for ASCII-only string 'hello'");
        if (!ValidationUtility.IsAsciiOnly("Hello World"))
            problems.Add("IsAsciiOnly failed for ASCII-only string 'Hello World'");
        if (ValidationUtility.IsAsciiOnly("hello世界"))
            problems.Add("IsAsciiOnly returned true for non-ASCII string 'hello世界'");
        if (ValidationUtility.IsAsciiOnly(""))
            problems.Add("IsAsciiOnly returned true for empty string");

        // Test IsValidPort with various inputs
        if (!ValidationUtility.IsValidPort(80))
            problems.Add("IsValidPort failed for valid port 80");
        if (!ValidationUtility.IsValidPort(443))
            problems.Add("IsValidPort failed for valid port 443");
        if (!ValidationUtility.IsValidPort(65535))
            problems.Add("IsValidPort failed for valid port 65535");
        if (ValidationUtility.IsValidPort(0))
            problems.Add("IsValidPort returned true for invalid port 0");
        if (ValidationUtility.IsValidPort(-1))
            problems.Add("IsValidPort returned true for invalid port -1");
        if (ValidationUtility.IsValidPort(65536))
            problems.Add("IsValidPort returned true for invalid port 65536");

        // Test IsValidHttpMethod with various inputs
        if (!ValidationUtility.IsValidHttpMethod("GET"))
            problems.Add("IsValidHttpMethod failed for valid method 'GET'");
        if (!ValidationUtility.IsValidHttpMethod("POST"))
            problems.Add("IsValidHttpMethod failed for valid method 'POST'");
        if (!ValidationUtility.IsValidHttpMethod("get"))
            problems.Add("IsValidHttpMethod failed for valid method 'get' (case insensitive)");
        if (ValidationUtility.IsValidHttpMethod("INVALID"))
            problems.Add("IsValidHttpMethod returned true for invalid method 'INVALID'");
        if (ValidationUtility.IsValidHttpMethod(""))
            problems.Add("IsValidHttpMethod returned true for empty string");

        // Test IsValidHttpStatusCode with various inputs
        if (!ValidationUtility.IsValidHttpStatusCode(200))
            problems.Add("IsValidHttpStatusCode failed for valid status code 200");
        if (!ValidationUtility.IsValidHttpStatusCode(404))
            problems.Add("IsValidHttpStatusCode failed for valid status code 404");
        if (!ValidationUtility.IsValidHttpStatusCode(500))
            problems.Add("IsValidHttpStatusCode failed for valid status code 500");
        if (ValidationUtility.IsValidHttpStatusCode(99))
            problems.Add("IsValidHttpStatusCode returned true for invalid status code 99");
        if (ValidationUtility.IsValidHttpStatusCode(600))
            problems.Add("IsValidHttpStatusCode returned true for invalid status code 600");

        // Test IsNull with various inputs
        if (!ValidationUtility.IsNull(null))
            problems.Add("IsNull failed for null object");
        var testObj = new object();
        if (ValidationUtility.IsNull(testObj))
            problems.Add("IsNull returned true for non-null object");

        // Test IsValidType with various inputs
        if (!ValidationUtility.IsValidType<string>("test"))
            problems.Add("IsValidType failed for valid type 'string'");
        if (ValidationUtility.IsValidType<string>(123))
            problems.Add("IsValidType returned true for invalid type");

        // Test IsNullOrEmpty with collections
        if (!ValidationUtility.IsNullOrEmpty((IEnumerable<int>)null!))
            problems.Add("IsNullOrEmpty failed for null collection");
        if (!ValidationUtility.IsNullOrEmpty(new List<int>()))
            problems.Add("IsNullOrEmpty failed for empty collection");
        if (ValidationUtility.IsNullOrEmpty(new List<int> { 1, 2, 3 }))
            problems.Add("IsNullOrEmpty returned true for populated collection");

        // Test HasRequiredKeys with various inputs
        var dict = new Dictionary<string, string> { ["name"] = "John", ["email"] = "john@example.com" };
        if (!ValidationUtility.HasRequiredKeys<string, string>(dict, "name", "email"))
            problems.Add("HasRequiredKeys failed for dictionary with all required keys");
        if (ValidationUtility.HasRequiredKeys<string, string>(dict, "name", "phone"))
            problems.Add("HasRequiredKeys returned true for dictionary missing required key 'phone'");
        if (ValidationUtility.HasRequiredKeys<string, string>(null!, "name"))
            problems.Add("HasRequiredKeys returned true for null dictionary");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ValidationUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ValidationUtilityTests? value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="ValidationUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this ValidationUtilityTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ValidationUtilityTests instance is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}