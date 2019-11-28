# DateTimeUtilityTests

## Overview
`DateTimeUtilityTests` is a test class that verifies the correctness of the helper methods in the `DateTimeUtility` class. Each test method exercises a specific scenario—such as conversion to/from Unix timestamps, formatting, relative time calculation, and date comparison—to ensure the utility behaves as expected under typical conditions.

## API
| Method | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `ToUnixTimestamp_ValidDate_ReturnsCorrectTimestamp` | Confirms that `DateTimeUtility.ToUnixTimestamp` returns the expected Unix epoch seconds for a valid `DateTime`. | None | `void` | Throws an assertion exception (e.g., `AssertFailedException`) if the returned timestamp does not match the expected value. |
| `FromUnixTimestamp_ValidTimestamp_ReturnsCorrectDate` | Verifies that `DateTimeUtility.FromUnixTimestamp` correctly converts a valid Unix timestamp back to a `DateTime`. | None | `void` | Throws an assertion exception if the resulting `DateTime` differs from the expected date/time. |
| `FormatDateTime_ValidDate_ReturnsFormattedString` | Ensures that `DateTimeUtility.FormatDateTime` produces the correctly formatted string for‑DateTime` input. | None | `void` | Throws an assertion exception when the formatted string does not equal the expected pattern. |
| `GetRelativeTime_RecentDate_ReturnsJustNow` | Checks that `DateTimeUtility.GetRelativeTime` returns the string “just now” for a date that is within a few seconds of the current time. | None | `void` | Throws an assertion exception if the returned relative time string is not “just now”. |
| `IsPast_PastDate_ReturnsTrue` | Validates that `DateTimeUtility.IsPast` returns `true` when supplied with a date earlier than the current moment. | None | `void` | Throws an assertion exception if the method returns `false` for a past date. |
| `IsFuture_FutureDate_ReturnsTrue` | Confirms that `DateTimeUtility.IsFuture` returns `true` for a date later than the current moment. | None | `void` | Throws an assertion exception if the method returns `false` for a future date. |
| `IsSameDay_SameDayDifferentTime_ReturnsTrue` | Ensures that `DateTimeUtility.IsSameDay` returns `true` when two `DateTime` values fall on the same calendar day, regardless of their time components. | None | `void` | Throws an assertion exception if the method returns `false` for same‑day dates with different times. |
| `GetBusinessDaysBetween_OneWeekRange_ReturnsFive` | Validates that `DateTimeUtility.GetBusinessDaysBetween` correctly counts five business days between a Monday and the following Friday, excluding weekends. | None | `void` | Throws an assertion exception if the returned count differs from the expected five business days. |

## Usage
The following examples illustrate how the underlying `DateTimeUtility` methods are invoked in typical application code. The test methods themselves are invoked by a unit‑test runner (e.g., xUnit, NUnit, MSTest) and are not called directly in production code.

```csharp
using YourNamespace.Utilities; // Adjust namespace as needed

// Example 1: Convert a DateTime to a Unix timestamp and back
DateTime now = DateTime.UtcNow;
long timestamp = DateTimeUtility.ToUnixTimestamp(now);
DateTime roundTrip = DateTimeUtility.FromUnixTimestamp(timestamp);
// roundTrip should represent the same instant as `now` (within second precision).

// Example 2: Determine if a date is in the past or future and get a relative description
DateTime yesterday = DateTime.UtcNow.AddDays(-1);
DateTime tomorrow  = DateTime.UtcNow.AddDays(1);

bool isPast  = DateTimeUtility.IsPast(yesterday);   // true
bool isFuture = DateTimeUtility.IsFuture(tomorrow); // true
string relative = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddSeconds(-5));
// relative is likely "just now"
```

## Notes
- **Edge cases**: The utility methods assume `DateTime` values are expressed in UTC unless otherwise noted. Supplying a `DateTime` with an unspecified or local kind may produce unexpected results because the conversion to Unix timestamp treats the value as UTC. Callers should normalize inputs to `DateTime.UtcNow` or explicitly specify `DateTimeKind.Utc` before invoking the methods.
- **Overflow**: `ToUnixTimestamp` will overflow for dates far outside the range representable by a 64‑bit signed integer (approximately years ±292 billion). The tests do not cover these extremes; production code should validate input ranges if such values are possible.
- **Thread safety**: All methods in `DateTimeUtility` are stateless and rely only on immutable inputs, making them thread‑safe. Consequently, the test class does not require any synchronization, and multiple test threads can execute the test methods concurrently without interference.
