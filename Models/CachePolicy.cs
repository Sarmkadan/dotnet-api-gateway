#nullable enable
using System.Linq;

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines caching behavior for route responses
/// </summary>
public sealed class CachePolicy : IEquatable<CachePolicy>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool Enabled { get; set; } = false;
    public int DurationSeconds { get; set; } = 300;
    public CacheStrategy Strategy { get; set; } = CacheStrategy.CacheControl;
    public string[] CacheableStatusCodes { get; set; } = ["200"];
    public string[] CacheableHttpMethods { get; set; } = ["GET", "HEAD"];
    public bool VaryByQueryString { get; set; } = true;
    public bool VaryByHeaders { get; set; } = false;
    public string[] VaryHeaders { get; set; } = [];
    public int MaxEntriesInCache { get; set; } = 1000;

    public bool Equals(CachePolicy? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id == other.Id &&
               Enabled == other.Enabled &&
               DurationSeconds == other.DurationSeconds &&
               Strategy == other.Strategy &&
               CacheableStatusCodes.SequenceEqual(other.CacheableStatusCodes) &&
               CacheableHttpMethods.SequenceEqual(other.CacheableHttpMethods) &&
               VaryByQueryString == other.VaryByQueryString &&
               VaryByHeaders == other.VaryByHeaders;
    }

    public override bool Equals(object? obj) => Equals(obj as CachePolicy);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Enabled);
        hash.Add(DurationSeconds);
        hash.Add(Strategy);
        foreach (var code in CacheableStatusCodes) hash.Add(code);
        foreach (var method in CacheableHttpMethods) hash.Add(method);
        hash.Add(VaryByQueryString);
        hash.Add(VaryByHeaders);
        return hash.ToHashCode();
    }

    public static bool operator ==(CachePolicy? left, CachePolicy? right) =>
        Equals(left, right);

    public static bool operator !=(CachePolicy? left, CachePolicy? right) =>
        !Equals(left, right);

    public void Validate()
    {
        if (DurationSeconds < 1 || DurationSeconds > 3600)
            throw new ArgumentException("DurationSeconds must be between 1 and 3600");

        if (CacheableStatusCodes.Length == 0)
            throw new ArgumentException("At least one cacheable status code is required");

        if (CacheableHttpMethods.Length == 0)
            throw new ArgumentException("At least one cacheable HTTP method is required");

        if (MaxEntriesInCache < 1 || MaxEntriesInCache > 10000)
            throw new ArgumentException("MaxEntriesInCache must be between 1 and 10000");
    }

    public bool IsCacheable(int statusCode, string httpMethod)
    {
        ArgumentException.ThrowIfNullOrEmpty(httpMethod);
        return Enabled &&
               CacheableStatusCodes.Contains(statusCode.ToString()) &&
               CacheableHttpMethods.Any(m => m.Equals(httpMethod, StringComparison.OrdinalIgnoreCase));
    }

    public string GenerateCacheKey(string path, string method, Dictionary<string, string> queryParams)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(queryParams);

        var key = $"{method}:{path}";

        if (VaryByQueryString && queryParams.Count > 0)
        {
            var sortedParams = string.Join("&",
                queryParams.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
            key += $"?{sortedParams}";
        }

        return key;
    }
}
