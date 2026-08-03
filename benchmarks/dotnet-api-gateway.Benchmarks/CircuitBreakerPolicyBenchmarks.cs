using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="CircuitBreakerPolicy"/> class.
/// The benchmarks focus on the public methods that contain logic:
/// <list type="bullet">
///   <item><description>Validate()</description></item>
///   <item><description>IsFailureStatus(int)</description></item>
///   <item><description>IsEnabled()</description></item>
///   <item><description>Creating many instances</description></item>
/// </list>
/// </summary>
[MemoryDiagnoser]
public class CircuitBreakerPolicyBenchmarks
{
    private CircuitBreakerPolicy _policy = null!;

    // Used for the IsFailureStatus benchmark.
    // 500 is in the default FailureStatusCodes array, 404 is not.
    [Params(500, 404)]
    public int StatusCode { get; set; }

    // Used for the CreateInstances benchmark.
    [Params(10, 100, 1000)]
    public int InstanceCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Initialise a default policy that will be reused for the method benchmarks.
        _policy = new CircuitBreakerPolicy
        {
            // The defaults are already set in the class, but we explicitly assign them
            // to make the intent clear and to avoid any future changes affecting the benchmark.
            FailureThreshold = 5,
            SuccessThreshold = 2,
            TimeoutSeconds = 60,
            FailureStatusCodes = new[] { 500, 502, 503, 504 },
            Enabled = true,
            MaxRetries = 3,
            RetryDelayMilliseconds = 100
        };
    }

    /// <summary>
    /// Benchmark the validation logic of a <see cref="CircuitBreakerPolicy"/>.
    /// This method checks the range constraints of the policy properties.
    /// </summary>
    [Benchmark]
    public void ValidatePolicy()
    {
        _policy.Validate();
    }

    /// <summary>
    /// Benchmark checking whether a status code is considered a failure.
    /// </summary>
    [Benchmark]
    public bool IsFailureStatus()
    {
        return _policy.IsFailureStatus(StatusCode);
    }

    /// <summary>
    /// Benchmark the simple enabled‑check accessor.
    /// </summary>
    [Benchmark]
    public bool IsEnabled()
    {
        return _policy.IsEnabled();
    }

    /// <summary>
    /// Benchmark creating many <see cref="CircuitBreakerPolicy"/> instances.
    /// This measures allocation pressure and constructor overhead at scale.
    /// </summary>
    [Benchmark]
    public CircuitBreakerPolicy[] CreateInstances()
    {
        var policies = new CircuitBreakerPolicy[InstanceCount];
        for (int i = 0; i < InstanceCount; i++)
        {
            policies[i] = new CircuitBreakerPolicy();
        }

        return policies;
    }
}
