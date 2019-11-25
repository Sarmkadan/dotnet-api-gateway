#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Validation helpers for JsonUtility to ensure data integrity before serialization/deserialization operations.
/// Provides validation methods for all JsonUtility method parameters.
/// </summary>
public static class JsonUtilityValidation
{
    /// <summary>
    /// Validates serialization input parameters.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> Validate<T>(T obj) where T : class
    {
        var problems = new List<string>();

        if (obj is null)
        {
            problems.Add("Serialization input object is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates serialization input parameters with pretty formatting.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidatePretty<T>(T obj) where T : class
    {
        return Validate(obj);
    }

    /// <summary>
    /// Validates deserialization input JSON string.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidateDeserialize(string json)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(json))
        {
            problems.Add("JSON input string is null, empty, or whitespace");
        }
        else if (json.Length > 10_000_000) // 10MB limit
        {
            problems.Add("JSON input string exceeds maximum size limit of 10MB");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates safe deserialization input JSON string.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidateDeserializeSafe(string json)
    {
        return ValidateDeserialize(json);
    }

    /// <summary>
    /// Validates dynamic JSON parsing input.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidateParseDynamic(string json)
    {
        return ValidateDeserialize(json);
    }

    /// <summary>
    /// Validates JSON string for validity check.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidateIsValidJson(string json)
    {
        return ValidateDeserialize(json);
    }

    /// <summary>
    /// Validates JSON merge operation inputs.
    /// Returns a list of human-readable problem descriptions.
    /// </summary>
    public static IReadOnlyList<string> ValidateMergeJson(string json1, string json2)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(json1))
        {
            problems.Add("First JSON string for merge is null, empty, or whitespace");
        }
        else if (json1.Length > 10_000_000) // 10MB limit
        {
            problems.Add("First JSON string for merge exceeds maximum size limit of 10MB");
        }

        if (string.IsNullOrWhiteSpace(json2))
        {
            problems.Add("Second JSON string for merge is null, empty, or whitespace");
        }
        else if (json2.Length > 10_000_000) // 10MB limit
        {
            problems.Add("Second JSON string for merge exceeds maximum size limit of 10MB");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if serialization input is valid.
    /// </summary>
    public static bool IsValid<T>(T obj) where T : class
    {
        return Validate(obj).Count == 0;
    }

    /// <summary>
    /// Checks if serialization input with pretty formatting is valid.
    /// </summary>
    public static bool IsValidPretty<T>(T obj) where T : class
    {
        return IsValid(obj);
    }

    /// <summary>
    /// Checks if deserialization input JSON is valid.
    /// </summary>
    public static bool IsValidDeserialize(string json)
    {
        return ValidateDeserialize(json).Count == 0;
    }

    /// <summary>
    /// Checks if safe deserialization input JSON is valid.
    /// </summary>
    public static bool IsValidDeserializeSafe(string json)
    {
        return IsValidDeserialize(json);
    }

    /// <summary>
    /// Checks if dynamic JSON parsing input is valid.
    /// </summary>
    public static bool IsValidParseDynamic(string json)
    {
        return IsValidDeserialize(json);
    }

    /// <summary>
    /// Checks if JSON validity check input is valid.
    /// </summary>
    public static bool IsValidIsValidJson(string json)
    {
        return IsValidDeserialize(json);
    }

    /// <summary>
    /// Checks if JSON merge operation inputs are valid.
    /// </summary>
    public static bool IsValidMergeJson(string json1, string json2)
    {
        return ValidateMergeJson(json1, json2).Count == 0;
    }

    /// <summary>
    /// Ensures that serialization input is valid, throwing ArgumentException with detailed problems if not.
    /// </summary>
    public static void EnsureValid<T>(T obj) where T : class
    {
        var problems = Validate(obj);

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JsonUtility serialization input validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that serialization input with pretty formatting is valid.
    /// </summary>
    public static void EnsureValidPretty<T>(T obj) where T : class
    {
        EnsureValid(obj);
    }

    /// <summary>
    /// Ensures that deserialization input JSON is valid.
    /// </summary>
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
    /// Ensures that safe deserialization input JSON is valid.
    /// </summary>
    public static void EnsureValidDeserializeSafe(string json)
    {
        EnsureValidDeserialize(json);
    }

    /// <summary>
    /// Ensures that dynamic JSON parsing input is valid.
    /// </summary>
    public static void EnsureValidParseDynamic(string json)
    {
        EnsureValidDeserialize(json);
    }

    /// <summary>
    /// Ensures that JSON validity check input is valid.
    /// </summary>
    public static void EnsureValidIsValidJson(string json)
    {
        EnsureValidDeserialize(json);
    }

    /// <summary>
    /// Ensures that JSON merge operation inputs are valid.
    /// </summary>
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
