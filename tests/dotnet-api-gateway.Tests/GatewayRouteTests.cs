#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for the GatewayRoute class.
/// </summary>
public sealed class GatewayRouteTests
{
    /// <summary>
    /// Creates a valid RouteTarget instance.
    /// </summary>
    /// <returns>A valid RouteTarget instance.</returns>
    private static RouteTarget ValidTarget() => new()
    {
        Name = "backend",
        BaseUrl = "http://backend:8080",
        Weight = 1,
        HealthCheckIntervalSeconds = 60
    };

    /// <summary>
    /// Creates a valid GatewayRoute instance.
    /// </summary>
    /// <returns>A valid GatewayRoute instance.</returns>
    private static GatewayRoute ValidRoute() => new()
    {
        Name = "UserRoute",
        PathPattern = "/api/users/{id}",
        AllowedMethods = ["GET", "PUT"],
        Targets = [ValidTarget()],
        TimeoutSeconds = 30
    };

    // --- MatchesPath ---

    /// <summary>
    /// Verifies that the MatchesPath method returns true for an exact static path.
    /// </summary>
    [Fact]
    public void MatchesPath_ExactStaticPath_ReturnsTrue()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/health" };

        // Act / Assert
        route.MatchesPath("/api/health").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns true for a wildcard segment.
    /// </summary>
    [Fact]
    public void MatchesPath_WildcardSegment_MatchesAnyValue()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/*/details" };

        // Act / Assert
        route.MatchesPath("/api/users/details").Should().BeTrue();
        route.MatchesPath("/api/orders/details").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns true for a parameter segment.
    /// </summary>
    [Fact]
    public void MatchesPath_ParameterSegment_MatchesVariableId()
    {
        // Arrange
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };

        // Act / Assert
        route.MatchesPath("/api/users/42").Should().BeTrue();
        route.MatchesPath("/api/users/some-uuid").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns false for a path with too many segments.
    /// </summary>
    [Fact]
    public void MatchesPath_TooManySegments_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };
        route.MatchesPath("/api/users/42/orders").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns false for a path with too few segments.
    /// </summary>
    [Fact]
    public void MatchesPath_TooFewSegments_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users/{id}" };
        route.MatchesPath("/api/users").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns false for a static segment mismatch.
    /// </summary>
    [Fact]
    public void MatchesPath_StaticSegmentMismatch_ReturnsFalse()
    {
        var route = new GatewayRoute { PathPattern = "/api/users" };
        route.MatchesPath("/api/orders").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the MatchesPath method returns true for a static segment case insensitive match.
    /// </summary>
    [Fact]
    public void MatchesPath_StaticSegmentsCaseInsensitive_ReturnsTrue()
    {
        var route = new GatewayRoute { PathPattern = "/api/Health" };
        route.MatchesPath("/api/health").Should().BeTrue();
    }

    // --- SupportsMethod ---

    /// <summary>
    /// Verifies that the SupportsMethod method returns true for a supported method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="expected">The expected result.</param>
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

    /// <summary>
    /// Verifies that the Validate method does not throw for a valid route.
    /// </summary>
    [Fact]
    public void Validate_AllValid_DoesNotThrow()
    {
        var route = ValidRoute();
        var act = () => route.Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for a missing name.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for a whitespace name.
    /// </summary>
    [Fact]
    public void Validate_WhitespaceName_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.Name = "   ";
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for a missing path pattern.
    /// </summary>
    [Fact]
    public void Validate_MissingPathPattern_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.PathPattern = "";
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for no allowed methods.
    /// </summary>
    [Fact]
    public void Validate_NoAllowedMethods_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.AllowedMethods = [];
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for no targets.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for a timeout below the minimum.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Validate method throws an ArgumentException for a timeout above the maximum.
    /// </summary>
    [Fact]
    public void Validate_TimeoutAboveMaximum_ThrowsArgumentException()
    {
        var route = ValidRoute();
        route.TimeoutSeconds = 301;
        var act = () => route.Validate();
        act.Should().Throw<ArgumentException>().WithMessage("*Timeout*");
    }

    /// <summary>
    /// Verifies that the Validate method does not throw for a timeout at the boundary of 300 seconds.
    /// </summary>
    [Fact]
    public void Validate_TimeoutAtBoundary300_DoesNotThrow()
    {
        var route = ValidRoute();
        route.TimeoutSeconds = 300;
        var act = () => route.Validate();
        act.Should().NotThrow();
    }
}
