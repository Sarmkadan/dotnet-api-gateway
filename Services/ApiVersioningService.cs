#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using System.Text.RegularExpressions;
using DotNetApiGateway.Models;

/// <summary>
/// Extracts API version information from an HTTP request and validates it against
/// a route's <see cref="ApiVersioningPolicy"/>.
/// </summary>
public sealed class ApiVersioningService
{
    // Matches path segments like "v1", "V2", "v10"
    private static readonly Regex VersionSegmentRegex =
        new(@"(?:^|/)v(\d+)(?:/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches media-type vendor version, e.g. application/vnd.api.v2+json
    private static readonly Regex MediaTypeVersionRegex =
        new(@"\.v(\d+)\+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<ApiVersioningService> _logger;

    public ApiVersioningService(ILogger<ApiVersioningService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to resolve the API version from the request according to the route's policy.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="policy">The versioning policy on the matched route.</param>
    /// <param name="resolvedVersion">
    /// The resolved version string (e.g. "1", "2") when <see langword="true"/> is returned.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a version was successfully resolved and (if supported versions
    /// are configured) is in the allowed set; <see langword="false"/> otherwise.
    /// </returns>
    public bool TryResolveVersion(
        HttpContext context,
        ApiVersioningPolicy policy,
        out string? resolvedVersion)
    {
        resolvedVersion = null;

        if (!policy.Enabled)
        {
            resolvedVersion = policy.DefaultVersion;
            return true;
        }

        foreach (var strategy in policy.Strategies)
        {
            var candidate = strategy switch
            {
                VersioningStrategy.UrlPath       => ExtractFromUrlPath(context.Request.Path),
                VersioningStrategy.Header        => ExtractFromHeader(context.Request, policy.HeaderName),
                VersioningStrategy.QueryParameter => ExtractFromQueryString(context.Request, policy.QueryParameterName),
                VersioningStrategy.MediaType     => ExtractFromMediaType(context.Request),
                _                                => null
            };

            if (candidate is not null)
            {
                resolvedVersion = candidate;
                break;
            }
        }

        // Fall back to default version when nothing was found
        if (resolvedVersion is null)
        {
            if (policy.DefaultVersion is not null)
            {
                resolvedVersion = policy.DefaultVersion;
                _logger.LogDebug("No version in request; using default version {Version}", resolvedVersion);
            }
            else if (policy.RequireVersion)
            {
                _logger.LogWarning("Version is required but was not found in request to {Path}", context.Request.Path);
                return false;
            }
            else
            {
                // Version is optional and no default — pass through without a version
                return true;
            }
        }

        // Validate against supported versions when a restriction list is provided
        if (policy.SupportedVersions.Count > 0 &&
            !policy.SupportedVersions.Contains(resolvedVersion, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Version {Version} is not in the supported versions list [{Supported}]",
                resolvedVersion,
                string.Join(", ", policy.SupportedVersions));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Strips the version segment from a URL path when the policy is configured to do so.
    /// For example, <c>/v2/orders/123</c> becomes <c>/orders/123</c>.
    /// </summary>
    /// <param name="path">The original request path.</param>
    /// <param name="policy">The versioning policy on the matched route.</param>
    /// <returns>The path with the version segment removed, or the original path when stripping is disabled.</returns>
    public string StripVersionFromPath(string path, ApiVersioningPolicy policy)
    {
        if (!policy.StripVersionFromPath)
            return path;

        var match = VersionSegmentRegex.Match(path);
        if (!match.Success)
            return path;

        // Remove the matched version segment, normalising double slashes
        var stripped = VersionSegmentRegex.Replace(path, m =>
        {
            // Preserve leading slash; drop "v{N}" and deduplicate the surrounding slashes
            var hasLeadingSlash = m.Value.StartsWith('/');
            var hasTrailingSlash = m.Value.EndsWith('/');
            return hasLeadingSlash && hasTrailingSlash ? "/" : hasLeadingSlash ? "/" : "";
        }, count: 1);

        // Ensure the path is not empty
        return string.IsNullOrEmpty(stripped) ? "/" : stripped;
    }

    /// <summary>
    /// Builds an error response body describing the versioning problem.
    /// </summary>
    public static object BuildVersionErrorResponse(ApiVersioningPolicy policy, string? attemptedVersion)
    {
        return new
        {
            error = "Unsupported or missing API version",
            attemptedVersion,
            supportedVersions = policy.SupportedVersions,
            defaultVersion = policy.DefaultVersion,
            strategies = policy.Strategies.Select(s => s.ToString())
        };
    }

    // -------------------------------------------------------------------------
    // Extraction helpers
    // -------------------------------------------------------------------------

    private static string? ExtractFromUrlPath(PathString path)
    {
        var match = VersionSegmentRegex.Match(path.Value ?? string.Empty);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractFromHeader(HttpRequest request, string headerName)
    {
        if (request.Headers.TryGetValue(headerName, out var values))
            return values.FirstOrDefault()?.Trim();
        return null;
    }

    private static string? ExtractFromQueryString(HttpRequest request, string paramName)
    {
        if (request.Query.TryGetValue(paramName, out var values))
            return values.FirstOrDefault()?.Trim();
        return null;
    }

    private static string? ExtractFromMediaType(HttpRequest request)
    {
        var accept = request.Headers.Accept.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(accept))
            return null;

        var match = MediaTypeVersionRegex.Match(accept);
        return match.Success ? match.Groups[1].Value : null;
    }
}
