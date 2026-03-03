#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;

namespace DotNetApiGateway.Models;

/// <summary>
/// Defines how requests are aggregated and conditionally fanned out.
/// </summary>
public sealed class AggregationPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool Enabled { get; set; } = false;
    public AggregationStrategy Strategy { get; set; } = AggregationStrategy.Parallel;
    public ConditionalAggregationTarget[] Targets { get; set; } = []; // List of conditional targets

    public void Validate()
    {
        if (Enabled)
        {
            if (Targets.Length == 0)
                throw new ArgumentException("AggregationPolicy must have at least one target if enabled.");

            foreach (var target in Targets)
                target.Validate();
        }
    }
}
