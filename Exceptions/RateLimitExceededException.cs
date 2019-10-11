#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Exceptions;

/// <summary>
/// Thrown when a client exceeds the rate limit threshold
/// </summary>
public class RateLimitExceededException : GatewayException
{
    public string ClientId { get; set; }
    public long RemainingSeconds { get; set; }
    public int LimitPerMinute { get; set; }

    public RateLimitExceededException(
        string clientId,
        int limitPerMinute,
        long remainingSeconds)
        : base(
            $"Rate limit exceeded for client {clientId}. Limit: {limitPerMinute}/min. Reset in {remainingSeconds}s",
            "RATE_LIMIT_EXCEEDED",
            429)
    {
        ClientId = clientId;
        LimitPerMinute = limitPerMinute;
        RemainingSeconds = remainingSeconds;
    }
}
