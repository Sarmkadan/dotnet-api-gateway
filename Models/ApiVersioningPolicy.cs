#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// The strategy used to extract the API version from an incoming request.
/// Multiple strategies can be enabled simultaneously; they are evaluated in the
/// order listed and the first match wins.
/// </summary>
public enum VersioningStrategy
{
    /// <summary>
    /// Version is embedded in the URL path, e.g. <c>/v1/users</c> or <c>/api/v2/orders</c>.
    /// The version segment must match the pattern <c>v{N}</c> (case-insensitive).
    /// </summary>
    UrlPath,

    /// <summary>
    /// Version is provided in a request header, e.g. <c>X-API-Version: 2</c>.
    /// </summary>
    Header,

    /// <summary>
    /// Version is provided as a query parameter, e.g. <c>?api-version=1</c>.
    /// </summary>
    QueryParameter,

    /// <summary>
    /// Version is embedded in the <c>Accept</c> header media type, e.g.
    /// <c>Accept: application/vnd.myapi.v2+json</c>.
    /// </summary>
    MediaType
}

/// <summary>
/// Configures how API versioning is applied to a <see cref="GatewayRoute"/>.
/// </summary>
public sealed class ApiVersioningPolicy
{
    /// <summary>Whether versioning is enforced for this route. Defaults to false.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The default version assumed when no version information is present in the request.
    /// If <see langword="null"/> and <see cref="RequireVersionHeader"/> is false, unversioned
    /// requests are routed using the route's own path pattern without version stripping.
    /// </summary>
    public string? DefaultVersion { get; set; }

    /// <summary>
    /// When true, requests with no recognisable version segment are rejected with 400.
    /// When false, the <see cref="DefaultVersion"/> is used (if set) or the request passes through.
    /// </summary>
    public bool RequireVersion { get; set; } = false;

    /// <summary>Ordered list of strategies used to detect the version.</summary>
    public List<VersioningStrategy> Strategies { get; set; } =
    [
        VersioningStrategy.UrlPath,
        VersioningStrategy.Header,
        VersioningStrategy.QueryParameter
    ];

    /// <summary>Header name used when <see cref="VersioningStrategy.Header"/> is active.</summary>
    public string HeaderName { get; set; } = "X-API-Version";

    /// <summary>Query parameter name used when <see cref="VersioningStrategy.QueryParameter"/> is active.</summary>
    public string QueryParameterName { get; set; } = "api-version";

    /// <summary>
    /// Explicit set of supported versions. When non-empty, requests bearing an unsupported
    /// version are rejected with HTTP 400 and a descriptive error body.
    /// When empty, all parseable version values are accepted.
    /// </summary>
    public List<string> SupportedVersions { get; set; } = [];

    /// <summary>
    /// When <see cref="VersioningStrategy.UrlPath"/> is active and a version segment is found,
    /// strip it from the path before forwarding so backend services do not see it.
    /// Defaults to true.
    /// </summary>
    public bool StripVersionFromPath { get; set; } = true;
}
