#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Utility class for JSON serialization and deserialization operations.
/// Provides consistent JSON handling across the gateway with custom serialization policies.
/// </summary>
public static class JsonUtility
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serialize object to JSON string with standard formatting.
    /// </summary>
    public static string Serialize<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Serialize object to JSON string with pretty-print formatting for logging/debugging.
    /// </summary>
    public static string SerializePretty<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize(obj, PrettyOptions);
    }

    /// <summary>
    /// Deserialize JSON string to strongly-typed object.
    /// Throws JsonException if JSON is invalid.
    /// </summary>
    public static T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Safely deserialize JSON string with exception handling.
    /// Returns null if deserialization fails instead of throwing.
    /// </summary>
    public static T? DeserializeSafe<T>(string json) where T : class
    {
        try
        {
            return Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse JSON string to untyped JsonElement for dynamic access.
    /// Allows navigating unknown JSON structures.
    /// </summary>
    public static JsonElement? ParseDynamic(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json, DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Check if string is valid JSON without throwing exceptions.
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonSerializer.Deserialize<object>(json, DefaultOptions);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Merge two JSON objects, with second object overwriting first.
    /// Useful for configuration or patch operations.
    /// </summary>
    public static string MergeJson(string json1, string json2)
    {
        var obj1 = ParseDynamic(json1);
        var obj2 = ParseDynamic(json2);

        if (obj1 is null || obj2 is null)
            return json1;

        var doc1 = JsonDocument.Parse(json1);
        var doc2 = JsonDocument.Parse(json2);

        // For simplicity, return second if both are valid
        // In production, implement deep merge logic
        return json2;
    }
}
