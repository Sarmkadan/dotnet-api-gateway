using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Exceptions;

namespace DotNetApiGateway.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="RateLimitExceededException"/> class.
/// The benchmarks focus on the cost of constructing the exception, which is the
/// primary operation performed by this type. Creating many instances in a loop
/// provides a realistic workload for measuring allocation and execution time.
/// </summary>
[MemoryDiagnoser]
public class RateLimitExceededExceptionBenchmarks
{
    private string _baseClientId = "client-";
    private int _limitPerMinute = 60;
    private long _remainingSeconds = 30;

    /// <summary>
    /// Number of exceptions to create in the bulk benchmark.
    /// </summary>
    [Params(10, 100, 1000)]
    public int Count;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // No heavy setup required; just ensure the base values are ready.
        // The values are deliberately simple to keep the benchmark focused on
        // the constructor overhead.
    }

    /// <summary>
    /// Benchmark creating a single <see cref="RateLimitExceededException"/>.
    /// </summary>
    [Benchmark]
    public RateLimitExceededException CreateSingleException()
    {
        return new RateLimitExceededException(
            clientId: $"{_baseClientId}single",
            limitPerMinute: _limitPerMinute,
            remainingSeconds: _remainingSeconds);
    }

    /// <summary>
    /// Benchmark creating <c>Count</c> exceptions in a tight loop.
    /// This measures allocation pressure and constructor cost at scale.
    /// </summary>
    [Benchmark]
    public RateLimitExceededException[] CreateMultipleExceptions()
    {
        var exceptions = new RateLimitExceededException[Count];
        for (int i = 0; i < Count; i++)
        {
            exceptions[i] = new RateLimitExceededException(
                clientId: $"{_baseClientId}{i}",
                limitPerMinute: _limitPerMinute,
                remainingSeconds: _remainingSeconds);
        }

        return exceptions;
    }

    /// <summary>
    /// Benchmark accessing the public properties after construction.
    /// While property access is cheap, this ensures the JIT does not inline
    /// away the constructor work when the properties are used.
    /// </summary>
    [Benchmark]
    public (string clientId, int limit, long remaining) AccessProperties()
    {
        var ex = new RateLimitExceededException(
            clientId: $"{_baseClientId}props",
            limitPerMinute: _limitPerMinute,
            remainingSeconds: _remainingSeconds);

        // Access each property once.
        return (ex.ClientId, ex.LimitPerMinute, ex.RemainingSeconds);
    }
}
