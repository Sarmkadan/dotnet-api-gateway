# PerformanceAnalyzer

The `PerformanceAnalyzer` class provides a statistical utility for capturing, aggregating, and retrieving performance metrics within the `dotnet-api-gateway` project. It maintains an internal collection of measurement values, allowing developers to record individual data points and subsequently query aggregate statistics such as average, minimum, maximum, median, and specific percentiles. This component is designed to facilitate real-time monitoring and post-execution analysis of latency, throughput, or resource consumption without requiring external dependencies.

## API

### Methods

#### `RecordMeasurement`
Records a new performance value into the analyzer's dataset.
*   **Parameters**: Takes a single `long` value representing the measurement (e.g., ticks or milliseconds).
*   **Return Value**: `void`.
*   **Exceptions**: May throw an `ArgumentOutOfRangeException` if the provided value is negative, depending on internal validation logic.

#### `GetAverage`
Calculates the arithmetic mean of all recorded measurements.
*   **Parameters**: None.
*   **Return Value**: Returns a `double` representing the average.
*   **Exceptions**: Throws an `InvalidOperationException` if no measurements have been recorded (count is zero).

#### `GetMinimum`
Retrieves the smallest value currently stored in the dataset.
*   **Parameters**: None.
*   **Return Value**: Returns a `long` representing the minimum value.
*   **Exceptions**: Throws an `InvalidOperationException` if the dataset is empty.

#### `GetMaximum`
Retrieves the largest value currently stored in the dataset.
*   **Parameters**: None.
*   **Return Value**: Returns a `long` representing the maximum value.
*   **Exceptions**: Throws an `InvalidOperationException` if the dataset is empty.

#### `GetMedian`
Calculates the median value of the recorded measurements.
*   **Parameters**: None.
*   **Return Value**: Returns a `long` representing the median.
*   **Exceptions**: Throws an `InvalidOperationException` if the dataset is empty.

#### `GetPercentile95`
Calculates the 95th percentile value, indicating the threshold below which 95% of the observations fall.
*   **Parameters**: None.
*   **Return Value**: Returns a `long` representing the 95th percentile.
*   **Exceptions**: Throws an `InvalidOperationException` if the dataset is empty.

#### `GetPercentile`
Calculates a specific percentile value based on the provided rank.
*   **Parameters**: Takes a `double` or `int` (signature dependent on implementation context, typically `double percentile`) representing the desired percentile (0-100).
*   **Return Value**: Returns a `long` representing the calculated percentile value.
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if the percentile argument is outside the valid range (0-100) or an `InvalidOperationException` if the dataset is empty.

#### `GetCount`
Returns the total number of measurements currently recorded.
*   **Parameters**: None.
*   **Return Value**: Returns an `int`.
*   **Exceptions**: None.

#### `GetSummary`
Generates a comprehensive snapshot of all current statistics.
*   **Parameters**: None.
*   **Return Value**: Returns a `PerformanceSummary` object containing aggregated data (Count, Average, Min, Max, Median, etc.).
*   **Exceptions**: None (returns a summary with default/zero values if empty, or throws based on internal summary constructor logic).

#### `Clear`
Resets the analyzer by removing all recorded measurements.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: None.

#### `ToString`
Returns a string representation of the current state of the analyzer.
*   **Parameters**: None.
*   **Return Value**: Returns a `string` containing formatted statistical data.
*   **Exceptions**: None.

### Properties

#### `Count`
Gets the current number of recorded measurements.
*   **Type**: `int`
*   **Behavior**: Equivalent to calling `GetCount()`.

#### `Average`
Gets the arithmetic mean of the recorded measurements.
*   **Type**: `double`
*   **Behavior**: Equivalent to calling `GetAverage()`. Throws if empty.

#### `Minimum`
Gets the lowest recorded measurement.
*   **Type**: `long`
*   **Behavior**: Equivalent to calling `GetMinimum()`. Throws if empty.

#### `Maximum`
Gets the highest recorded measurement.
*   **Type**: `long`
*   **Behavior**: Equivalent to calling `GetMaximum()`. Throws if empty.

#### `Median`
Gets the median of the recorded measurements.
*   **Type**: `long`
*   **Behavior**: Equivalent to calling `GetMedian()`. Throws if empty.

#### `Percentile95`
Gets the 95th percentile of the recorded measurements.
*   **Type**: `long`
*   **Behavior**: Equivalent to calling `GetPercentile95()`. Throws if empty.

## Usage

### Example 1: Basic Latency Tracking
This example demonstrates recording request latencies and retrieving key statistics after a batch of operations.

```csharp
var analyzer = new PerformanceAnalyzer();

// Simulate recording latency in milliseconds for 5 requests
analyzer.RecordMeasurement(120);
analyzer.RecordMeasurement(145);
analyzer.RecordMeasurement(130);
analyzer.RecordMeasurement(200);
analyzer.RecordMeasurement(115);

// Retrieve specific metrics
long maxLatency = analyzer.GetMaximum();
double avgLatency = analyzer.GetAverage();
long p95 = analyzer.GetPercentile95();

Console.WriteLine($"Max: {maxLatency}ms, Avg: {avgLatency:F2}ms, P95: {p95}ms");

// Output a full summary string
Console.WriteLine(analyzer.ToString());
```

### Example 2: Sliding Window Simulation
This example shows how to use the `Clear` method to reset statistics for a new time window, ensuring old data does not skew new averages.

```csharp
var analyzer = new PerformanceAnalyzer();

// Record metrics for Window 1
for (int i = 0; i < 100; i++)
{
    analyzer.RecordMeasurement(50 + (i % 10));
}

var summary1 = analyzer.GetSummary();
Console.WriteLine($"Window 1 Count: {summary1.Count}");

// Reset for Window 2
analyzer.Clear();

// Record metrics for Window 2
for (int i = 0; i < 50; i++)
{
    analyzer.RecordMeasurement(200 + (i % 20));
}

// Access via properties
if (analyzer.Count > 0)
{
    Console.WriteLine($"Window 2 Median: {analyzer.Median}");
    Console.WriteLine($"Window 2 Min: {analyzer.Minimum}");
}
```

## Notes

*   **Empty State Behavior**: Accessing statistical properties (`Average`, `Minimum`, `Maximum`, `Median`, `Percentile95`) or calling their corresponding getter methods when `Count` is zero will result in an `InvalidOperationException`. Always verify `Count > 0` before querying these members in dynamic environments.
*   **Thread Safety**: The provided signatures do not imply internal synchronization. `RecordMeasurement` and `Clear` modify internal state, while getters read it. If `PerformanceAnalyzer` is accessed concurrently from multiple threads, external locking (e.g., using a `lock` statement) is required around calls to `RecordMeasurement`, `Clear`, and any subsequent read operations to prevent race conditions and data corruption.
*   **Data Precision**: The `Average` property and method return a `double` to preserve fractional precision, whereas input measurements and other statistical outputs (Min, Max, Median, Percentiles) are retained as `long`.
*   **Percentile Calculation**: The specific algorithm used for `GetPercentile` and `GetPercentile95` (e.g., nearest rank vs. linear interpolation) is encapsulated within the implementation. Results may vary slightly for small datasets depending on the underlying sorting and interpolation logic.
