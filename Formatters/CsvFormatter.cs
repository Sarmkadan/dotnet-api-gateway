#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetApiGateway.Formatters;

using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// Formatter for exporting data to CSV format.
/// Supports converting lists of objects to comma-separated values with headers.
/// Implements RFC 4180 compliant CSV formatting with proper escaping and
/// CSV formula injection protection.
/// </summary>
public static class CsvFormatter
{
    private const char CsvSeparator = ',';
    private const char CsvQuote = '"';
    private static readonly string[] NewLineChars = { "\r\n", "\r", "\n" };
    private const string FormulaInjectionPrefix = "\t"; // Tab prefix prevents Excel formula execution

    /// <summary>
    /// Convert list of objects to CSV string.
    /// Uses public properties as columns.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="items">The collection of objects to convert.</param>
    /// <param name="cultureInfo">The culture info for number formatting (default: invariant culture).</param>
    /// <returns>CSV formatted string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public static string FormatCsv<T>(IEnumerable<T> items, CultureInfo? cultureInfo = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return string.Empty;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sb = new StringBuilder();
        var culture = cultureInfo ?? CultureInfo.InvariantCulture;

        // Write header
        var headers = properties.Select(p => EscapeCsvValue(p.Name, protectAgainstFormulaInjection: false));
        sb.AppendLine(string.Join(CsvSeparator, headers));

        // Write data rows
        foreach (var item in itemList)
        {
            var values = properties.Select(p => EscapeCsvValue(GetPropertyValue(item, p, culture), protectAgainstFormulaInjection: true));
            sb.AppendLine(string.Join(CsvSeparator, values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert list of dictionaries to CSV string.
    /// Uses dictionary keys as headers.
    /// </summary>
    /// <param name="items">The collection of dictionaries to convert.</param>
    /// <param name="cultureInfo">The culture info for number formatting (default: invariant culture).</param>
    /// <returns>CSV formatted string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public static string FormatCsv(IEnumerable<Dictionary<string, object?>> items, CultureInfo? cultureInfo = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return string.Empty;

        var headers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in itemList)
        {
            foreach (var key in item.Keys)
            {
                headers.Add(key);
            }
        }

        var sb = new StringBuilder();
        var culture = cultureInfo ?? CultureInfo.InvariantCulture;

        // Write header
        sb.AppendLine(string.Join(CsvSeparator, headers.Select(h => EscapeCsvValue(h, protectAgainstFormulaInjection: false))));

        // Write data rows
        foreach (var item in itemList)
        {
            var values = headers.Select(h => EscapeCsvValue(item.ContainsKey(h) ? item[h]?.ToString() ?? "" : "", protectAgainstFormulaInjection: true));
            sb.AppendLine(string.Join(CsvSeparator, values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export list to CSV bytes.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="items">The collection of objects to convert.</param>
    /// <param name="cultureInfo">The culture info for number formatting (default: invariant culture).</param>
    /// <returns>CSV formatted byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public static byte[] FormatCsvBytes<T>(IEnumerable<T> items, CultureInfo? cultureInfo = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);

        var csv = FormatCsv(items, cultureInfo);
        return Encoding.UTF8.GetBytes(csv);
    }

    /// <summary>
    /// Escape CSV value for safe inclusion in CSV format according to RFC 4180.
    /// Implements proper RFC 4180 quoting and CSV formula injection protection.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <param name="protectAgainstFormulaInjection">Whether to protect against CSV formula injection (default: true).</param>
    /// <returns>Escaped CSV value ready for inclusion in a field.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null and protectAgainstFormulaInjection is true.</exception>
    private static string EscapeCsvValue(string? value, bool protectAgainstFormulaInjection = true)
    {
        if (value is null)
        {
            if (protectAgainstFormulaInjection)
                ArgumentNullException.ThrowIfNull(value);
            return string.Empty;
        }

        // Handle empty string
        if (value.Length == 0)
            return string.Empty;

        // Check if value needs quoting according to RFC 4180
        // A field containing line breaks (CRLF), double quotes, or commas should be quoted
        bool needsQuoting = value.Contains(CsvSeparator) ||
                            value.Contains(CsvQuote) ||
                            value.Contains('\r') ||
                            value.Contains('\n') ||
                            (protectAgainstFormulaInjection && IsFormulaInjectionCandidate(value));

        if (!needsQuoting)
            return value;

        // Escape quotes by doubling them and wrap in quotes
        // RFC 4180: quotes within a field must be doubled
        var escapedValue = value.Replace(CsvQuote.ToString(), CsvQuote.ToString() + CsvQuote);
        return $"{CsvQuote}{escapedValue}{CsvQuote}";
    }

    /// <summary>
    /// Check if a value is a candidate for CSV formula injection protection.
    /// Values starting with =, +, -, @ can execute formulas in Excel and other spreadsheet applications.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is a formula injection candidate.</returns>
    private static bool IsFormulaInjectionCandidate(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        char firstChar = value[0];
        // Check for common formula injection patterns
        // Excel formulas start with =, Google Sheets with +, - for negative numbers, @ for array formulas
        return firstChar == '=' || firstChar == '+' || firstChar == '-' || firstChar == '@';
    }

    /// <summary>
    /// Get property value from object safely with culture-invariant formatting.
    /// </summary>
    /// <param name="obj">The object to get the property from.</param>
    /// <param name="property">The property to retrieve.</param>
    /// <param name="culture">The culture for formatting numbers.</param>
    /// <returns>The string representation of the property value.</returns>
    private static string GetPropertyValue(object obj, PropertyInfo property, CultureInfo culture)
    {
        try
        {
            var value = property.GetValue(obj);

            // Handle different value types with proper formatting
            if (value is null)
                return string.Empty;

            if (value is string strValue)
                return strValue;

            if (value is IFormattable formattable)
            {
                // Use invariant culture for numbers to ensure consistent formatting across systems
                return formattable.ToString(null, culture);
            }

            return value.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}