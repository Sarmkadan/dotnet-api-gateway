#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Formatters;

using System.Collections;
using System.Reflection;
using System.Text;

/// <summary>
/// Formatter for exporting data to CSV format.
/// Supports converting lists of objects to comma-separated values with headers.
/// </summary>
public static class CsvFormatter
{
    /// <summary>
    /// Convert list of objects to CSV string.
    /// Uses public properties as columns.
    /// </summary>
    public static string FormatCsv<T>(IEnumerable<T> items) where T : class
    {
        if (items is null)
            return string.Empty;

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return string.Empty;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase);
        var sb = new StringBuilder();

        // Write header
        var headers = properties.Select(p => EscapeCsvValue(p.Name));
        sb.AppendLine(string.Join(",", headers));

        // Write data rows
        foreach (var item in itemList)
        {
            var values = properties.Select(p => EscapeCsvValue(GetPropertyValue(item, p)));
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert list of dictionaries to CSV string.
    /// Uses dictionary keys as headers.
    /// </summary>
    public static string FormatCsv(IEnumerable<Dictionary<string, object?>> items)
    {
        if (items is null)
            return string.Empty;

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return string.Empty;

        var headers = new HashSet<string>();
        foreach (var item in itemList)
        {
            foreach (var key in item.Keys)
            {
                headers.Add(key);
            }
        }

        var sb = new StringBuilder();

        // Write header
        sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsvValue(h))));

        // Write data rows
        foreach (var item in itemList)
        {
            var values = headers.Select(h => EscapeCsvValue(item.ContainsKey(h) ? item[h]?.ToString() ?? "" : ""));
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export list to CSV bytes.
    /// </summary>
    public static byte[] FormatCsvBytes<T>(IEnumerable<T> items) where T : class
    {
        var csv = FormatCsv(items);
        return Encoding.UTF8.GetBytes(csv);
    }

    /// <summary>
    /// Escape CSV value for safe inclusion in CSV format.
    /// Handles quotes and commas by wrapping in double quotes.
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    /// <summary>
    /// Get property value from object safely.
    /// </summary>
    private static string GetPropertyValue(object obj, PropertyInfo property)
    {
        try
        {
            var value = property.GetValue(obj);
            return value?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
