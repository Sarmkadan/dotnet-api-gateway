#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation extension methods for <see cref="GatewayRoute"/> instances
/// </summary>
public static class GatewayRouteTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="GatewayRoute"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The GatewayRoute instance to validate</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this GatewayRoute value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Name cannot be null or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.PathPattern))
        {
            problems.Add("PathPattern cannot be null or whitespace");
        }

        if (value.AllowedMethods is null || value.AllowedMethods.Length == 0)
        {
            problems.Add("AllowedMethods must contain at least one method");
        }
        else
        {
            for (var i = 0; i < value.AllowedMethods.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(value.AllowedMethods[i]))
                {
                    problems.Add($"AllowedMethods[{i}] cannot be null or whitespace");
                }
            }
        }

        if (value.Targets is null || value.Targets.Length == 0)
        {
            problems.Add("Targets must contain at least one target");
        }
        else
        {
            for (var i = 0; i < value.Targets.Length; i++)
            {
                if (value.Targets[i] is null)
                {
                    problems.Add($"Targets[{i}] cannot be null");
                }
                else
                {
                    try
                    {
                        value.Targets[i].Validate();
                    }
                    catch (Exception ex)
                    {
                        problems.Add($"Targets[{i}] validation failed: {ex.Message}");
                    }
                }
            }
        }

        if (value.TimeoutSeconds < 1 || value.TimeoutSeconds > 300)
        {
            problems.Add("TimeoutSeconds must be between 1 and 300 inclusive");
        }

        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt cannot be default(DateTime)");
        }

        if (value.ModifiedAt.HasValue && value.ModifiedAt.Value == default)
        {
            problems.Add("ModifiedAt cannot be default(DateTime) if set");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="GatewayRoute"/> instance is valid.
    /// </summary>
    /// <param name="value">The GatewayRoute instance to check</param>
    /// <returns>true if the instance is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this GatewayRoute value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="GatewayRoute"/> instance is valid.
    /// </summary>
    /// <param name="value">The GatewayRoute instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of problems</exception>
    public static void EnsureValid(this GatewayRoute value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"GatewayRoute is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}