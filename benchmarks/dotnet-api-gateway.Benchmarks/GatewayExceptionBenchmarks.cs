using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Exceptions;

namespace DotNetApiGateway.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="GatewayException"/> class.
/// The benchmarks focus on the cost of constructing the exception, constructing it
/// with an inner exception, creating many instances, and accessing its public
/// properties. These operations represent the typical usage patterns of the
/// exception within the gateway code base.
/// </summary>
[MemoryDiagnoser]
public class GatewayExceptionBenchmarks
{
    private string _message = string.Empty;
    private string _errorCode = string.Empty;
    private int _statusCode;
    private Exception _innerException = null!;

    /// <summary>
    /// Number of exceptions to create in the bulk benchmark.
    /// </summary>
    [Params(10, 100, 1000)]
    public int Count;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Prepare realistic test data.
        _message = "An unexpected gateway error occurred.";
        _errorCode = "GATEWAY_ERROR";
        _statusCode = 500;
        _innerException = new InvalidOperationException("Inner exception for testing.");
    }

    /// <summary>
    /// Benchmark creating a single <see cref="GatewayException"/> using the
    /// primary constructor (message, errorCode, statusCode).
    /// </summary>
    [Benchmark]
    public GatewayException CreateSingleException()
    {
        return new GatewayException(_message, _errorCode, _statusCode);
    }

    /// <summary>
    /// Benchmark creating a single <see cref="GatewayException"/> that includes
    /// an inner exception.
    /// </summary>
    [Benchmark]
    public GatewayException CreateExceptionWithInner()
    {
        return new GatewayException(_message, _innerException, _errorCode, _statusCode);
    }

    /// <summary>
    /// Benchmark creating <c>Count</c> exceptions in a tight loop. This measures
    /// allocation pressure and constructor overhead at scale.
    /// </summary>
    [Benchmark]
    public GatewayException[] CreateMultipleExceptions()
    {
        var exceptions = new GatewayException[Count];
        for (int i = 0; i < Count; i++)
        {
            exceptions[i] = new GatewayException(
                message: $"{_message} #{i}",
                errorCode: _errorCode,
                statusCode: _statusCode);
        }

        return exceptions;
    }

    /// <summary>
    /// Benchmark accessing the public properties after construction. While
    /// property access is cheap, this ensures the JIT does not inline away the
    /// constructor work when the properties are used.
    /// </summary>
    [Benchmark]
    public (string errorCode, int statusCode) AccessProperties()
    {
        var ex = new GatewayException(_message, _errorCode, _statusCode);
        return (ex.ErrorCode, ex.StatusCode);
    }
}
