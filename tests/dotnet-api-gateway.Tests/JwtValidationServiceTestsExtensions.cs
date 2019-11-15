#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DotNetApiGateway.Tests;

public static class JwtValidationServiceTestsExtensions
{
    /// <summary>
    /// Creates a test token with custom claims for testing specific scenarios.
    /// </summary>
    /// <param name="service">The JwtValidationService instance</param>
    /// <param name="claims">Additional claims to include in the token</param>
    /// <param name="secret">JWT secret key</param>
    /// <param name="issuer">Token issuer</param>
    /// <param name="audience">Token audience</param>
    /// <param name="expiryMinutes">Token expiry in minutes</param>
    /// <returns>Generated JWT token string</returns>
    public static string CreateTestToken(this JwtValidationService service,
        IEnumerable<Claim> claims,
        string secret = "my-very-long-secret-key-for-testing-purposes-at-least-32-chars",
        string issuer = "test-issuer",
        string audience = "test-audience",
        int expiryMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var allClaims = new List<Claim>(claims)
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new("sub", "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com")
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            allClaims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates a test authentication policy with the specified validation settings.
    /// </summary>
    /// <param name="service">The JwtValidationService instance</param>
    /// <param name="enabled">Whether the policy is enabled</param>
    /// <param name="validateSignature">Whether to validate token signature</param>
    /// <param name="validateExpiration">Whether to validate token expiration</param>
    /// <param name="clockSkewSeconds">Clock skew tolerance in seconds</param>
    /// <param name="jwtSecret">JWT secret key</param>
    /// <param name="jwtIssuer">Token issuer</param>
    /// <param name="jwtAudience">Token audience</param>
    /// <returns>Configured AuthenticationPolicy</returns>
    public static AuthenticationPolicy CreateTestPolicy(this JwtValidationService service,
        bool enabled = true,
        bool validateSignature = true,
        bool validateExpiration = true,
        int clockSkewSeconds = 0,
        string? jwtSecret = null,
        string? jwtIssuer = null,
        string? jwtAudience = null)
    {
        return new AuthenticationPolicy
        {
            Enabled = enabled,
            Type = AuthenticationType.Bearer,
            ValidateSignature = validateSignature,
            ValidateExpiration = validateExpiration,
            ClockSkewSeconds = clockSkewSeconds,
            JwtSecret = jwtSecret ?? "my-very-long-secret-key-for-testing-purposes-at-least-32-chars",
            JwtIssuer = jwtIssuer ?? "test-issuer",
            JwtAudience = jwtAudience ?? "test-audience"
        };
    }

    /// <summary>
    /// Validates that a token throws a specific exception type when validation fails.
    /// </summary>
    /// <param name="service">The JwtValidationService instance</param>
    /// <param name="token">Token to validate</param>
    /// <param name="policy">Authentication policy to use</param>
    /// <param name="expectedExceptionType">Expected exception type</param>
    /// <returns>Async assertion for the exception</returns>
    public static async Task<FluentAssertions.Specialized.ExceptionAssertions<AuthenticationException>>
        ShouldThrowAuthenticationExceptionAsync(this JwtValidationService service,
        string token,
        AuthenticationPolicy policy)
    {
        var act = () => service.ValidateTokenAsync(token, policy);
        return await act.Should().ThrowAsync<AuthenticationException>();
    }

    /// <summary>
    /// Validates that a token throws a specific exception type when validation fails.
    /// </summary>
    /// <param name="service">The JwtValidationService instance</param>
    /// <param name="token">Token to validate</param>
    /// <param name="policy">Authentication policy to use</param>
    /// <param name="expectedExceptionMessage">Expected exception message substring</param>
    /// <returns>Async assertion for the exception</returns>
    public static async Task<FluentAssertions.Specialized.ExceptionAssertions<AuthenticationException>>
        ShouldThrowAuthenticationExceptionAsync(this JwtValidationService service,
        string token,
        AuthenticationPolicy policy,
        string expectedExceptionMessage)
    {
        var act = () => service.ValidateTokenAsync(token, policy);
        return await act.Should().ThrowAsync<AuthenticationException>().WithMessage("*" + expectedExceptionMessage + "*");
    }
}