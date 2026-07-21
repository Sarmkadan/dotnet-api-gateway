#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Web;

/// <summary>
/// Utility class for URL and URI manipulation operations.
/// Provides helpers for parsing, encoding, and transforming URLs safely.
/// </summary>
public static class UrlUtility
{
    /// <summary>
    /// Combine base URL with path, handling trailing slashes correctly.
    /// Ensures no double slashes in the final URL.
    /// </summary>
    public static string CombineUrl(string baseUrl, string path, bool encodePath = false)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            return baseUrl ?? path ?? "/";

        var trimmedBase = baseUrl.TrimEnd('/').Replace("%2f", "/");
        var trimmedPath = path.TrimStart('/').Replace("%2f", "/");

        // Hotfix: Handle encoded path segments properly
        if (encodePath)
        {
            trimmedPath = HttpUtility.UrlPathEncode(trimmedPath);
        }

        return $"{trimmedBase}/{trimmedPath}";
    }

    /// <summary>
    /// Parse query string into dictionary of key-value pairs.
    /// Handles URL-encoded values and duplicate keys (keeps first value).
    /// </summary>
    public static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var parameters = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(queryString))
            return parameters;

        ReadOnlySpan<char> query = queryString.AsSpan().TrimStart('?');
        int start = 0;

        while (start < query.Length)
        {
            int ampIndex = query.Slice(start).IndexOf('&');
            ReadOnlySpan<char> pair = ampIndex < 0
                ? query.Slice(start)
                : query.Slice(start, ampIndex);

            if (!pair.IsEmpty)
            {
                int eqIndex = pair.IndexOf('=');
                ReadOnlySpan<char> key = eqIndex < 0
                    ? pair
                    : pair.Slice(0, eqIndex);
                ReadOnlySpan<char> value = eqIndex < 0
                    ? ReadOnlySpan<char>.Empty
                    : pair.Slice(eqIndex + 1);

                if (!key.IsEmpty)
                {
                    var decodedKey = HttpUtility.UrlDecode(key.ToString());
                    var decodedValue = HttpUtility.UrlDecode(value.ToString());

                    if (!parameters.ContainsKey(decodedKey))
                        parameters[decodedKey] = decodedValue;
                }
            }

            start = ampIndex < 0 ? query.Length : start + ampIndex + 1;
        }

        return parameters;
    }

    /// <summary>
    /// Build query string from dictionary of parameters.
    /// Properly encodes values for use in URLs.
    /// </summary>
    public static string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return string.Empty;

        var pairs = new List<string>(parameters.Count);
        foreach (var kvp in parameters)
        {
            pairs.Add(string.Concat(HttpUtility.UrlEncode(kvp.Key), "=", HttpUtility.UrlEncode(kvp.Value)));
        }
        return "?" + string.Join("&", pairs);
    }

    /// <summary>
    /// Extract hostname from full URL.
    /// </summary>
    public static string? GetHostname(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Host;
    }

    /// <summary>
    /// Extract port number from URL, returning default ports for http/https.
    /// </summary>
    public static int GetPort(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return 80;

        if (uri.Port == -1)
        {
            return uri.Scheme.ToLowerInvariant() == "https" ? 443 : 80;
        }

        return uri.Port;
    }

    /// <summary>
    /// Check if URL is valid and can be used for forwarding.
    /// Validates both format and scheme.
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == "http" || uri.Scheme == "https";
    }

    /// <summary>
    /// Remove or replace sensitive parameters from query string (e.g., passwords, tokens).
    /// Useful for logging URLs without exposing secrets.
    /// </summary>
    public static string SanitizeUrl(string url, string[]? sensitiveParams = null)
    {
        sensitiveParams ??= new[] { "password", "token", "api_key", "secret", "authorization" };

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var parameters = ParseQueryString(uri.Query);
        var sanitized = new Dictionary<string, string>();

        foreach (var param in parameters)
        {
            if (sensitiveParams.Any(s => param.Key.Equals(s, StringComparison.OrdinalIgnoreCase)))
                sanitized[param.Key] = "***";
            else
                sanitized[param.Key] = param.Value;
        }

        var safeQuery = BuildQueryString(sanitized);
        return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}{safeQuery}";
    }

    /// <summary>
    /// Extract path from URL, removing query string and fragment.
    /// </summary>
    public static string GetPath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        return uri.AbsolutePath;
    }

    /// <summary>
    /// Check if URL has a specific query parameter.
    /// </summary>
    public static bool HasQueryParameter(string url, string paramName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var parameters = ParseQueryString(uri.Query);
        return parameters.ContainsKey(paramName);
    }
}
