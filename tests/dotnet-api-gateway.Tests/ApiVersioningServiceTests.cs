#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides unit tests for the <see cref="ApiVersioningService"/> class.
/// Tests various versioning strategies including URL path, headers, query parameters, and media types.
/// </summary>
public sealed class ApiVersioningServiceTests
{
    /// <summary>
/// Creates a new instance of <see cref="ApiVersioningService"/> with a null logger.
/// </summary>
/// <returns>An initialized <see cref="ApiVersioningService"/> instance.</returns>
private static ApiVersioningService CreateService() =>
        new(NullLogger<ApiVersioningService>.Instance);

    /// <summary>
/// Creates a new <see cref="DefaultHttpContext"/> with configurable request properties for testing.
/// </summary>
/// <param name="path">The request path to set on the context.</param>
/// <param name="versionHeader">Optional X-API-Version header value.</param>
/// <param name="versionQuery">Optional api-version query parameter value.</param>
/// <param name="acceptHeader">Optional Accept header value for media type versioning.</param>
/// <returns>A configured <see cref="DefaultHttpContext"/> instance.</returns>
private static DefaultHttpContext CreateContext(
        string path,
        string? versionHeader = null,
        string? versionQuery = null,
        string? acceptHeader = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;

        if (versionHeader is not null)
            ctx.Request.Headers["X-API-Version"] = versionHeader;

        if (versionQuery is not null)
            ctx.Request.QueryString = new QueryString($"?api-version={versionQuery}");

        if (acceptHeader is not null)
            ctx.Request.Headers.Accept = acceptHeader;

        return ctx;
    }

    // -------------------------------------------------------------------------
    // URL-path strategy
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that the URL path versioning strategy correctly extracts version from path segments.
/// Verifies that when a version is specified in the URL path (e.g., /v2/orders/123),
/// the service correctly identifies and returns the version number.
/// </summary>
    [Fact]
    public void TryResolveVersion_UrlPath_ExtractsVersionFromPath()
    {
        var svc = CreateService();
        var ctx = CreateContext("/v2/orders/123");
        var policy = new ApiVersioningPolicy { Enabled = true };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("2");
    }

/// <summary>
/// Tests that the URL path versioning strategy is case-insensitive.
/// Verifies that version extraction works regardless of the case used in the path (e.g., /V3/products).
/// </summary>
    [Fact]
    public void TryResolveVersion_UrlPath_CaseInsensitive()
    {
        var svc = CreateService();
        var ctx = CreateContext("/V3/products");
        var policy = new ApiVersioningPolicy { Enabled = true };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("3");
    }

    // -------------------------------------------------------------------------
    // Header strategy
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that the header versioning strategy correctly extracts version from X-API-Version header.
/// Verifies that when a version is specified in the X-API-Version header,
/// the service correctly identifies and returns the version number.
/// </summary>
    [Fact]
    public void TryResolveVersion_Header_ExtractsVersionFromHeader()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/users", versionHeader: "3");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            Strategies = [VersioningStrategy.Header]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("3");
    }

/// <summary>
/// Tests that the header versioning strategy supports custom header names.
/// Verifies that when a custom header name is configured in the policy,
/// the service correctly extracts the version from that header.
/// </summary>
    [Fact]
    public void TryResolveVersion_Header_CustomHeaderName()
    {
        var svc = CreateService();
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/data";
        ctx.Request.Headers["API-Version"] = "5";

        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            Strategies = [VersioningStrategy.Header],
            HeaderName = "API-Version"
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("5");
    }

    // -------------------------------------------------------------------------
    // Query-parameter strategy
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that the query parameter versioning strategy correctly extracts version from api-version query parameter.
/// Verifies that when a version is specified in the api-version query parameter,
/// the service correctly identifies and returns the version number.
/// </summary>
    [Fact]
    public void TryResolveVersion_QueryParam_ExtractsVersionFromQuery()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/items", versionQuery: "2");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            Strategies = [VersioningStrategy.QueryParameter]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("2");
    }

    // -------------------------------------------------------------------------
    // MediaType strategy
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that the media type versioning strategy correctly extracts version from Accept header.
/// Verifies that when a version is specified in the Accept header using media type format
/// (e.g., application/vnd.myapi.v4+json), the service correctly identifies and returns the version number.
/// </summary>
    [Fact]
    public void TryResolveVersion_MediaType_ExtractsVersionFromAcceptHeader()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/catalog", acceptHeader: "application/vnd.myapi.v4+json");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            Strategies = [VersioningStrategy.MediaType]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("4");
    }

    // -------------------------------------------------------------------------
    // Default version & required version
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that the default version is used when no version is specified in the request.
/// Verifies that when version resolution fails and a default version is configured in the policy,
/// the service returns the default version instead of failing.
/// </summary>
    [Fact]
    public void TryResolveVersion_NoVersionInRequest_UsesDefaultVersion()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/users");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            DefaultVersion = "1",
            Strategies = [VersioningStrategy.Header]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("1");
    }

