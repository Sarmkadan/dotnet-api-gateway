#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class GatewayRouteTests
{
    private static RouteTarget ValidTarget() => new()
    {
        Name = "backend",
        BaseUrl = "http://backend:8080",
        Weight = 1,
        HealthCheckIntervalSeconds = 60
    };

    private static GatewayRoute ValidRoute() => new()
    {
        Name = "UserRoute",
        PathPattern = "/api/users/{id}",
        AllowedMethods = ["GET", "PUT"],
        Targets = [ValidTarget()],
        TimeoutSeconds = 30
    };

    // --- MatchesPath ---

    [Fact]
    public void MatchesPath_ExactStaticPath_ReturnsTrue()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/health" };

        // Act / Assert
        route.MatchesPath("/api/health").Should().BeTrue();
    }

    [Fact]
    public void MatchesPath_WildcardSegment_MatchesAnyValue()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/*/details" };

        // Act / Assert
        route.MatchesPath("/api/users/details").Should().BeTrue();
        route.MatchesPath("/api/orders/details").Should().BeTrue();
    }

    [Fact]
    public void MatchesPath_ParameterSegment_MatchesVariableId()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };

        // Act / Assert
        route.MatchesPath("/api/users/42").Should().BeTrue();
        route.MatchesPath("/api/users/some-uuid").Should().BeTrue();
    }

    [Fact]
    public void MatchesPath_TooManySegments_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };
        route.MatchesPath("/api/users/42/orders").Should().BeFalse();
    }

    [Fact]
    public void MatchesPath_TooFewSegments_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };
        route.MatchesPath("/api/users").Should().BeFalse();
    }

    [Fact]
    public void MatchesPath_StaticSegmentMismatch_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users" };
        route.MatchesPath("/api/orders").Should().BeFalse();
    }

    [Fact]
    public void MatchesPath_StaticSegmentsCaseInsensitive_ReturnsTrue()
    {
        var route = new GatewayRoute { PathPattern = "/api/Health" };
        route.MatchesPath("/api/health").Should().BeTrue();
    }

    // --- SupportsMethod ---

    [Theory]
    [InlineData("GET", true)]
    [InlineData("get", true)]
    [InlineData("Put", true)]
    [InlineData("DELETE", false)]
    [InlineData("PATCH", false)]
    public void SupportsMethod_CaseInsensitive_ReturnsExpected(string method, bool expected)
    {
        // Arrange
        var route = new GatewayRoute { AllowedMethods = ["GET", "PUT"] };

        // Act / Assert
        route.SupportsMethod(method).Should().Be(expected);
    }

    // --- Validate ---

    [Fact]
    public void Validate_AllValid_DoesNotThrow()
    {
        var route = ValidRoute();
        var act = () => route.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MissingName_ThrowsArgumentException()
    {
        // Arrange
        var route = ValidRoute();
        route.Name = "";

        // Act
        var act = () => route.Validate();

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Validate_WhitespaceName_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.Name = "   ";
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_MissingPathPattern_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.PathPattern = "";
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_NoAllowedMethods_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.AllowedMethods = [];
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_NoTargets_ThrowsArgumentException()
    {
        // Arrange
        var route = ValidRoute();
        route.Targets = [];

        // Act
        var act = () => route.Validate();

        // Assert — message references "target"
        act.Should().Throw<ArgumentException>().WithMessage("*target*");
    }

    [Fact]
    public void Validate_TimeoutBelowMinimum_ThrowsArgumentException()
    {
        // Arrange
        var route = ValidRoute();
        route.TimeoutSeconds = 0;

        // Act / Assert
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>().WithMessage("*Timeout*");
    }

    [Fact]
    public void Validate_TimeoutAboveMaximum_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.TimeoutSeconds = 301;
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>().WithMessage("*Timeout*");
    }

    [Fact]
    public void Validate_TimeoutAtBoundary300_DoesNotThrow()
    {
        var route = ValidRoute();
        route.TimeoutSeconds = 300;
        var act = () => route.Validate();
        act.Should().NotThrow();
    }
}
