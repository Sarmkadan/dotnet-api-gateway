#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Formatters;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides extension methods for serializing and deserializing response types to/from JSON.
/// These extension methods complement the functionality provided by <see cref="JsonResponseFormatter"/>.
/// </summary>
public static class JsonResponseFormatterJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static JsonSerializerOptions GetOptions(bool indented)
    {
        var options = new JsonSerializerOptions(Options)
        {
            WriteIndented = indented
        };
        return options;
    }

    /// <summary>
    /// Serializes a <see cref="SuccessResponse{T}"/> instance to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="value">The response instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson<T>(this SuccessResponse<T> value, bool indented = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, GetOptions(indented));
    }

    /// <summary>
    /// Serializes an <see cref="ErrorResponse"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The error response instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the error response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ErrorResponse value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, GetOptions(indented));
    }

    /// <summary>
    /// Serializes a <see cref="PaginatedResponse{T}"/> instance to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of data items in the response.</typeparam>
    /// <param name="value">The paginated response instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the paginated response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson<T>(this PaginatedResponse<T> value, bool indented = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, GetOptions(indented));
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="SuccessResponse{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized response instance, or null if the JSON is invalid.</returns>
    public static SuccessResponse<T>? FromJson<T>(string json) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<SuccessResponse<T>>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="ErrorResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized error response instance, or null if the JSON is invalid.</returns>
    public static ErrorResponse? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }


    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="SuccessResponse{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized response instance, or null if deserialization failed.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson<T>(string json, out SuccessResponse<T>? value) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<SuccessResponse<T>>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="ErrorResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized error response instance, or null if deserialization failed.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ErrorResponse? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ErrorResponse>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="PaginatedResponse{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of data items in the response.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized paginated response instance, or null if deserialization failed.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson<T>(string json, out PaginatedResponse<T>? value) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<PaginatedResponse<T>>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}