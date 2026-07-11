#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace dotnet_api_gateway.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extension methods for <see cref="AdminDashboardSummaryTests"/>.
/// </summary>
public static class AdminDashboardSummaryTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="AdminDashboardSummaryTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="AdminDashboardSummaryTests"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the <see cref="AdminDashboardSummaryTests"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToJson(this AdminDashboardSummaryTests? value, bool indented = false)
        => value is null
            ? "null"
            : JsonSerializer.Serialize(
                value,
                indented
                    ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                    : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to an <see cref="AdminDashboardSummaryTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="AdminDashboardSummaryTests"/> instance, or <see langword="null"/> if the JSON is <see langword="null"/> or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
    public static AdminDashboardSummaryTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json) || json == "null"
            ? null
            : JsonSerializer.Deserialize<AdminDashboardSummaryTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="AdminDashboardSummaryTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="AdminDashboardSummaryTests"/> instance
    /// if deserialization succeeds; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
    public static bool TryFromJson(string json, out AdminDashboardSummaryTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<AdminDashboardSummaryTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}