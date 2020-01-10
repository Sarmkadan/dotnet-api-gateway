#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetApiGateway.Utilities;

using System;
using System.Collections.Generic;

/// <summary>
/// Validation helpers for JsonUtility to ensure data integrity before serialization/deserialization operations.
/// Provides validation methods for JSON input parameters.
/// </summary>
public static class JsonUtilityValidation
{
    private const int MaxJsonSizeBytes = 10_000_000; // 10MB limit

    /// <summary>
    /// Validates serialization input object.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    public static IReadOnlyList<string> Validate<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates deserialization input JSON string.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is empty or whitespace.</exception>
    public static IReadOnlyList<string> ValidateDeserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new[] { "JSON input string is null, empty, or whitespace" };
        }

        if (json.Length > MaxJsonSizeBytes)
        {
            return new[] { $"JSON input string exceeds maximum size limit of {MaxJsonSizeBytes:N0} bytes" };
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates JSON merge operation inputs.
    /// </summary>
    /// <param name="json1">The first JSON string to validate.</param>
    /// <param name="json2">The second JSON string to validate.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="json1"/> or <paramref name="json2"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if either <paramref name="json1"/> or <paramref name="json2"/> is empty or whitespace.</exception>
    public static IReadOnlyList<string> ValidateMergeJson(string json1, string json2)
    {
        ArgumentNullException.ThrowIfNull(json1);
        ArgumentNullException.ThrowIfNull(json2);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(json1))
        {
            problems.Add("First JSON string for merge is null, empty, or whitespace");
        }
        else if (json1.Length > MaxJsonSizeBytes)
        {
            problems.Add($"First JSON string for merge exceeds maximum size limit of {MaxJsonSizeBytes:N0} bytes");
        }

        if (string.IsNullOrWhiteSpace(json2))
        {
            problems.Add("Second JSON string for merge is null, empty, or whitespace");
        }
        else if (json2.Length > MaxJsonSizeBytes)
        {
            problems.Add($"Second JSON string for merge exceeds maximum size limit of {MaxJsonSizeBytes:N0} bytes");
        }

        return problems;
    }

    /// <summary>
    /// Checks if serialization input object is valid.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    public static bool IsValid<T>(T obj) => Validate(obj).Count == 0;

    /// <summary>
    /// Checks if deserialization input JSON is valid.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is empty or whitespace.</exception>
    public static bool IsValidDeserialize(string json) => ValidateDeserialize(json).Count == 0;

    /// <summary>
    /// Checks if JSON merge operation inputs are valid.
    /// </summary>
    /// <param name="json1">The first JSON string to validate.</param>
    /// <param name="json2">The second JSON string to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="json1"/> or <paramref name="json2"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if either <paramref name="json1"/> or <paramref name="json2"/> is empty or whitespace.</exception>
    public static bool IsValidMergeJson(string json1, string json2) => ValidateMergeJson(json1, json2).Count == 0;

    /// <summary>
    /// Ensures that serialization input object is valid, throwing <see cref="ArgumentException"/> with detailed problems if not.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    public static void EnsureValid<T>(T obj)
    {
        var problems = Validate(obj);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JsonUtility serialization input validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that deserialization input JSON is valid.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is empty, whitespace, or exceeds size limit.</exception>
    public static void EnsureValidDeserialize(string json)
    {
        var problems = ValidateDeserialize(json);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JsonUtility deserialization input validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that JSON merge operation inputs are valid.
    /// </summary>
    /// <param name="json1">The first JSON string to validate.</param>
    /// <param name="json2">The second JSON string to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="json1"/> or <paramref name="json2"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    public static void EnsureValidMergeJson(string json1, string json2)
    {
        var problems = ValidateMergeJson(json1, json2);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JsonUtility merge operation validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}