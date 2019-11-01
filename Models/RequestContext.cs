#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Contains context about the incoming request for gateway processing
/// </summary>
public sealed class RequestContext
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, string> QueryParameters { get; set; } = [];
    public string? Body { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public string? AuthToken { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> CustomData { get; set; } = [];
    public GatewayRoute? MatchedRoute { get; set; }
    public RouteTarget? SelectedTarget { get; set; }
    public ClientIdentity? ClientIdentity { get; set; }

    public string GetClientIdentifier()
    {
        if (ClientIdentity?.Id is not null)
            return ClientIdentity.Id;

        return ClientIp;
    }

    public bool HasAuthToken()
    {
        return !string.IsNullOrWhiteSpace(AuthToken);
    }

    public string ExtractBearerToken()
    {
        if (!HasAuthToken())
            return string.Empty;

        const string bearerPrefix = "Bearer ";
        if (AuthToken!.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthToken[bearerPrefix.Length..];

        return AuthToken;
    }

    public TimeSpan ElapsedTime()
    {
        return DateTime.UtcNow - ReceivedAt;
    }
}
