#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation helpers for <see cref="DateTimeUtilityTests"/> instances.
/// </summary>
public static class DateTimeUtilityTestsValidation
{
    /// <summary>
    /// Validates a <see cref="DateTimeUtilityTests"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DateTimeUtilityTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate that the DateTimeUtility class methods work correctly
        // These validations check the actual behavior of DateTimeUtility methods
        // rather than the test class itself

        // Validate ToUnixTimestamp works correctly with valid dates
        var testDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var unixTimestamp = DateTimeUtility.ToUnixTimestamp(testDate);
        if (unixTimestamp != 1672531200)
        {
            errors.Add("DateTimeUtility.ToUnixTimestamp produces incorrect results");
        }

        // Validate FromUnixTimestamp works correctly
        var parsedDate = DateTimeUtility.FromUnixTimestamp(1672531200L);
        if (parsedDate != new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        {
            errors.Add("DateTimeUtility.FromUnixTimestamp produces incorrect results");
        }

        // Validate FormatDateTime produces expected format
        var formattedDate = DateTimeUtility.FormatDateTime(testDate);
        if (formattedDate != "2023-01-01 00:00:00")
        {
            errors.Add("DateTimeUtility.FormatDateTime produces incorrect results");
        }

        // Validate GetRelativeTime produces expected output for recent dates
        var relativeTime = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddSeconds(-10));
        if (relativeTime != "just now")
        {
            errors.Add("DateTimeUtility.GetRelativeTime produces incorrect results");
        }

        // Validate IsPast correctly identifies past dates
        var pastDate = DateTime.UtcNow.AddMinutes(-5);
        if (!DateTimeUtility.IsPast(pastDate))
        {
            errors.Add("DateTimeUtility.IsPast produces incorrect results");
        }

        // Validate IsFuture correctly identifies future dates
        var futureDate = DateTime.UtcNow.AddMinutes(5);
        if (!DateTimeUtility.IsFuture(futureDate))
        {
            errors.Add("DateTimeUtility.IsFuture produces incorrect results");
        }

        // Validate IsSameDay correctly identifies same-day dates
        var date1 = new DateTime(2023, 1, 1, 10, 0, 0);
        var date2 = new DateTime(2023, 1, 1, 20, 0, 0);
        if (!DateTimeUtility.IsSameDay(date1, date2))
        {
            errors.Add("DateTimeUtility.IsSameDay produces incorrect results");
        }

        // Validate GetBusinessDaysBetween correctly calculates business days
        var start = new DateTime(2023, 1, 2); // Monday
        var end = new DateTime(2023, 1, 6); // Friday
        var businessDays = DateTimeUtility.GetBusinessDaysBetween(start, end);
        if (businessDays != 5)
        {
            errors.Add("DateTimeUtility.GetBusinessDaysBetween produces incorrect results");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DateTimeUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DateTimeUtilityTests? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DateTimeUtilityTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing the validation errors.</exception>
    public static void EnsureValid(this DateTimeUtilityTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DateTimeUtilityTests instance is not valid. Validation errors: {string.Join("; ", errors)}");
        }
    }
}