/// <summary>
/// Tests that version requirement validation works correctly.
/// Verifies that when a version is required but not provided in the request,
/// the service returns false and null version to indicate failure.
/// </summary>
    [Fact]
    public void TryResolveVersion_RequireVersion_ReturnsFalseWhenMissing()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/users");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            RequireVersion = true,
            DefaultVersion = null,
            Strategies = [VersioningStrategy.Header]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeFalse();
        version.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Supported versions list
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that unsupported versions are properly rejected.
/// Verifies that when a version is specified that is not in the supported versions list,
/// the service returns false to indicate the version is not supported.
/// </summary>
    [Fact]
    public void TryResolveVersion_UnsupportedVersion_ReturnsFalse()
    {
        var svc = CreateService();
        var ctx = CreateContext("/v99/users");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            SupportedVersions = ["1", "2", "3"]
        };

        var result = svc.TryResolveVersion(ctx, policy, out _);

        result.Should().BeFalse();
    }

/// <summary>
/// Tests that supported versions are properly accepted.
/// Verifies that when a version is specified that is in the supported versions list,
/// the service returns true and the correct version number.
/// </summary>
    [Fact]
    public void TryResolveVersion_SupportedVersion_ReturnsTrue()
    {
        var svc = CreateService();
        var ctx = CreateContext("/v2/users");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            SupportedVersions = ["1", "2", "3"]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("2");
    }

    // -------------------------------------------------------------------------
    // Disabled policy
    // -------------------------------------------------------------------------

/// <summary>
/// Tests that disabled versioning policies always return success.
/// Verifies that when versioning is disabled in the policy,
/// the service always returns true regardless of the request, allowing requests to proceed.
/// </summary>
    [Fact]
    public void TryResolveVersion_PolicyDisabled_AlwaysReturnsTrue()
    {
        var svc = CreateService();
        var ctx = CreateContext("/api/data");
        var policy = new ApiVersioningPolicy { Enabled = false };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // StripVersionFromPath
    // -------------------------------------------------------------------------

    [Fact]
    public void StripVersionFromPath_RemovesVersionSegment()
    {
        var svc = CreateService();
        var policy = new ApiVersioningPolicy { StripVersionFromPath = true };

        var result = svc.StripVersionFromPath("/v2/orders/123", policy);

        result.Should().Be("/orders/123");
    }

    [Fact]
    public void StripVersionFromPath_NestedPath_RemovesVersionSegmentOnly()
    {
        var svc = CreateService();
        var policy = new ApiVersioningPolicy { StripVersionFromPath = true };

        var result = svc.StripVersionFromPath("/api/v1/users", policy);

        result.Should().Be("/api/users");
    }

    [Fact]
    public void StripVersionFromPath_NoVersionSegment_ReturnsOriginalPath()
    {
        var svc = CreateService();
        var policy = new ApiVersioningPolicy { StripVersionFromPath = true };

        var result = svc.StripVersionFromPath("/api/users", policy);

        result.Should().Be("/api/users");
    }

    [Fact]
    public void StripVersionFromPath_StripDisabled_ReturnsOriginalPath()
    {
        var svc = CreateService();
        var policy = new ApiVersioningPolicy { StripVersionFromPath = false };

        var result = svc.StripVersionFromPath("/v3/items", policy);

        result.Should().Be("/v3/items");
    }

    // -------------------------------------------------------------------------
    // Strategy priority (first match wins)
    // -------------------------------------------------------------------------

    [Fact]
    public void TryResolveVersion_MultipleStrategies_UrlPathWinsFirst()
    {
        var svc = CreateService();
        var ctx = CreateContext("/v1/items", versionHeader: "5");
        var policy = new ApiVersioningPolicy
        {
            Enabled = true,
            Strategies = [VersioningStrategy.UrlPath, VersioningStrategy.Header]
        };

        var result = svc.TryResolveVersion(ctx, policy, out var version);

        result.Should().BeTrue();
        version.Should().Be("1"); // UrlPath wins
    }
}
