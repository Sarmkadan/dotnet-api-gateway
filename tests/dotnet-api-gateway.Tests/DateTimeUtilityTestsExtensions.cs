namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides extension methods for <see cref="DateTimeUtilityTests"/> to inspect and validate test methods.
/// </summary>

public static class DateTimeUtilityTestsExtensions
{
    /// <summary>
    /// Determines whether the specified test method on a <see cref="DateTimeUtilityTests"/> instance returns true.
    /// </summary>
    /// <param name="tests">The <see cref="DateTimeUtilityTests"/> instance to check.</param>
    /// <param name="methodName">Name of the test method to invoke.</param>
    /// <returns>True if the test method exists and returns true; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="methodName"/> is null.</exception>
    public static bool HasTestMethodReturningTrue(this DateTimeUtilityTests tests, string methodName)
    {
        ArgumentNullException.ThrowIfNull(tests);

        ArgumentNullException.ThrowIfNull(methodName);

        var methodInfo = typeof(DateTimeUtilityTests).GetMethod(methodName);
        return methodInfo != null && (bool?)methodInfo.Invoke(tests, null) is true;
    }

    /// <summary>
    /// Gets the names of all test methods on a <see cref="DateTimeUtilityTests"/> instance that return true.
    /// </summary>
    /// <param name="tests">The <see cref="DateTimeUtilityTests"/> instance to check.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of test method names that return true.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static IReadOnlyList<string> GetTestMethodsReturningTrue(this DateTimeUtilityTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return typeof(DateTimeUtilityTests)
            .GetMethods()
            .Where(static m => m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            .Where(static m => (bool?)m.Invoke(null, null) ?? false)
            .Select(static m => m.Name)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Determines whether all parameterless boolean-returning test methods on a <see cref="DateTimeUtilityTests"/> instance return true.
    /// </summary>
    /// <param name="tests">The <see cref="DateTimeUtilityTests"/> instance to check.</param>
    /// <returns>True if all test methods return true; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static bool AllTestMethodsReturnTrue(this DateTimeUtilityTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return typeof(DateTimeUtilityTests)
            .GetMethods()
            .Where(m => m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            .All(static m => (bool?)m.Invoke(null, null) ?? false);
    }
}
