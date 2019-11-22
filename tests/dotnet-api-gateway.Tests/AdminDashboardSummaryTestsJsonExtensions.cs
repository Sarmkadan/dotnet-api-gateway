#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace dotnet_api_gateway.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extension methods for AdminDashboardSummaryTests.
/// </summary>
public static class AdminDashboardSummaryTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the AdminDashboardSummaryTests instance to a JSON string.
    /// </summary>
    /// <param name="value">The AdminDashboardSummaryTests instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the AdminDashboardSummaryTests instance.</returns>
    public static string ToJson(this AdminDashboardSummaryTests? value, bool indented = false)
    {
        if (value is null)
        {
            return "null";
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
    /// Deserializes a JSON string to an AdminDashboardSummaryTests instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized AdminDashboardSummaryTests instance, or null if the JSON is null.</returns>
    public static AdminDashboardSummaryTests? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }

        return JsonSerializer.Deserialize<AdminDashboardSummaryTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an AdminDashboardSummaryTests instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized AdminDashboardSummaryTests instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out AdminDashboardSummaryTests? value)
    {
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
