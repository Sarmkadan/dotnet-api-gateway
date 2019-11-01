#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines authentication and authorization requirements
/// </summary>
public sealed class AuthenticationPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool Enabled { get; set; } = false;
    public AuthenticationType Type { get; set; } = AuthenticationType.Bearer;
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
    public string? JwtSecret { get; set; }
    public string[] JwtAlgorithms { get; set; } = ["HS256"];
    public string[] AllowedScopes { get; set; } = [];
    public string[] AllowedRoles { get; set; } = [];
    public bool ValidateExpiration { get; set; } = true;
    public bool ValidateSignature { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 60;

    public void Validate()
    {
        if (Enabled)
        {
            if (Type == AuthenticationType.Bearer)
            {
                if (string.IsNullOrWhiteSpace(JwtSecret) && string.IsNullOrWhiteSpace(JwtIssuer))
                    throw new ArgumentException("Either JwtSecret or JwtIssuer must be provided for Bearer authentication");
            }

            if (JwtAlgorithms.Length == 0)
                throw new ArgumentException("At least one JWT algorithm must be specified");

            if (ClockSkewSeconds < 0 || ClockSkewSeconds > 300)
                throw new ArgumentException("ClockSkewSeconds must be between 0 and 300");
        }
    }

    public bool RequiresAuthentication()
    {
        return Enabled;
    }

    public bool HasScopeRequirements()
    {
        return AllowedScopes.Length > 0;
    }

    public bool HasRoleRequirements()
    {
        return AllowedRoles.Length > 0;
    }
}
