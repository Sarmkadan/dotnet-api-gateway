using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetApiGateway.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="RequestTransformationServiceTests"/>.
    /// </summary>
    public static class RequestTransformationServiceTestsExtensions
    {
        /// <summary>
        /// Executes all public <c>ApplyRequestRules_*</c> test methods on the supplied <paramref name="tests"/> instance.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static void RunAllApplyRequestRulesTests(this RequestTransformationServiceTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            // The order mirrors the logical grouping of the original test methods.
            tests.ApplyRequestRules_AddHeader_AppendsHeaderWhenMissing();
            tests.ApplyRequestRules_AddHeader_DoesNotOverwriteExistingHeader();
            tests.ApplyRequestRules_SetHeader_ReplacesExistingHeader();
            tests.ApplyRequestRules_RemoveHeader_DeletesHeader();
            tests.ApplyRequestRules_SetQueryParam_AppendsParamToUri();
            tests.ApplyRequestRules_RemoveQueryParam_DropsParamFromUri();
            tests.ApplyRequestRules_RewritePathPrefix_ReplacesMatchingPrefix();
            tests.ApplyRequestRules_DisabledRule_IsSkipped();
            tests.ApplyRequestRules_ResponsePhaseRule_IsNotAppliedToRequest();
            tests.ApplyResponseRules_SetHeader_InjectsHeaderIntoResponse();
            tests.ApplyResponseRules_RemoveHeader_DeletesHeaderFromResponse();
            tests.ApplyRequestRules_OrderedRules_AreAppliedInOrder();
        }

        /// <summary>
        /// Executes the three <c>TransformationRule_Validate_*</c> test methods and returns <c>true</c>
        /// if none of them throws an exception.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns><c>true</c> when all validation tests complete without throwing; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static bool VerifyTransformationRuleValidation(this RequestTransformationServiceTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            try
            {
                tests.TransformationRule_Validate_ThrowsWhenKeyEmpty();
                tests.TransformationRule_Validate_ThrowsWhenValueMissingForAdd();
                tests.TransformationRule_Validate_DoesNotThrowForRemove();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the names of all public test methods defined on <see cref="RequestTransformationServiceTests"/>.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> GetTestMethodNames(this RequestTransformationServiceTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return typeof(RequestTransformationServiceTests)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .Where(name => name.StartsWith("ApplyRequestRules_", StringComparison.Ordinal) ||
                              name.StartsWith("ApplyResponseRules_", StringComparison.Ordinal) ||
                              name.StartsWith("TransformationRule_Validate_", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
    }
}