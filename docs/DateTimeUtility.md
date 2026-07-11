# DateTimeUtility

A utility class providing common date and time operations including Unix timestamp conversions, date range calculations, business day counting, and human-readable formatting.

## API

### `public static string GetCurrentUtcIso8601()`
Returns the current UTC date and time formatted as an ISO 8601 string.

- **Returns**: A string in ISO 8601 format (e.g., `"2024-05-20T14:30:00Z"`).
- **Throws**: `System.InvalidOperationException` if the system clock cannot be read.

---

### `public static long GetCurrentUnixTimestamp()`
Returns the current UTC timestamp as the number of seconds since the Unix epoch (1970-01-01T00:00:00Z).

- **Returns**: A `long` representing the Unix timestamp.
- **Throws**: `System.InvalidOperationException` if the system clock cannot be read.

---

### `public static long ToUnixTimestamp(DateTime dateTime)`
Converts the specified `DateTime` to a Unix timestamp (seconds since epoch).

- **Parameters**:
  - `dateTime` (`DateTime`): The date and time to convert. Assumed to be in UTC; if `Kind` is `Local` or `Unspecified`, it is treated as UTC.
- **Returns**: A `long` representing the Unix timestamp.
- **Throws**: `System.ArgumentOutOfRangeException` if the input date is before the Unix epoch.

---

### `public static DateTime FromUnixTimestamp(long unixTimestamp)`
Converts a Unix timestamp (seconds since epoch) to a `DateTime` in UTC.

- **Parameters**:
  - `unixTimestamp` (`long`): The Unix timestamp to convert.
- **Returns**: A `DateTime` in UTC representing the timestamp.
- **Throws**: `System.ArgumentOutOfRangeException` if the timestamp is before the Unix epoch or exceeds the maximum representable date.

---

### `public static string FormatDateTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")`
Formats a `DateTime` using a custom format string.

- **Parameters**:
  - `dateTime` (`DateTime`): The date and time to format.
  - `format` (`string`, optional): The format string (default: `"yyyy-MM-dd HH:mm:ss"`).
- **Returns**: A formatted string representation of the date and time.
- **Throws**: `System.FormatException` if the format string is invalid.

---

### `public static string GetRelativeTime(DateTime dateTime)`
Generates a human-readable relative time string (e.g., "2 hours ago", "in 3 days").

- **Parameters**:
  - `dateTime` (`DateTime`): The date and time to compare against the current time.
- **Returns**: A localized string describing the relative time.
- **Throws**: `System.InvalidOperationException` if the system clock cannot be read.

---

### `public static bool IsPast(DateTime dateTime)`
Determines whether the specified date and time is in the past relative to the current UTC time.

- **Parameters**:
  - `dateTime` (`DateTime`): The date and time to check.
- **Returns**: `true` if the date and time is in the past; otherwise, `false`.
- **Throws**: `System.InvalidOperationException` if the system clock cannot be read.

---
### `public static bool IsFuture(DateTime dateTime)`
Determines whether the specified date and time is in the future relative to the current UTC time.

- **Parameters**:
  - `dateTime` (`DateTime`): The date and time to check.
- **Returns**: `true` if the date and time is in the future; otherwise, `false`.
- **Throws**: `System.InvalidOperationException` if the system clock cannot be read.

---
### `public static DateTime GetStartOfDay(DateTime dateTime)`
Returns a `DateTime` representing the start of the day (00:00:00) for the given date.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
- **Returns**: A `DateTime` at the start of the day.
- **Throws**: None.

---
### `public static DateTime GetEndOfDay(DateTime dateTime)`
Returns a `DateTime` representing the end of the day (23:59:59.999) for the given date.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
- **Returns**: A `DateTime` at the end of the day.
- **Throws**: None.

---
### `public static DateTime GetStartOfWeek(DateTime dateTime, DayOfWeek startDay = DayOfWeek.Monday)`
Returns a `DateTime` representing the start of the week for the given date, where the week starts on the specified day.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
  - `startDay` (`DayOfWeek`, optional): The first day of the week (default: `DayOfWeek.Monday`).
- **Returns**: A `DateTime` at the start of the week.
- **Throws**: None.

---
### `public static DateTime GetEndOfWeek(DateTime dateTime, DayOfWeek startDay = DayOfWeek.Monday)`
Returns a `DateTime` representing the end of the week for the given date, where the week starts on the specified day.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
  - `startDay` (`DayOfWeek`, optional): The first day of the week (default: `DayOfWeek.Monday`).
- **Returns**: A `DateTime` at the end of the week.
- **Throws**: None.

---
### `public static DateTime GetStartOfMonth(DateTime dateTime)`
Returns a `DateTime` representing the start of the month (1st day at 00:00:00) for the given date.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
- **Returns**: A `DateTime` at the start of the month.
- **Throws**: None.

---
### `public static DateTime GetEndOfMonth(DateTime dateTime)`
Returns a `DateTime` representing the end of the month (last day at 23:59:59.999) for the given date.

- **Parameters**:
  - `dateTime` (`DateTime`): The date to use.
- **Returns**: A `DateTime` at the end of the month.
- **Throws**: None.

---
### `public static bool IsSameDay(DateTime date1, DateTime date2)`
Determines whether two `DateTime` instances represent the same calendar day.

- **Parameters**:
  - `date1` (`DateTime`): The first date.
  - `date2` (`DateTime`): The second date.
- **Returns**: `true` if both dates are on the same calendar day; otherwise, `false`.
- **Throws**: None.

---
### `public static int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)`
Calculates the number of business days (Monday through Friday) between two dates, inclusive.

- **Parameters**:
  - `startDate` (`DateTime`): The start date.
  - `endDate` (`DateTime`): The end date.
- **Returns**: The number of business days between the two dates.
- **Throws**:
  - `System.ArgumentException` if `startDate` is after `endDate`.
  - `System.ArgumentOutOfRangeException` if either date is outside the valid range.

## Usage
