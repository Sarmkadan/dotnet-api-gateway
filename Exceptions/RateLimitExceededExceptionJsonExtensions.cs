#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization extensions for RateLimitExceededException
/// </summary>
public static class RateLimitExceededExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a RateLimitExceededException to a JSON string
    /// </summary>
    /// <param name="value">The exception to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this RateLimitExceededException value, bool indented = false)
    {
        if (value is null)
        {
            return "{}";
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a RateLimitExceededException
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>The deserialized exception, or null if JSON is invalid</returns>
    public static RateLimitExceededException? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<RateLimitExceededException>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a RateLimitExceededException
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized exception</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public static bool TryFromJson(string json, out RateLimitExceededException? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<RateLimitExceededException>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}