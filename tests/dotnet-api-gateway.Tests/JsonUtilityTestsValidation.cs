#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation helpers for <see cref="JsonUtilityTests"/> instances.
/// </summary>
public static class JsonUtilityTestsValidation
{
    /// <summary>
    /// Validates a <see cref="JsonUtilityTests"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A read-only list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this JsonUtilityTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Name - must not be null or whitespace
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Name is null or whitespace.");
        }

        // Validate Age (should be positive)
        if (value.Age <= 0)
        {
            problems.Add("Age must be a positive integer.");
        }

        // Validate OptionalField - if not null, must not be whitespace-only
        if (value.OptionalField is not null && string.IsNullOrWhiteSpace(value.OptionalField))
        {
            problems.Add("OptionalField contains only whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="JsonUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this JsonUtilityTests value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="JsonUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value"/> is not valid, containing a list of problems.
    /// The exception message includes all validation problems.
    /// </exception>
    public static void EnsureValid(this JsonUtilityTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JsonUtilityTests instance is not valid. Problems: {string.Join(" ", problems)}");
        }
    }
}