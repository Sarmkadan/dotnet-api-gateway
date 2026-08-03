using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Benchmarks;

[MemoryDiagnoser]
public class RequestCoalescingPolicyBenchmarks
{
    private RequestCoalescingPolicy _policy = null!;
    private Dictionary<string, string> _queryParams = null!;

    [Params(10, 100, 1000)]
    public int QueryParamCount;

    [GlobalSetup]
    public void Setup()
    {
        _policy = new RequestCoalescingPolicy
        {
            Enabled = true,
            TimeoutMs = 5000,
            MaxQueuedRequests = 200,
            CoalescibleMethods = ["GET", "HEAD", "POST"],
            IncludeQueryString = true
        };

        _queryParams = new Dictionary<string, string>();
        for (int i = 0; i < QueryParamCount; i++)
        {
            _queryParams.Add($"key{i}", $"value{i}");
        }
    }

    [Benchmark]
    public void Validate()
    {
        _policy.Validate();
    }

    [Benchmark]
    public bool IsCoalescible()
    {
        return _policy.IsCoalescible("GET");
    }

    [Benchmark]
    public string GenerateCoalescingKey()
    {
        return _policy.GenerateCoalescingKey("/api/resource", "GET", _queryParams);
    }
}
