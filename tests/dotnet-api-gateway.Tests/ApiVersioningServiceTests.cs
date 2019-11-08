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

public sealed class ApiVersioningServiceTests
{
    private static ApiVersioningService CreateService() =>
        new(NullLogger<ApiVersioningService>.Instance);

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
