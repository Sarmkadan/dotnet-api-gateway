#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Text.RegularExpressions;

/// <summary>
/// Utility class for common validation operations.
/// Provides validators for email, URL, phone numbers, and custom patterns.
/// </summary>
public static class ValidationUtility
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"^https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IpAddressRegex = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);
    private static readonly Regex UuidRegex = new(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validate email address format.
    /// Uses simple regex validation; for strict RFC compliance, use System.Net.Mail.MailAddress.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Validate URL format starting with http or https.
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!UrlRegex.IsMatch(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Validate IPv4 address format (basic check).
    /// Does not validate if octets are within 0-255 range.
    /// </summary>
    public static bool IsValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return IpAddressRegex.IsMatch(ipAddress);
    }

    /// <summary>
    /// Validate UUID/GUID format.
    /// Accepts both hyphenated and non-hyphenated formats.
    /// </summary>
    public static bool IsValidUuid(string uuid)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            return false;

        return Guid.TryParse(uuid, out _) || UuidRegex.IsMatch(uuid);
    }

    /// <summary>
    /// Check if string is empty or consists only of whitespace.
    /// </summary>
    public static bool IsNullOrEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Validate string length is within expected bounds.
    /// </summary>
    public static bool IsValidLength(string value, int minLength, int maxLength)
    {
        if (value is null)
            return minLength == 0;

        return value.Length >= minLength && value.Length <= maxLength;
    }

    /// <summary>
    /// Check if string contains only alphanumeric characters.
    /// </summary>
    public static bool IsAlphanumeric(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Check if string contains only ASCII characters.
    /// </summary>
    public static bool IsAsciiOnly(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(c => c < 128);
    }

    /// <summary>
    /// Validate port number is within valid range (1-65535).
    /// </summary>
    public static bool IsValidPort(int port)
    {
        return port > 0 && port <= 65535;
    }

    /// <summary>
    /// Validate HTTP method is in allowed list.
    /// </summary>
    public static bool IsValidHttpMethod(string method)
    {
        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE" };
        return !string.IsNullOrWhiteSpace(method) && validMethods.Contains(method.ToUpperInvariant());
    }

    /// <summary>
    /// Validate HTTP status code is in valid range (100-599).
    /// </summary>
    public static bool IsValidHttpStatusCode(int statusCode)
    {
        return statusCode >= 100 && statusCode <= 599;
    }

    /// <summary>
    /// Check if object is null.
    /// </summary>
    public static bool IsNull(object obj)
    {
        return obj is null;
    }

    /// <summary>
    /// Validate that object is not null and is of expected type.
    /// </summary>
    public static bool IsValidType<T>(object obj)
    {
        return obj is not null && obj is T;
    }

    /// <summary>
    /// Check if collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(IEnumerable<T> collection)
    {
        return collection is null || !collection.Any();
    }

    /// <summary>
    /// Validate that dictionary contains required keys.
    /// </summary>
    public static bool HasRequiredKeys<TKey, TValue>(Dictionary<TKey, TValue?> dict, params TKey[] requiredKeys) where TKey : notnull
    {
        if (dict is null)
            return false;

        return requiredKeys.All(k => dict.ContainsKey(k));
    }
}
