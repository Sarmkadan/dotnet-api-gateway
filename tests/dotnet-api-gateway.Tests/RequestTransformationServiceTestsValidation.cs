#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Reflection;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation helpers for <see cref="RequestTransformationServiceTests"/> instances.
/// </summary>
public static class RequestTransformationServiceTestsValidation
{
    private static readonly string[] ExpectedMethodNames =
    [
        "ApplyRequestRules_AddHeader_AppendsHeaderWhenMissing",
        "ApplyRequestRules_AddHeader_DoesNotOverwriteExistingHeader",
        "ApplyRequestRules_SetHeader_ReplacesExistingHeader",
        "ApplyRequestRules_RemoveHeader_DeletesHeader",
        "ApplyRequestRules_SetQueryParam_AppendsParamToUri",
        "ApplyRequestRules_RemoveQueryParam_DropsParamFromUri",
        "ApplyRequestRules_RewritePathPrefix_ReplacesMatchingPrefix",
        "ApplyRequestRules_DisabledRule_IsSkipped",
        "ApplyRequestRules_ResponsePhaseRule_IsNotAppliedToRequest",
        "ApplyResponseRules_SetHeader_InjectsHeaderIntoResponse",
        "ApplyResponseRules_RemoveHeader_DeletesHeaderFromResponse",
        "ApplyRequestRules_OrderedRules_AreAppliedInOrder",
        "TransformationRule_Validate_ThrowsWhenKeyEmpty",
        "TransformationRule_Validate_ThrowsWhenValueMissingForAdd",
        "TransformationRule_Validate_DoesNotThrowForRemove"
    ];

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
            if (method == null)
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
    public static bool IsValid(this RequestTransformationServiceTests? value)
    {
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