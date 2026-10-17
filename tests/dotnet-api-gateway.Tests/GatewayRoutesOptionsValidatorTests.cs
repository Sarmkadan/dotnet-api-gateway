#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Configuration;
using DotNetApiGateway.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for the GatewayRoutesOptionsValidator startup validation rules.
/// </summary>
public sealed class GatewayRoutesOptionsValidatorTests
{
    /// <summary>
    /// Creates a route with a single valid upstream unless targets are given.
    /// </summary>
    private static GatewayRoute Route(string name, string path, params RouteTarget[] targets) => new()
    {
        Name = name,
        PathPattern = path,
        AllowedMethods = ["GET"],
        Targets = targets.Length > 0 ? targets : [ValidTarget(name + "-svc")]
    };

    /// <summary>
    /// Creates an upstream with a valid absolute http URI.
    /// </summary>
    private static RouteTarget ValidTarget(string name, string baseUrl = "http://backend:8080") => new()
    {
        Name = name,
        BaseUrl = baseUrl
    };

    /// <summary>
    /// Creates options with two valid routes.
    /// </summary>
    private static DotnetApiGatewayOptions ValidOptions() => new()
    {
        Routes = new List<GatewayRoute>
        {
            Route("orders", "/orders"),
            Route("users", "/users")
        }
    };

    /// <summary>
    /// Runs the validator against the given options.
    /// </summary>
    private static ValidateOptionsResult Validate(DotnetApiGatewayOptions options) =>
        new GatewayRoutesOptionsValidator().Validate(null, options);

    [Fact]
    public void Validate_ValidConfiguration_Succeeds()
    {
        var result = Validate(ValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_UpstreamTimeoutUnset_Succeeds()
    {
        var options = ValidOptions();
        options.Routes[0].Targets[0].TimeoutSeconds = null;

        var result = Validate(options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoRoutes_Fails()
    {
        var options = new DotnetApiGatewayOptions { Routes = new List<GatewayRoute>() };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("must define at least one route");
    }

    [Fact]
    public void Validate_DuplicateRouteNames_Fails()
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                Route("orders", "/orders"),
                Route("ORDERS", "/orders-v2")
            }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Route name 'orders' is used by 2 routes");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyPathPrefix_Fails(string path)
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute> { Route("orders", path) }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Route 'orders': path prefix must not be empty.");
    }

    [Fact]
    public void Validate_PathPrefixWithoutLeadingSlash_Fails()
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute> { Route("orders", "orders") }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("path prefix 'orders' must start with '/'");
    }

    [Fact]
    public void Validate_DuplicatePathPrefixes_Fails()
    {
        // Routing ignores case and trailing slashes, so these two are the same prefix
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                Route("orders", "/api"),
                Route("users", "/API/")
            }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Path prefixes '/api', '/API/' are duplicates");
    }

