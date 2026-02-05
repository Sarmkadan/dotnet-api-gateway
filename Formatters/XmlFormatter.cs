#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Formatters;

using System.Text;
using System.Xml.Serialization;

/// <summary>
/// Formatter for XML serialization of responses.
/// Provides XML output format for legacy systems and specific API requirements.
/// </summary>
public static class XmlFormatter
{
    /// <summary>
    /// Serialize object to XML string.
    /// </summary>
    public static string Serialize<T>(T obj) where T : class
    {
        if (obj is null)
            return "<null />";

        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, obj);
            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            return $"<error><message>{EscapeXml(ex.Message)}</message></error>";
        }
    }

    /// <summary>
    /// Serialize object to XML bytes.
    /// </summary>
    public static byte[] SerializeToBytes<T>(T obj) where T : class
    {
        var xml = Serialize(obj);
        return Encoding.UTF8.GetBytes(xml);
    }

    /// <summary>
    /// Deserialize XML string to object.
    /// </summary>
    public static T? Deserialize<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            return (T?)xmlSerializer.Deserialize(stringReader);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Escape string for safe inclusion in XML.
    /// </summary>
    public static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// Unescape XML-escaped string.
    /// </summary>
    public static string UnescapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&apos;", "'")
            .Replace("&quot;", "\"")
            .Replace("&gt;", ">")
            .Replace("&lt;", "<")
            .Replace("&amp;", "&");
    }
}
