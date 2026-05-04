// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Globalization;

/// <summary>
/// Utility class for type conversion and casting operations.
/// Provides safe conversion methods with default values and exception handling.
/// </summary>
public static class ConversionUtility
{
    /// <summary>
    /// Safely convert object to string, returning default if conversion fails.
    /// </summary>
    public static string ToString(object? obj, string defaultValue = "")
    {
        if (obj == null)
            return defaultValue;

        try
        {
            return obj.ToString() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely convert string to integer, returning default if conversion fails.
    /// </summary>
    public static int ToInt(string value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely convert string to long integer, returning default if conversion fails.
    /// </summary>
    public static long ToLong(string value, long defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely convert string to decimal, returning default if conversion fails.
    /// </summary>
    public static decimal ToDecimal(string value, decimal defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely convert string to double, returning default if conversion fails.
    /// </summary>
    public static double ToDouble(string value, double defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely convert string to boolean, returning default if conversion fails.
    /// Accepts "true", "yes", "1", "on" as true values (case-insensitive).
    /// </summary>
    public static bool ToBoolean(string value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (bool.TryParse(value, out var result))
            return result;

        // Accept common true representations
        return value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase) ? true : defaultValue;
    }

    /// <summary>
    /// Safely convert string to DateTime, returning default if conversion fails.
    /// Tries multiple formats for flexible parsing.
    /// </summary>
    public static DateTime ToDateTime(string value, DateTime? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue ?? DateTime.MinValue;

        var formats = new[] { "O", "R", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
                return result;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return parsed;

        return defaultValue ?? DateTime.MinValue;
    }

    /// <summary>
    /// Safely convert string to Guid, returning default if conversion fails.
    /// </summary>
    public static Guid ToGuid(string value, Guid? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue ?? Guid.Empty;

        return Guid.TryParse(value, out var result) ? result : (defaultValue ?? Guid.Empty);
    }

    /// <summary>
    /// Convert byte array to base64 string.
    /// </summary>
    public static string ToBase64(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Convert base64 string to byte array, returning null if conversion fails.
    /// </summary>
    public static byte[]? FromBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Convert.FromBase64String(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert object to another type using reflection and conversion methods.
    /// Returns null if conversion is not possible.
    /// </summary>
    public static T? ConvertTo<T>(object? value) where T : class
    {
        if (value == null)
            return null;

        if (value is T typedValue)
            return typedValue;

        try
        {
            return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
