namespace DotNetApiGateway.Tests;

public static class DateTimeUtilityTestsExtensions
{
    /// <summary>
    /// Determines if a DateTimeUtilityTests instance has a specific validation test method that returns true.
    /// </summary>
    /// <param name="tests">The DateTimeUtilityTests instance to check.</param>
    /// <param name="methodName">The name of the test method to check.</param>
    /// <returns>True if the test method exists and returns true; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static bool HasTestMethodReturningTrue(this DateTimeUtilityTests tests, string methodName)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var methodInfo = typeof(DateTimeUtilityTests).GetMethod(methodName);
        if (methodInfo == null)
            return false;

        var result = methodInfo.Invoke(tests, null) as bool?;
        return result ?? false;
    }

    /// <summary>
    /// Gets a collection of test method names that return true for a given DateTimeUtilityTests instance.
    /// </summary>
    /// <param name="tests">The DateTimeUtilityTests instance to check.</param>
    /// <returns>An IReadOnlyList of test method names that return true.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static IReadOnlyList<string> GetTestMethodsReturningTrue(this DateTimeUtilityTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return typeof(DateTimeUtilityTests)
            .GetMethods()
            .Where(m => m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            .Where(m => (bool?)m.Invoke(tests, null) ?? false)
            .Select(m => m.Name)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Checks if all test methods of a DateTimeUtilityTests instance return true.
    /// </summary>
    /// <param name="tests">The DateTimeUtilityTests instance to check.</param>
    /// <returns>True if all test methods return true; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static bool AllTestMethodsReturnTrue(this DateTimeUtilityTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return typeof(DateTimeUtilityTests)
            .GetMethods()
            .Where(m => m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            .All(m => (bool?)m.Invoke(tests, null) ?? false);
    }
}
