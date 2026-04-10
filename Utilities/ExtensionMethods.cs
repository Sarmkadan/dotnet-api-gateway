#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Text;

/// <summary>
/// Extension methods for common types used throughout the gateway.
/// Provides fluent API for string manipulation, collections, and object operations.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Check if string is null, empty, or whitespace.
    /// </summary>
    public static bool IsEmpty(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Check if string has content (not null, not empty, not whitespace).
    /// </summary>
    public static bool HasContent(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Truncate string to maximum length with optional suffix.
    /// Useful for logging and display purposes.
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value is null || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Remove all occurrences of specified characters from string.
    /// </summary>
    public static string Remove(this string value, params char[] chars)
    {
        if (value is null || chars is null || chars.Length == 0)
            return value;

        var sb = new StringBuilder(value.Length);
        var charSet = new HashSet<char>(chars);

        foreach (var c in value)
        {
            if (!charSet.Contains(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Repeat string multiple times.
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        if (string.IsNullOrEmpty(value) || count <= 0)
            return string.Empty;

        var sb = new StringBuilder(value.Length * count);
        for (int i = 0; i < count; i++)
        {
            sb.Append(value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert string to byte array using UTF-8 encoding.
    /// </summary>
    public static byte[] ToBytes(this string value, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(value))
            return []byte>();

        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(value);
    }

    /// <summary>
    /// Get bytes from byte array as hex string.
    /// </summary>
    public static string ToHexString(this byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;

        return Convert.ToHexString(data);
    }

    /// <summary>
    /// Check if collection is null or contains no elements.
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection is null || !collection.Any();
    }

    /// <summary>
    /// Check if collection has elements.
    /// </summary>
    public static bool HasElements<T>(this IEnumerable<T>? collection)
    {
        return collection is not null && collection.Any();
    }

    /// <summary>
    /// Safely get element at index, returning default if out of bounds.
    /// </summary>
    public static T? GetOrDefault<T>(this List<T> list, int index, T? defaultValue = default)
    {
        if (list is null || index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// Convert dictionary keys to lowercase for case-insensitive lookup.
    /// </summary>
    public static Dictionary<string, T> ToLowerKeyDictionary<T>(this Dictionary<string, T> dict)
    {
        if (dict is null)
            return new Dictionary<string, T>();

        return dict.ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
    }

    /// <summary>
    /// Merge two dictionaries, with source overwriting destination.
    /// </summary>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue> destination,
        Dictionary<TKey, TValue> source) where TKey : notnull
    {
        if (destination is null)
            return source ?? new Dictionary<TKey, TValue>();

        if (source is null)
            return destination;

        foreach (var kvp in source)
        {
            destination[kvp.Key] = kvp.Value;
        }

        return destination;
    }

    /// <summary>
    /// Format milliseconds as human-readable time string.
    /// </summary>
    public static string FormatMilliseconds(this long milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds}ms";

        var seconds = milliseconds / 1000.0;
        if (seconds < 60)
            return $"{seconds:F2}s";

        var minutes = seconds / 60;
        return $"{minutes:F2}m";
    }

    /// <summary>
    /// Check if string matches any pattern in list (case-insensitive).
    /// </summary>
    public static bool MatchesAny(this string value, params string[] patterns)
    {
        if (string.IsNullOrEmpty(value) || patterns is null || patterns.Length == 0)
            return false;

        return patterns.Any(p => value.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get approximate memory size of object in bytes (rough estimate).
    /// </summary>
    public static long GetMemorySize(this string value)
    {
        if (value is null)
            return 0;

        return System.Text.Encoding.UTF8.GetByteCount(value) + 48; // Account for string object overhead
    }
}
