// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

/// <summary>
/// Utility class for date and time operations.
/// Provides helpers for time calculations, formatting, and timezone handling.
/// </summary>
public static class DateTimeUtility
{
    /// <summary>
    /// Get current UTC time in ISO 8601 format.
    /// </summary>
    public static string GetCurrentUtcIso8601()
    {
        return DateTime.UtcNow.ToString("O");
    }

    /// <summary>
    /// Get Unix timestamp for current time.
    /// </summary>
    public static long GetCurrentUnixTimestamp()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Convert DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Convert Unix timestamp to DateTime.
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
    }

    /// <summary>
    /// Format DateTime as human-readable string.
    /// </summary>
    public static string FormatDateTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return dateTime.ToString(format);
    }

    /// <summary>
    /// Get relative time string (e.g., "5 minutes ago", "2 hours from now").
    /// </summary>
    public static string GetRelativeTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalSeconds < 60)
            return "just now";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes > 1 ? "s" : "")} ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours > 1 ? "s" : "")} ago";

        if (diff.TotalDays < 30)
            return $"{(int)diff.TotalDays} day{((int)diff.TotalDays > 1 ? "s" : "")} ago";

        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)} month{((int)(diff.TotalDays / 30) > 1 ? "s" : "")} ago";

        return $"{(int)(diff.TotalDays / 365)} year{((int)(diff.TotalDays / 365) > 1 ? "s" : "")} ago";
    }

    /// <summary>
    /// Check if datetime is in the past.
    /// </summary>
    public static bool IsPast(DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Check if datetime is in the future.
    /// </summary>
    public static bool IsFuture(DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Get start of day (midnight).
    /// </summary>
    public static DateTime GetStartOfDay(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return dt.Date;
    }

    /// <summary>
    /// Get end of day (23:59:59).
    /// </summary>
    public static DateTime GetEndOfDay(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return dt.Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Get start of week (Monday).
    /// </summary>
    public static DateTime GetStartOfWeek(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        var diff = (int)dt.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0)
            diff += 7;

        return dt.AddDays(-diff).Date;
    }

    /// <summary>
    /// Get end of week (Sunday).
    /// </summary>
    public static DateTime GetEndOfWeek(DateTime? dateTime = null)
    {
        var startOfWeek = GetStartOfWeek(dateTime);
        return startOfWeek.AddDays(7).AddSeconds(-1);
    }

    /// <summary>
    /// Get start of month.
    /// </summary>
    public static DateTime GetStartOfMonth(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return new DateTime(dt.Year, dt.Month, 1);
    }

    /// <summary>
    /// Get end of month.
    /// </summary>
    public static DateTime GetEndOfMonth(DateTime? dateTime = null)
    {
        var startOfMonth = GetStartOfMonth(dateTime);
        return startOfMonth.AddMonths(1).AddSeconds(-1);
    }

    /// <summary>
    /// Check if two datetimes are on the same day.
    /// </summary>
    public static bool IsSameDay(DateTime date1, DateTime date2)
    {
        return date1.Date == date2.Date;
    }

    /// <summary>
    /// Get number of business days between two dates.
    /// </summary>
    public static int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        int businessDays = 0;
        var current = startDate;

        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                businessDays++;

            current = current.AddDays(1);
        }

        return businessDays;
    }
}
