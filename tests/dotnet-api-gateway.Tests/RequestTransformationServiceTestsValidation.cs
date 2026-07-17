#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation helpers for <see cref="RequestTransformationServiceTests"/> instances.
/// </summary>
public static class RequestTransformationServiceTestsValidation
{
    private static readonly IReadOnlyList<string> ExpectedMethodNames = new List<string>
    {
        nameof(RequestTransformationServiceTests.ApplyRequestRules_AddHeader_AppendsHeaderWhenMissing),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_AddHeader_DoesNotOverwriteExistingHeader),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_SetHeader_ReplacesExistingHeader),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_RemoveHeader_DeletesHeader),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_SetQueryParam_AppendsParamToUri),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_RemoveQueryParam_DropsParamFromUri),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_RewritePathPrefix_ReplacesMatchingPrefix),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_DisabledRule_IsSkipped),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_ResponsePhaseRule_IsNotAppliedToRequest),
        nameof(RequestTransformationServiceTests.ApplyResponseRules_SetHeader_InjectsHeaderIntoResponse),
        nameof(RequestTransformationServiceTests.ApplyResponseRules_RemoveHeader_DeletesHeaderFromResponse),
        nameof(RequestTransformationServiceTests.ApplyRequestRules_OrderedRules_AreAppliedInOrder),
        nameof(RequestTransformationServiceTests.TransformationRule_Validate_ThrowsWhenKeyEmpty),
        nameof(RequestTransformationServiceTests.TransformationRule_Validate_ThrowsWhenValueMissingForAdd),
        nameof(RequestTransformationServiceTests.TransformationRule_Validate_DoesNotThrowForRemove)
    };

    /// <summary>
    /// Validates that a <see cref="RequestTransformationServiceTests"/> instance is well-formed.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RequestTransformationServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate all public methods exist using reflection
        // This ensures the class has the expected public API surface
        var type = typeof(RequestTransformationServiceTests);

        foreach (var methodName in ExpectedMethodNames)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (method is null)
            {
                problems.Add($"Missing method: {methodName}");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="RequestTransformationServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this RequestTransformationServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="RequestTransformationServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this RequestTransformationServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"RequestTransformationServiceTests instance is not valid. Problems: {string.Join(", ", problems)}",
            nameof(value));
    }
}