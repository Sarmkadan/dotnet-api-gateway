#nullable enable
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
public sealed class JwtValidationService
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
            _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                throw new AuthenticationException("Invalid token format", "Bearer", "Not a JWT");

            return ExtractClientIdentity(jwtToken);
        }
        catch (AuthenticationException)
        {
            throw;
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
            ClockSkew = TimeSpan.Zero, // Set to zero, and handle skew for 'exp' in custom validator
            LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
            {
                if (validationParameters.ValidateLifetime)
                {
                    // Strictly validate 'nbf': token must not be used before 'notBefore'
                    // Allow a very small tolerance (e.g., 1 second) for clock synchronization
                    if (notBefore.HasValue && notBefore.Value > DateTime.UtcNow + TimeSpan.FromSeconds(1))
                    {
                        return false; // Token is not yet valid
                    }

                    // Validate 'exp': token must not have expired, applying the configured clock skew
                    if (expires.HasValue)
                    {
                        var adjustedExpires = expires.Value + TimeSpan.FromSeconds(policy.ClockSkewSeconds);
                        if (adjustedExpires < DateTime.UtcNow)
                        {
                            return false; // Token has expired after accounting for clock skew
                        }
                    }
                }
                return true; // Valid
            }
        };

        if (!policy.ValidateSignature)
        {
            // Skip cryptographic signature verification entirely: read the token
            // without checking its signature instead of resolving a signing key.
            parameters.RequireSignedTokens = false;
            parameters.SignatureValidator = (token, _) => new JwtSecurityToken(token);
        }

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

    private static ClientIdentity ExtractClientIdentity(JwtSecurityToken jwtToken)
    {
        // Read the raw (unmapped) JWT payload claims. The default JwtSecurityTokenHandler
        // remaps short claim names on the principal ("sub" becomes ClaimTypes.NameIdentifier,
        // "role" becomes ClaimTypes.Role, and so on), so lookups against the principal by
        // short name silently miss.
        var subject = FindRawClaim(jwtToken, "sub");
        var nameId = FindRawClaim(jwtToken, "nameid", System.Security.Claims.ClaimTypes.NameIdentifier);

        var identity = new ClientIdentity
        {
            Id = subject ?? nameId ?? Guid.NewGuid().ToString(),
            Subject = subject,
            Name = FindRawClaim(jwtToken, "name", "unique_name", System.Security.Claims.ClaimTypes.Name),
            Email = FindRawClaim(jwtToken, "email", System.Security.Claims.ClaimTypes.Email),
            IssuedAt = jwtToken.IssuedAt
        };

        // Extract scopes
        var scopeClaim = FindRawClaim(jwtToken, "scope", "scp");
        if (!string.IsNullOrWhiteSpace(scopeClaim))
        {
            identity.Scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        // Extract roles from the common claim names
        var roles = jwtToken.Claims
            .Where(c => c.Type is "role" or "roles" || c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
        if (roles.Length > 0)
        {
            identity.Roles = roles;
        }

        // Extract other claims
        foreach (var claim in jwtToken.Claims.Where(c => !IsStandardClaim(c.Type)))
        {
            identity.Claims[claim.Type] = claim.Value;
        }

        // Set expiration
        if (jwtToken.ValidTo > DateTime.UtcNow)
            identity.ExpiresAt = jwtToken.ValidTo;

        return identity;
    }

    private static string? FindRawClaim(JwtSecurityToken jwtToken, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, claimType, StringComparison.Ordinal))?.Value;
            if (value is not null)
                return value;
        }

        return null;
    }

    private static bool IsStandardClaim(string claimType)
    {
        return claimType is "sub" or "iss" or "aud" or "iat" or "exp" or "nbf" or "jti" or "scope" or "scp"
            or "name" or "unique_name" or "nameid" or "email" or "role" or "roles"
            || claimType == System.Security.Claims.ClaimTypes.NameIdentifier
            || claimType == System.Security.Claims.ClaimTypes.Name
            || claimType == System.Security.Claims.ClaimTypes.Email
            || claimType == System.Security.Claims.ClaimTypes.Role;
    }
}
