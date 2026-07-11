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

public sealed class JwtValidationServiceTests
{
    private const string TestSecret = "my-very-long-secret-key-for-testing-purposes-at-least-32-chars";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private static string GenerateTestToken(string secret, string issuer = TestIssuer, string audience = TestAudience,
        int expiryMinutes = 60, int notBeforeMinutes = 0)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-123"),
            new("sub", "user-123"),
            new(ClaimTypes.Name, "John Doe"),
            new(ClaimTypes.Email, "john@example.com"),
            new("role", "admin"),
            new("scope", "read write")
        };

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(expiryMinutes);
        var notBefore = notBeforeMinutes > 0 ? now.AddMinutes(notBeforeMinutes) : now;

        // JwtSecurityToken rejects expires <= notBefore, so shift notBefore back
        // when generating an already-expired token.
        if (notBefore >= expires)
            notBefore = expires.AddMinutes(-5);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: credentials,
            notBefore: notBefore
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthenticationPolicy ValidPolicy() => new()
    {
        Enabled = true,
        Type = AuthenticationType.Bearer,
        JwtSecret = TestSecret,
        JwtIssuer = TestIssuer,
        JwtAudience = TestAudience,
        ValidateSignature = true,
        ValidateExpiration = true,
        ClockSkewSeconds = 0
    };

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsClientIdentity()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);
        var policy = ValidPolicy();

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Should().NotBeNull();
        identity.Id.Should().Be("user-123");
        identity.Subject.Should().Be("user-123");
        identity.Name.Should().Be("John Doe");
        identity.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenWithScopes_ExtractsScopesCorrectly()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);
        var policy = ValidPolicy();

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Scopes.Should().Contain("read");
        identity.Scopes.Should().Contain("write");
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenWithRoles_ExtractsRolesCorrectly()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);
        var policy = ValidPolicy();

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Roles.Should().Contain("admin");
    }

    [Fact]
    public async Task ValidateTokenAsync_DisabledPolicy_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);
        var policy = new AuthenticationPolicy { Enabled = false };

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync("", policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>().WithMessage("*Token is required*");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhitespaceToken_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync("   ", policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidTokenSignature_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken("a-completely-different-secret-key-of-sufficient-length");
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, expiryMinutes: -5); // Expired 5 minutes ago
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_FutureToken_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, notBeforeMinutes: 5); // Valid in 5 minutes
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidIssuer_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, issuer: "wrong-issuer");
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidAudience_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, audience: "wrong-audience");
        var policy = ValidPolicy();

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public void DecodeToken_ValidToken_ReturnsJwtToken()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);

        // Act
        var decodedToken = service.DecodeToken(token);

        // Assert
        decodedToken.Should().NotBeNull();
        decodedToken.Subject.Should().Be("user-123");
    }

    [Fact]
    public void DecodeToken_InvalidTokenFormat_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();

        // Act
        var act = () => service.DecodeToken("not-a-valid-token");

        // Assert
        act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_NoSignatureValidation_SkipsSignatureCheck()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken("a-completely-different-secret-key-of-sufficient-length");
        var policy = new AuthenticationPolicy
        {
            Enabled = true,
            Type = AuthenticationType.Bearer,
            ValidateSignature = false,
            ValidateExpiration = false,
            JwtIssuer = TestIssuer,
            JwtAudience = TestAudience
        };

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithClockSkew_AcceptsNearExpiredToken()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, expiryMinutes: -1); // Expired 1 minute ago
        var policy = new AuthenticationPolicy
        {
            Enabled = true,
            Type = AuthenticationType.Bearer,
            JwtSecret = TestSecret,
            JwtIssuer = TestIssuer,
            JwtAudience = TestAudience,
            ValidateSignature = true,
            ValidateExpiration = true,
            ClockSkewSeconds = 120 // 2 minutes
        };

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_NoIssuerValidation_SkipsIssuerCheck()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret, issuer: "any-issuer");
        var policy = new AuthenticationPolicy
        {
            Enabled = true,
            Type = AuthenticationType.Bearer,
            JwtSecret = TestSecret,
            ValidateSignature = true,
            ValidateExpiration = true,
            JwtIssuer = null // No issuer validation
        };

        // Act
        var identity = await service.ValidateTokenAsync(token, policy);

        // Assert
        identity.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_GeneratesIdFromNameIdIfNoSub_UsesNameId()
    {
        // Arrange
        var service = new JwtValidationService();
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "nameid-123"),
            new(ClaimTypes.Name, "Jane Doe")
        };

        var token = new JwtSecurityToken(
            TestIssuer,
            TestAudience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var policy = new AuthenticationPolicy
        {
            Enabled = true,
            Type = AuthenticationType.Bearer,
            JwtSecret = TestSecret,
            ValidateSignature = false,
            ValidateExpiration = true
        };

        // Act
        var identity = await service.ValidateTokenAsync(tokenString, policy);

        // Assert
        identity.Id.Should().Be("nameid-123");
    }

    [Fact]
    public async Task ValidateTokenAsync_WrongAuthType_ThrowsAuthenticationException()
    {
        // Arrange
        var service = new JwtValidationService();
        var token = GenerateTestToken(TestSecret);
        var policy = new AuthenticationPolicy
        {
            Enabled = true,
            Type = AuthenticationType.ApiKey
        };

        // Act
        var act = () => service.ValidateTokenAsync(token, policy);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
