# AggregationPolicy
The `AggregationPolicy` type is used to define a policy for aggregating data from multiple sources. It provides a way to specify the strategy for combining data, the targets for aggregation, and the conditions under which the aggregation should occur. This type is useful in scenarios where data needs to be collected and combined from multiple sources, such as in data warehousing, business intelligence, or real-time analytics applications.

## API
* `Id`: A unique identifier for the aggregation policy.
* `Enabled`: A boolean indicating whether the aggregation policy is enabled.
* `Strategy`: The aggregation strategy to use, represented by an instance of `AggregationStrategy`.
* `Targets`: An array of `ConditionalAggregationTarget` objects, specifying the targets for aggregation and the conditions under which the aggregation should occur.
* `Validate()`: Validates the aggregation policy, ensuring that it is correctly configured. This method does not return a value, but may throw an exception if the policy is invalid.

## Usage
The following examples demonstrate how to use the `AggregationPolicy` type:
```csharp
// Example 1: Creating a simple aggregation policy
AggregationPolicy policy = new AggregationPolicy
{
    Id = "MyPolicy",
    Enabled = true,
    Strategy = new AggregationStrategy(),
    Targets = new[]
    {
        new ConditionalAggregationTarget { Condition = "true", Target = "MyTarget" }
    }
};
policy.Validate();
```

```csharp
// Example 2: Creating a more complex aggregation policy with multiple targets
AggregationPolicy policy = new AggregationPolicy
{
    Id = "MyComplexPolicy",
    Enabled = true,
    Strategy = new AggregationStrategy(),
    Targets = new[]
    {
        new ConditionalAggregationTarget { Condition = "value > 10", Target = "MyTarget1" },
        new ConditionalAggregationTarget { Condition = "value <= 10", Target = "MyTarget2" }
    }
};
policy.Validate();
```

## Notes
When using the `AggregationPolicy` type, note that the `Validate()` method may throw an exception if the policy is invalid. It is recommended to handle this exception and provide meaningful error messages to the user. Additionally, the `AggregationPolicy` type is not thread-safe, and care should be taken to ensure that instances are not accessed concurrently by multiple threads. Edge cases, such as an empty `Targets` array or a null `Strategy`, should be carefully considered and handled accordingly to avoid unexpected behavior.
