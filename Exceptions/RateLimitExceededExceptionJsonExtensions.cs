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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static string ToJson(this RateLimitExceededException value, bool indented = false)
        => value is null
            ? throw new ArgumentNullException(nameof(value))
            : JsonSerializer.Serialize(value, GetOptions(indented));

    /// <summary>
    /// Deserializes a JSON string to a RateLimitExceededException
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>The deserialized exception, or null if JSON is invalid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null</exception>
    public static RateLimitExceededException? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null</exception>
    public static bool TryFromJson(string json, out RateLimitExceededException? value)
    {
        ArgumentNullException.ThrowIfNull(json);

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

    private static JsonSerializerOptions GetOptions(bool indented)
        => indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;
}