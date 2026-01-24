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
    public static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            return baseUrl ?? path ?? "/";

        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedPath = path.TrimStart('/');

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

        var query = queryString.TrimStart('?');
        var pairs = query.Split('&');

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length >= 2)
            {
                var key = HttpUtility.UrlDecode(parts[0]);
                var value = HttpUtility.UrlDecode(parts[1]);

                if (!parameters.ContainsKey(key))
                    parameters[key] = value;
            }
        }

        return parameters;
    }

    /// <summary>
    /// Build query string from dictionary of parameters.
    /// Properly encodes values for use in URLs.
    /// </summary>
    public static string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;

        var pairs = parameters.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}");
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
    public static string SanitizeUrl(string url, string[] sensitiveParams = null)
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
