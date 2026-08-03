using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Models;

namespace DotNetApiGateway.Benchmarks;

[MemoryDiagnoser]
public class TransformationRuleBenchmarks
{
    [Params(10, 100, 1000)]
    public int RuleCount { get; set; }

    private List<TransformationRule> _rules = new();

    [GlobalSetup]
    public void Setup()
    {
        _rules = new List<TransformationRule>();
        for (int i = 0; i < RuleCount; i++)
        {
            _rules.Add(new TransformationRule
            {
                Key = $"Key{i}",
                Value = $"Value{i}",
                Operation = TransformationOperation.AddHeader
            });
        }
    }

    [Benchmark]
    public void ValidateAll()
    {
        foreach (var rule in _rules)
        {
            rule.Validate();
        }
    }

    [Benchmark]
    public void CloneAll()
    {
        foreach (var rule in _rules)
        {
            rule.Clone();
        }
    }

    [Benchmark]
    public void CanApplyAll()
    {
        foreach (var rule in _rules)
        {
            rule.CanApply();
        }
    }
}
