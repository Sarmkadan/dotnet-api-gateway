// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines caching behavior for route responses
/// </summary>
public class CachePolicy
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
        return Enabled &&
               CacheableStatusCodes.Contains(statusCode.ToString()) &&
               CacheableHttpMethods.Any(m => m.Equals(httpMethod, StringComparison.OrdinalIgnoreCase));
    }

    public string GenerateCacheKey(string path, string method, Dictionary<string, string> queryParams)
    {
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
