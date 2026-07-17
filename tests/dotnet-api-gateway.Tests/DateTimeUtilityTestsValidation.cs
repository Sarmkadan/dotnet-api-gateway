#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides validation helpers for <see cref="DateTimeUtility"/> class behavior.
/// This class validates that DateTimeUtility methods work correctly with various inputs.
/// </summary>
public static class DateTimeUtilityTestsValidation
{
    /// <summary>
    /// Validates that DateTimeUtility methods produce correct results.
    /// </summary>
    /// <returns>An empty list if all validations pass; otherwise, a list of validation messages.</returns>
    public static IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        // Validate ToUnixTimestamp and FromUnixTimestamp are reciprocal operations
        var testDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var unixTimestamp = DateTimeUtility.ToUnixTimestamp(testDate);
        var parsedDate = DateTimeUtility.FromUnixTimestamp(unixTimestamp);

        if (parsedDate != testDate)
        {
            errors.Add("DateTimeUtility.ToUnixTimestamp and FromUnixTimestamp are not reciprocal operations");
        }

        // Validate FormatDateTime produces consistent output
        var formattedDate = DateTimeUtility.FormatDateTime(testDate);
        if (string.IsNullOrWhiteSpace(formattedDate) || formattedDate.Length != 19)
        {
            errors.Add("DateTimeUtility.FormatDateTime produces invalid formatted string");
        }

        // Validate GetRelativeTime produces non-empty output for recent dates
        var relativeTime = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddSeconds(-10));
        if (string.IsNullOrWhiteSpace(relativeTime))
        {
            errors.Add("DateTimeUtility.GetRelativeTime produces empty result");
        }

        // Validate IsPast correctly identifies past dates
        var pastDate = DateTime.UtcNow.AddMinutes(-5);
        if (!DateTimeUtility.IsPast(pastDate))
        {
            errors.Add("DateTimeUtility.IsPast incorrectly identifies past date as future");
        }

        // Validate IsFuture correctly identifies future dates
        var futureDate = DateTime.UtcNow.AddMinutes(5);
        if (!DateTimeUtility.IsFuture(futureDate))
        {
            errors.Add("DateTimeUtility.IsFuture incorrectly identifies future date as past");
        }

        // Validate IsSameDay correctly identifies same-day dates
        var date1 = new DateTime(2023, 1, 1, 10, 0, 0);
        var date2 = new DateTime(2023, 1, 1, 20, 0, 0);
        if (!DateTimeUtility.IsSameDay(date1, date2))
        {
            errors.Add("DateTimeUtility.IsSameDay incorrectly identifies same-day dates");
        }

        // Validate IsSameDay correctly identifies different-day dates
        var date3 = new DateTime(2023, 1, 1, 10, 0, 0);
        var date4 = new DateTime(2023, 1, 2, 10, 0, 0);
        if (DateTimeUtility.IsSameDay(date3, date4))
        {
            errors.Add("DateTimeUtility.IsSameDay incorrectly identifies different-day dates as same");
        }

        // Validate GetBusinessDaysBetween correctly calculates business days
        var monday = new DateTime(2023, 1, 2); // Monday
        var friday = new DateTime(2023, 1, 6); // Friday
        var businessDays = DateTimeUtility.GetBusinessDaysBetween(monday, friday);
        if (businessDays != 5)
        {
            errors.Add("DateTimeUtility.GetBusinessDaysBetween produces incorrect business day count");
        }

        // Validate GetBusinessDaysBetween with weekend dates
        var saturday = new DateTime(2023, 1, 7); // Saturday
        var sunday = new DateTime(2023, 1, 8); // Sunday
        var weekendDays = DateTimeUtility.GetBusinessDaysBetween(saturday, sunday);
        if (weekendDays != 0)
        {
            errors.Add("DateTimeUtility.GetBusinessDaysBetween incorrectly counts weekend days");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether DateTimeUtility methods are working correctly.
    /// </summary>
    /// <returns><see langword="true"/> if all validations pass; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid()
    {
        return Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that DateTimeUtility methods are working correctly.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if any DateTimeUtility method produces incorrect results.</exception>
    public static void EnsureValid()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DateTimeUtility methods are not working correctly. Validation errors: {string.Join("; ", errors)}");
        }
    }
}
