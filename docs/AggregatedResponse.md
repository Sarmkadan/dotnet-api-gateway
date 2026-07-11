# AggregatedResponse

Represents a collection of responses aggregated from multiple downstream services or endpoints, typically used in API gateway scenarios to consolidate results, track performance metrics, and determine overall success or failure of a distributed operation.

## API

### `public string Id`
A unique identifier for the aggregated response instance.

### `public Dictionary<string, AggregatedResponseData> Responses`
A dictionary mapping string keys to individual `AggregatedResponseData` instances, representing the responses from each aggregated source.

### `public DateTime AggregatedAt`
The timestamp when the aggregation process completed.

### `public TimeSpan TotalDuration`
The total elapsed time taken to receive all aggregated responses.

### `public int SuccessCount`
The number of responses that completed successfully.

### `public int FailureCount`
The number of responses that failed during processing.

### `public void AddResponse(string key, AggregatedResponseData data)`
Adds a response to the aggregation.  
**Parameters**:  
- `key` (`string`): The unique key identifying the response source.  
- `data` (`AggregatedResponseData`): The response data to add.  
**Throws**:  
- `ArgumentNullException`: If `data` is null.  
- `ArgumentException`: If `key` is null or empty.

### `public AggregatedResponseData? GetResponse(string key)`
Retrieves the response data associated with the specified key.  
**Parameters**:  
- `key` (`string`): The key of the response to retrieve.  
**Returns**:  
- The `AggregatedResponseData` instance if found; otherwise, `null`.

### `public bool IsSuccessful`
Indicates whether all aggregated responses were successful. Returns `true` if `FailureCount` is zero and `SuccessCount` is greater than zero.

### `public double GetAverageResponseTime()`
Calculates the average response time across all aggregated responses.  
**Returns**:  
- The average duration in milliseconds as a `double`.

### `public string Alias`
An optional alias or friendly name for the aggregated response.

### `public int StatusCode`
The HTTP status code representing the overall status of the aggregated response.

### `public string? Body`
The combined or representative response body content, if applicable.

### `public Dictionary<string, string> Headers`
A dictionary of headers associated with the aggregated response.

### `public TimeSpan Duration`
The duration of the entire aggregation operation.

### `public DateTime ReceivedAt`
The timestamp when the first response was received.

### `public string? Error`
An error message if the aggregation encountered a critical failure.

## Usage

```csharp
// Example 1: Aggregating responses from multiple services
var aggregated = new AggregatedResponse
{
    Id = Guid.NewGuid().ToString(),
    Alias = "User Profile Aggregation"
};

aggregated.AddResponse("service-a", new AggregatedResponseData
{
    StatusCode = 200,
    Body = "{\"name\": \"John\"}",
    Duration = TimeSpan.FromMilliseconds(150)
});

aggregated.AddResponse("service-b", new AggregatedResponseData
{
    StatusCode = 500,
    Error = "Internal Server Error",
    Duration = TimeSpan.FromMilliseconds(300)
});

Console.WriteLine($"Success: {aggregated.IsSuccessful}"); // Output: False
Console.WriteLine($"Average Time: {aggregated.GetAverageResponseTime()}ms"); // Output: 225ms
```

```csharp
// Example 2: Retrieving individual responses and handling errors
var response = aggregated.GetResponse("service-a");
if (response != null)
{
    Console.WriteLine($"Service A Status: {response.StatusCode}");
    Console.WriteLine($"Received at: {response.ReceivedAt}");
}

if (!string.IsNullOrEmpty(aggregated.Error))
{
    Console.WriteLine($"Aggregation failed: {aggregated.Error}");
}
```

## Notes

- **Thread Safety**: The `Responses` dictionary and counters (`SuccessCount`, `FailureCount`) are not thread-safe. Concurrent modifications or reads may lead to race conditions or inconsistent state. External synchronization is required for multi-threaded access.
- **Edge Cases**:  
  - `AddResponse` will throw if `data` is null or `key` is invalid.  
  - `GetResponse` returns `null` for non-existent keys without throwing.  
  - `GetAverageResponseTime` may return zero if no responses are present.  
  - `IsSuccessful` returns `false` if no responses are added or all responses fail.  
- **Immutability**: Properties like `Id`, `AggregatedAt`, and `TotalDuration` are set during initialization or aggregation and should not be modified afterward.
