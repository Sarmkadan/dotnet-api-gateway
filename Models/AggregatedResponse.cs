// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Contains the aggregated results from multiple requests
/// </summary>
public class AggregatedResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, AggregatedResponseData> Responses { get; set; } = [];
    public DateTime AggregatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalDuration { get; set; }
    public int SuccessCount { get; set; } = 0;
    public int FailureCount { get; set; } = 0;
    public bool HasErrors => FailureCount > 0;

    public void AddResponse(string alias, int statusCode, string? body, Dictionary<string, string>? headers = null, TimeSpan? duration = null)
    {
        var response = new AggregatedResponseData
        {
            Alias = alias,
            StatusCode = statusCode,
            Body = body,
            Headers = headers ?? [],
            Duration = duration ?? TimeSpan.Zero,
            ReceivedAt = DateTime.UtcNow
        };

        Responses[alias] = response;

        if (statusCode >= 200 && statusCode < 300)
            SuccessCount++;
        else
            FailureCount++;
    }

    public AggregatedResponseData? GetResponse(string alias)
    {
        return Responses.TryGetValue(alias, out var response) ? response : null;
    }

    public bool IsSuccessful()
    {
        return FailureCount == 0;
    }

    public double GetAverageResponseTime()
    {
        if (Responses.Count == 0)
            return 0;

        var totalMs = Responses.Values.Sum(r => r.Duration.TotalMilliseconds);
        return totalMs / Responses.Count;
    }
}

/// <summary>
/// Individual response data from an aggregated request
/// </summary>
public class AggregatedResponseData
{
    public string Alias { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public TimeSpan Duration { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? Error { get; set; }

    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
