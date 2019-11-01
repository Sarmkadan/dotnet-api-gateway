// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Service for validating JWT tokens and extracting claims
/// </summary>
public class JwtValidationService
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtValidationService()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<ClientIdentity> ValidateTokenAsync(string token, AuthenticationPolicy policy)
    {
        if (!policy.Enabled || policy.Type != AuthenticationType.Bearer)
            throw new AuthenticationException("JWT validation is not enabled", "Bearer", "Policy disabled");

        if (string.IsNullOrWhiteSpace(token))
            throw new AuthenticationException("Token is required", "Bearer", "Empty token");

        try
        {
            var validationParameters = BuildValidationParameters(policy);
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                throw new AuthenticationException("Invalid token format", "Bearer", "Not a JWT");

            return ExtractClientIdentity(jwtToken, principal);
        }
        catch (SecurityTokenException ex)
        {
            throw new AuthenticationException($"Token validation failed: {ex.Message}", "Bearer", ex.Message);
        }
        catch (Exception ex)
        {
            throw new AuthenticationException($"Unexpected error during token validation: {ex.Message}", "Bearer", ex.Message);
        }
    }

    public JwtSecurityToken DecodeToken(string token)
    {
        if (!_tokenHandler.CanReadToken(token))
            throw new AuthenticationException("Token cannot be read", "Bearer", "Invalid format");

        return _tokenHandler.ReadJwtToken(token);
    }

    private TokenValidationParameters BuildValidationParameters(AuthenticationPolicy policy)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = policy.ValidateSignature,
            ValidateIssuer = !string.IsNullOrWhiteSpace(policy.JwtIssuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(policy.JwtAudience),
            ValidateLifetime = policy.ValidateExpiration,
            ClockSkew = TimeSpan.FromSeconds(policy.ClockSkewSeconds)
        };

        if (!string.IsNullOrWhiteSpace(policy.JwtSecret))
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(policy.JwtSecret));
            parameters.IssuerSigningKey = key;
        }

        if (!string.IsNullOrWhiteSpace(policy.JwtIssuer))
            parameters.ValidIssuer = policy.JwtIssuer;

        if (!string.IsNullOrWhiteSpace(policy.JwtAudience))
            parameters.ValidAudience = policy.JwtAudience;

        return parameters;
    }

    private ClientIdentity ExtractClientIdentity(JwtSecurityToken jwtToken, System.Security.Claims.ClaimsPrincipal principal)
    {
        var identity = new ClientIdentity
        {
            Id = principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameid")?.Value ?? Guid.NewGuid().ToString(),
            Subject = principal.FindFirst("sub")?.Value,
            Name = principal.FindFirst("name")?.Value,
            Email = principal.FindFirst("email")?.Value,
            IssuedAt = jwtToken.IssuedAt
        };

        // Extract scopes
        var scopeClaim = principal.FindFirst("scope")?.Value ?? principal.FindFirst("scp")?.Value;
        if (!string.IsNullOrWhiteSpace(scopeClaim))
        {
            identity.Scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        // Extract roles
        var roleClaims = principal.FindAll("role") ?? principal.FindAll("roles");
        if (roleClaims.Any())
        {
            identity.Roles = roleClaims.Select(c => c.Value).ToArray();
        }

        // Extract other claims
        foreach (var claim in principal.Claims.Where(c => !IsStandardClaim(c.Type)))
        {
            identity.Claims[claim.Type] = claim.Value;
        }

        // Set expiration
        if (jwtToken.ValidTo > DateTime.UtcNow)
            identity.ExpiresAt = jwtToken.ValidTo;

        return identity;
    }

    private static bool IsStandardClaim(string claimType)
    {
        return claimType is "sub" or "iss" or "aud" or "iat" or "exp" or "nbf" or "scope" or "scp" or "name" or "email" or "role" or "roles";
    }
}
