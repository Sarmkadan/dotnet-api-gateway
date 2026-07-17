#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetApiGateway.Models;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="AggregatedResponse"/>
/// </summary>
public static class AggregatedResponseJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the JSON serialization options used by these extension methods
    /// </summary>
    public static JsonSerializerOptions JsonOptions => _jsonOptions;

    /// <summary>
    /// Serializes an AggregatedResponse instance to a JSON string
    /// </summary>
    /// <param name="value">The AggregatedResponse instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the AggregatedResponse</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this AggregatedResponse value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an AggregatedResponse from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized AggregatedResponse instance, or null if JSON is invalid</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized to AggregatedResponse</exception>
    public static AggregatedResponse? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<AggregatedResponse>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize JSON to AggregatedResponse", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize an AggregatedResponse from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized AggregatedResponse</param>
    /// <returns>True if deserialization succeeded; otherwise, false</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty</exception>
    public static bool TryFromJson(string json, out AggregatedResponse? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<AggregatedResponse>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}