#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Thrown when JWT validation or authentication fails
/// </summary>
public class AuthenticationException : GatewayException
{
    public string? TokenType { get; set; }
    public string? Reason { get; set; }

    public AuthenticationException(
        string message,
        string? tokenType = null,
        string? reason = null)
        : base(message, "UNAUTHORIZED", 401)
    {
        TokenType = tokenType;
        Reason = reason;
    }
}
