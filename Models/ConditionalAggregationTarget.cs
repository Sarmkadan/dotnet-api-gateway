#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;
using HttpMethod = DotNetApiGateway.Constants.HttpMethod;

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents a target for request aggregation that can be conditionally selected.
/// </summary>
public sealed class ConditionalAggregationTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UpstreamUrl { get; set; } = string.Empty;
    public string? JsonPathCondition { get; set; } // JSONPath expression for conditional selection
    public HttpMethod Method { get; set; } = HttpMethod.GET; // Method for the upstream call
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; } // Optional request body for the aggregated call
    public int TimeoutSeconds { get; set; } = 30;
    public bool Optional { get; set; } = false; // If true, failure of this target won't fail the whole aggregation

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(UpstreamUrl))
            throw new ArgumentException("UpstreamUrl cannot be empty for ConditionalAggregationTarget");
        
        if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
            throw new ArgumentException("TimeoutSeconds must be between 1 and 300 seconds");
    }
}
