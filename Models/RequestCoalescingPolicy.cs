// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines coalescing behaviour for duplicate concurrent requests.
/// When multiple identical requests arrive simultaneously, coalescing ensures
/// only one upstream call is made and the result is shared with all waiters.
/// </summary>
public class RequestCoalescingPolicy
{
    /// <summary>Gets or sets the unique policy identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets whether request coalescing is active for this policy.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum milliseconds a follower request will wait to join
    /// an in-flight coalesced request before falling back to an independent execution.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the maximum number of follower requests allowed to queue behind
    /// a single in-flight leader request. Requests beyond this limit execute independently.
    /// </summary>
    public int MaxQueuedRequests { get; set; } = 200;

    /// <summary>
    /// Gets or sets the HTTP methods eligible for coalescing.
    /// Only idempotent methods should be coalesced — GET and HEAD by default.
    /// </summary>
    public string[] CoalescibleMethods { get; set; } = ["GET", "HEAD"];

    /// <summary>
    /// Gets or sets whether query-string parameters are included when computing
    /// the coalescing key. Disable only if query parameters do not affect the response.
    /// </summary>
    public bool IncludeQueryString { get; set; } = true;

    /// <summary>
    /// Validates the policy configuration and throws on out-of-range values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any property value is invalid.</exception>
    public void Validate()
    {
        if (TimeoutMs < 100 || TimeoutMs > 30_000)
            throw new ArgumentException("TimeoutMs must be between 100 and 30000");

        if (MaxQueuedRequests < 1 || MaxQueuedRequests > 10_000)
            throw new ArgumentException("MaxQueuedRequests must be between 1 and 10000");

        if (CoalescibleMethods.Length == 0)
            throw new ArgumentException("At least one CoalescibleMethod is required");
    }

    /// <summary>
    /// Returns <see langword="true"/> when the policy is enabled and the HTTP method
    /// is listed in <see cref="CoalescibleMethods"/>.
    /// </summary>
    /// <param name="httpMethod">The HTTP method string, e.g. <c>"GET"</c>.</param>
    public bool IsCoalescible(string httpMethod) =>
        Enabled &&
        CoalescibleMethods.Any(m => m.Equals(httpMethod, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Produces a deterministic string key that uniquely identifies a logical request.
    /// Requests sharing the same key will be coalesced.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="queryParams">The parsed query-string parameters.</param>
    /// <returns>A coalescing key of the form <c>METHOD:path[?sorted-query]</c>.</returns>
    public string GenerateCoalescingKey(string path, string method, Dictionary<string, string> queryParams)
    {
        var key = $"{method.ToUpperInvariant()}:{path}";

        if (IncludeQueryString && queryParams.Count > 0)
        {
            var sorted = string.Join("&",
                queryParams.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
            key += $"?{sorted}";
        }

        return key;
    }
}
