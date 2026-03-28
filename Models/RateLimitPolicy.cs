// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines rate limiting rules for a route
/// </summary>
public class RateLimitPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int RequestsPerMinute { get; set; } = 1000;
    public int RequestsPerHour { get; set; } = 50000;
    public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.TokenBucket;
    public string KeyGenerator { get; set; } = "ClientIp";
    public bool BypassForAuthenticatedUsers { get; set; } = false;
    public int BurstSize { get; set; } = 10;
    public bool Enabled { get; set; } = true;

    public void Validate()
    {
        if (RequestsPerMinute < 1)
            throw new ArgumentException("RequestsPerMinute must be at least 1");

        if (RequestsPerHour < 1)
            throw new ArgumentException("RequestsPerHour must be at least 1");

        if (RequestsPerHour < RequestsPerMinute)
            throw new ArgumentException("RequestsPerHour must be >= RequestsPerMinute");

        if (BurstSize < 1 || BurstSize > RequestsPerMinute)
            throw new ArgumentException("BurstSize must be between 1 and RequestsPerMinute");
    }

    public int GetLimitForWindow(string windowType)
    {
        return windowType.ToLower() switch
        {
            "minute" => RequestsPerMinute,
            "hour" => RequestsPerHour,
            _ => RequestsPerMinute
        };
    }

    public bool IsEnabled()
    {
        return Enabled && (RequestsPerMinute > 0 || RequestsPerHour > 0);
    }
}
