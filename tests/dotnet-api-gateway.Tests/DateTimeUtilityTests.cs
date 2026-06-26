#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class DateTimeUtilityTests
{
    [Fact]
    public void ToUnixTimestamp_ValidDate_ReturnsCorrectTimestamp()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expected = 1672531200;

        // Act
        var result = DateTimeUtility.ToUnixTimestamp(date);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FromUnixTimestamp_ValidTimestamp_ReturnsCorrectDate()
    {
        // Arrange
        var timestamp = 1672531200L;
        var expected = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DateTimeUtility.FromUnixTimestamp(timestamp);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatDateTime_ValidDate_ReturnsFormattedString()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1, 12, 30, 45);
        var expected = "2023-01-01 12:30:45";

        // Act
        var result = DateTimeUtility.FormatDateTime(date);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetRelativeTime_RecentDate_ReturnsJustNow()
    {
        // Act
        var result = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddSeconds(-10));

        // Assert
        result.Should().Be("just now");
    }

    [Fact]
    public void IsPast_PastDate_ReturnsTrue()
    {
        // Act
        var result = DateTimeUtility.IsPast(DateTime.UtcNow.AddMinutes(-5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFuture_FutureDate_ReturnsTrue()
    {
        // Act
        var result = DateTimeUtility.IsFuture(DateTime.UtcNow.AddMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSameDay_SameDayDifferentTime_ReturnsTrue()
    {
        // Arrange
        var date1 = new DateTime(2023, 1, 1, 10, 0, 0);
        var date2 = new DateTime(2023, 1, 1, 20, 0, 0);

        // Act
        var result = DateTimeUtility.IsSameDay(date1, date2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetBusinessDaysBetween_OneWeekRange_ReturnsFive()
    {
        // Arrange
        var start = new DateTime(2023, 1, 2); // Monday
        var end = new DateTime(2023, 1, 6);   // Friday

        // Act
        var result = DateTimeUtility.GetBusinessDaysBetween(start, end);

        // Assert
        result.Should().Be(5);
    }
}
