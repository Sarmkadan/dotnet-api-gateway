#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Text.Json;
using System.Text.Json.Nodes;
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
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
    /// Returns null for null, empty, whitespace, or invalid input.
    /// </summary>
    public static JsonElement? ParseDynamic(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

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
    /// Merge two JSON documents, with values from the second overwriting the first.
    /// Objects are merged recursively; scalars and arrays are replaced.
    /// Returns the first document unchanged if either input is not valid JSON.
    /// Useful for configuration or patch operations.
    /// </summary>
    public static string MergeJson(string json1, string json2)
    {
        JsonNode? node1;
        JsonNode? node2;

        try
        {
            node1 = JsonNode.Parse(json1);
            node2 = JsonNode.Parse(json2);
        }
        catch (JsonException)
        {
            return json1;
        }
        catch (ArgumentNullException)
        {
            return json1;
        }

        if (node1 is not JsonObject target || node2 is not JsonObject source)
        {
            // Non-object documents cannot be merged property-wise; second wins.
            return json2;
        }

        MergeInto(target, source);
        return target.ToJsonString();
    }

    private static void MergeInto(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            if (property.Value is JsonObject sourceChild && target[property.Key] is JsonObject targetChild)
            {
                MergeInto(targetChild, sourceChild);
            }
            else
            {
                target[property.Key] = property.Value?.DeepClone();
            }
        }
    }
}
