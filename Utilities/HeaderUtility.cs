#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

/// <summary>
/// Utility class for HTTP header manipulation and parsing.
/// Provides safe operations on headers with case-insensitive lookups.
/// </summary>
public static class HeaderUtility
{
    /// <summary>
    /// Extract header value safely, returning null if not found.
    /// Header names are case-insensitive per HTTP spec.
    /// </summary>
    /// <param name="headers">The header dictionary to search.</param>
    /// <param name="headerName">The name of the header to retrieve (case-insensitive).</param>
    /// <returns>The header value if found and non-empty; otherwise, <c>null</c>.</returns>
    public static string? GetHeader(IHeaderDictionary headers, string headerName)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrEmpty(headerName);

        var header = headers.FirstOrDefault(h => h.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        var value = header.Value.ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Set header value, replacing any existing value.
    /// </summary>
    /// <param name="headers">The header dictionary to modify.</param>
    /// <param name="headerName">The name of the header to set.</param>
    /// <param name="value">The value to assign to the header.</param>
    public static void SetHeader(IHeaderDictionary headers, string headerName, string value)
    {
        if (headers is null || string.IsNullOrWhiteSpace(headerName))
            return;

        headers[headerName] = value;
    }

    /// <summary>
    /// Add header value, preserving multiple values with same name.
    /// </summary>
    /// <param name="headers">The header dictionary to modify.</param>
    /// <param name="headerName">The name of the header to add.</param>
    /// <param name="value">The value to append to the header.</param>
    public static void AddHeader(IHeaderDictionary headers, string headerName, string value)
    {
        if (headers is null || string.IsNullOrWhiteSpace(headerName))
            return;

        headers.Append(headerName, value);
    }

    /// <summary>
    /// Remove header by name, case-insensitive.
    /// </summary>
    /// <param name="headers">The header dictionary to modify.</param>
    /// <param name="headerName">The name of the header to remove (case-insensitive).</param>
    public static void RemoveHeader(IHeaderDictionary headers, string headerName)
    {
        if (headers is null || string.IsNullOrWhiteSpace(headerName))
            return;

        var keysToRemove = headers
            .Where(h => h.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
            .Select(h => h.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            headers.Remove(key);
        }
    }

    /// <summary>
    /// Check if header exists, case-insensitive.
    /// </summary>
    /// <param name="headers">The header dictionary to search.</param>
    /// <param name="headerName">The name of the header to check (case-insensitive).</param>
    /// <returns><c>true</c> if the header exists; otherwise, <c>false</c>.</returns>
    public static bool HasHeader(IHeaderDictionary headers, string headerName)
    {
        if (headers is null || string.IsNullOrWhiteSpace(headerName))
            return false;

        return headers.Any(h => h.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extract bearer token from Authorization header.
    /// Returns null if header is missing or not a Bearer token.
    /// </summary>
    /// <param name="headers">The header dictionary to search.</param>
    /// <returns>The bearer token if present and well-formed; otherwise, <c>null</c>.</returns>
    public static string? ExtractBearerToken(IHeaderDictionary headers)
    {
        var authHeader = GetHeader(headers, "Authorization");
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        const string bearerScheme = "Bearer ";
        if (!authHeader.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader.Substring(bearerScheme.Length).Trim();
    }

    /// <summary>
    /// Extract and parse WWW-Authenticate header for challenge information.
    /// </summary>
    /// <param name="headers">The header dictionary to search.</param>
    /// <returns>
    /// A dictionary containing the authentication scheme (under the "scheme" key) and any
    /// additional challenge parameters (e.g., realm). Empty if the header is missing.
    /// </returns>
    public static Dictionary<string, string> ParseAuthenticationChallenge(IHeaderDictionary headers)
    {
        var challenge = GetHeader(headers, "WWW-Authenticate");
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(challenge))
            return result;

        var parts = challenge.Split(' ');
        if (parts.Length > 0)
        {
            result["scheme"] = parts[0];
        }

        // Parse additional parameters (realm, etc.)
        for (int i = 1; i < parts.Length; i++)
        {
            var param = parts[i].Trim(',');
            if (param.Contains('='))
            {
                var kvp = param.Split('=');
                result[kvp[0].Trim()] = kvp[1].Trim('"');
            }
        }

        return result;
    }

    /// <summary>
    /// Copy specific headers from source to destination headers.
    /// Skips headers in the exclude list (case-insensitive).
    /// </summary>
    /// <param name="source">The source header dictionary to copy from.</param>
    /// <param name="destination">The destination HTTP request message to copy headers to.</param>
    /// <param name="excludeHeaders">
    /// Optional list of header names to exclude from copying.
    /// Defaults to Host, Transfer-Encoding, and Content-Length.
    /// </param>
    public static void CopyHeaders(IHeaderDictionary source, HttpRequestMessage destination, string[]? excludeHeaders = null)
    {
        excludeHeaders ??= new[] { "Host", "Transfer-Encoding", "Content-Length" };

        if (source is null || destination is null)
            return;

        foreach (var header in source)
        {
            // Skip excluded headers and hop-by-hop headers
            if (excludeHeaders.Any(h => h.Equals(header.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                destination.Headers.Add(header.Key, header.Value.ToArray());
            }
            catch
            {
                // Some headers cannot be added to HttpRequestMessage (content headers, etc.)
                // Silently skip them
            }
        }
    }

    /// <summary>
    /// Get all custom headers excluding standard HTTP headers.
    /// Returns a new dictionary with custom headers only.
    /// </summary>
    /// <param name="headers">The header dictionary to filter.</param>
    /// <returns>A new dictionary containing only the custom (non-standard) headers.</returns>
    public static Dictionary<string, string> GetCustomHeaders(IHeaderDictionary headers)
    {
        var standardHeaders = new[] { "Host", "Connection", "Content-Length", "Content-Type", "Accept", "User-Agent" };
        var custom = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            if (!standardHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                custom[header.Key] = header.Value.ToString();
            }
        }

        return custom;
    }
}
