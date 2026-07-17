#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetApiGateway.Models;

/// <summary>
/// Provides validation helpers for <see cref="AggregationPolicy"/> instances.
/// </summary>
public static class AggregationPolicyValidation
{
    /// <summary>
    /// Validates an <see cref="AggregationPolicy"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The aggregation policy to validate.</param>
    /// <returns>A read-only list of human-readable validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AggregationPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            errors.Add("Id cannot be null or whitespace.");
        }
        else if (value.Id.Length > 100)
        {
            errors.Add("Id cannot exceed 100 characters.");
        }

        // Validate Enabled - boolean values are always valid
        // Note: Logic around Enabled with empty targets is validated below

        // Validate Strategy
        if (!Enum.IsDefined(typeof(AggregationStrategy), value.Strategy))
        {
            errors.Add("Strategy must be a valid AggregationStrategy value.");
        }

        // Validate Targets
        if (value.Targets is null)
        {
            errors.Add("Targets collection cannot be null.");
        }
        else
        {
            if (value.Targets.Length == 0 && value.Enabled)
            {
                errors.Add("Targets must contain at least one target when Enabled is true.");
            }

            for (var i = 0; i < value.Targets.Length; i++)
            {
                var target = value.Targets[i];
                if (target is null)
                {
                    errors.Add($"Targets[{i}] cannot be null.");
                    continue;
                }

                try
                {
                    target.Validate();
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"Targets[{i}]: {ex.Message}");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="AggregationPolicy"/> instance is valid.
    /// </summary>
    /// <param name="value">The aggregation policy to check.</param>
    /// <returns>True if the policy is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this AggregationPolicy? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="AggregationPolicy"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed error message if it is not.
    /// </summary>
    /// <param name="value">The aggregation policy to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this AggregationPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"AggregationPolicy is invalid. Details:\n{string.Join("\n", errors)}",
                nameof(value));
        }
    }
}