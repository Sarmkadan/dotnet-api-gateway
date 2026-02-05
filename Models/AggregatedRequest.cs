#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Represents a request that is part of a request aggregation
/// </summary>
public sealed class AggregatedRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Alias { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, string>? QueryParameters { get; set; }
    public string? Body { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool Optional { get; set; } = false;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Alias))
            throw new ArgumentException("Alias cannot be empty");

        if (string.IsNullOrWhiteSpace(Path))
            throw new ArgumentException("Path cannot be empty");

        if (string.IsNullOrWhiteSpace(Method))
            throw new ArgumentException("Method cannot be empty");

        if (TimeoutSeconds.HasValue && (TimeoutSeconds < 1 || TimeoutSeconds > 300))
            throw new ArgumentException("TimeoutSeconds must be between 1 and 300");
    }
}
