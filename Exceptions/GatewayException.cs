#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Base exception for all gateway-related errors
/// </summary>
public class GatewayException : Exception
{
    public string ErrorCode { get; set; }
    public int StatusCode { get; set; }

    public GatewayException(string message, string errorCode = "GATEWAY_ERROR", int statusCode = 500)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public GatewayException(string message, Exception innerException, string errorCode = "GATEWAY_ERROR", int statusCode = 500)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