    [Fact]
    public void Validate_RouteWithoutUpstream_Fails()
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                new() { Name = "orders", PathPattern = "/orders", AllowedMethods = ["GET"], Targets = [] }
            }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Route 'orders': must define at least one upstream in Targets.");
    }

    [Theory]
    [InlineData("orders:8080")]
    [InlineData("/relative/path")]
    [InlineData("ftp://files.example.com")]
    [InlineData("not a uri")]
    [InlineData("")]
    public void Validate_UpstreamNotAbsoluteHttpUri_Fails(string baseUrl)
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                Route("orders", "/orders", ValidTarget("orders-svc", baseUrl))
            }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain($"upstream[0] 'orders-svc' BaseUrl '{baseUrl}' must be an absolute http or https URI.");
    }

    [Theory]
    [InlineData("http://backend:8080")]
    [InlineData("https://backend.example.com/base")]
    public void Validate_UpstreamAbsoluteHttpUri_Succeeds(string baseUrl)
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                Route("orders", "/orders", ValidTarget("orders-svc", baseUrl))
            }
        };

        var result = Validate(options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveRouteTimeout_Fails(int timeoutSeconds)
    {
        var options = ValidOptions();
        options.Routes[0].TimeoutSeconds = timeoutSeconds;

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain($"Route 'orders': TimeoutSeconds must be greater than 0 (was {timeoutSeconds}).");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveUpstreamTimeout_Fails(int timeoutSeconds)
    {
        var options = ValidOptions();
        options.Routes[0].Targets[0].TimeoutSeconds = timeoutSeconds;

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain($"upstream[0] 'orders-svc' TimeoutSeconds must be greater than 0 when set (was {timeoutSeconds}).");
    }

    [Fact]
    public void Validate_MultipleProblems_ReportsAllOfThemTogether()
    {
        var options = new DotnetApiGatewayOptions
        {
            Routes = new List<GatewayRoute>
            {
                Route("orders", "orders", ValidTarget("orders-svc", "ftp://nope")),
                new() { Name = "orders", PathPattern = "/orders", AllowedMethods = ["GET"], TimeoutSeconds = 0, Targets = [] }
            }
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(5);
        result.Failures.Should().Contain(f => f.Contains("Route name 'orders' is used by 2 routes"));
        result.Failures.Should().Contain(f => f.Contains("path prefix 'orders' must start with '/'"));
        result.Failures.Should().Contain(f => f.Contains("BaseUrl 'ftp://nope' must be an absolute http or https URI"));
        result.Failures.Should().Contain("Route 'orders': TimeoutSeconds must be greater than 0 (was 0).");
        result.Failures.Should().Contain("Route 'orders': must define at least one upstream in Targets.");
    }

    [Fact]
    public void Host_ValidConfiguration_StartsSuccessfully()
    {
        using var host = BuildHost(ValidRouteSettings());

        var act = () => host.StartAsync();

        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Host_InvalidRoutes_AbortsStartupWithAggregatedMessage()
    {
        var settings = new Dictionary<string, string?>
        {
            ["DotnetApiGateway:Routes:0:Name"] = "orders",
            ["DotnetApiGateway:Routes:0:PathPattern"] = "orders",
            ["DotnetApiGateway:Routes:1:Name"] = "orders",
            ["DotnetApiGateway:Routes:1:PathPattern"] = "/orders-v2",
            ["DotnetApiGateway:Routes:1:Targets:0:Name"] = "orders-svc",
            ["DotnetApiGateway:Routes:1:Targets:0:BaseUrl"] = "orders:8080"
        };
        using var host = BuildHost(settings);

        var act = () => host.StartAsync();

        var exception = await act.Should().ThrowAsync<OptionsValidationException>();
        exception.Which.Failures.Should().HaveCount(4);
        exception.Which.Message.Should().Contain("path prefix 'orders' must start with '/'")
            .And.Contain("Route name 'orders' is used by 2 routes")
            .And.Contain("must define at least one upstream")
            .And.Contain("BaseUrl 'orders:8080' must be an absolute http or https URI");
    }

    /// <summary>
    /// Configuration values for a single valid route.
    /// </summary>
    private static Dictionary<string, string?> ValidRouteSettings() => new()
    {
        ["DotnetApiGateway:Routes:0:Name"] = "orders",
        ["DotnetApiGateway:Routes:0:PathPattern"] = "/orders",
        ["DotnetApiGateway:Routes:0:AllowedMethods:0"] = "GET",
        ["DotnetApiGateway:Routes:0:Targets:0:Name"] = "orders-svc",
        ["DotnetApiGateway:Routes:0:Targets:0:BaseUrl"] = "http://orders:8080"
    };

    /// <summary>
    /// Builds a host that registers the gateway services from the given configuration values.
    /// </summary>
    private static IHost BuildHost(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new HostBuilder()
            .ConfigureServices(services => services.AddGatewayServices(configuration))
            .Build();
    }
}
