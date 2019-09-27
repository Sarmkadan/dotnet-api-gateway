#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents an authenticated client identity
/// </summary>
public sealed class ClientIdentity
{
    public string Id { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string[] Scopes { get; set; } = [];
    public string[] Roles { get; set; } = [];
    public Dictionary<string, object> Claims { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }

    public bool HasScope(string scope)
    {
        return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyScopeOf(params string[] scopes)
    {
        return scopes.Any(s => HasScope(s));
    }

    public bool HasAllScopesOf(params string[] scopes)
    {
        return scopes.All(s => HasScope(s));
    }

    public bool HasAnyRoleOf(params string[] roles)
    {
        return roles.Any(r => HasRole(r));
    }

    public T? GetClaim<T>(string claimName)
    {
        if (Claims.TryGetValue(claimName, out var claim))
        {
            return claim is T typedClaim ? typedClaim : default;
        }

        return default;
    }
}
