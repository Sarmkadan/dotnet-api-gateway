# RequestAggregationService

The `RequestAggregationService` aggregates multiple HTTP requests into a single response, consolidating headers, status codes, and response bodies from multiple upstream services. It is designed to simplify client-side handling of distributed requests by providing a unified response structure.

## API

### `RequestAggregationService`

The default constructor initializes a new instance of the `RequestAggregationService` with default configuration.

### `async Task<AggregatedResponse> AggregateAsync`

Asynchronously aggregates multiple HTTP requests into a single response.

- **Returns**: A `Task<AggregatedResponse>` representing the aggregated response containing the combined status code, body, headers, and duration.
- **Throws**:
  - `ArgumentNullException` if any request in the aggregation is `null`.
  - `InvalidOperationException` if no requests are provided for aggregation.

### `StatusCode`

Gets the HTTP status code of the aggregated response.

- **Type**: `int`
- **Remarks**: Represents the highest status code among aggregated responses, following HTTP status code precedence rules.

### `Body`

Gets the response body of the aggregated response.

- **Type**: `string?`
- **Remarks**: Contains the concatenated or merged body content from all aggregated responses. May be `null` if no valid body content exists.

### `Headers`

Gets the headers of the aggregated response.

- **Type**: `Dictionary<string, string>`
- **Remarks**: Contains the merged headers from all aggregated responses. Duplicate header names are handled according to HTTP header merging rules.

### `Duration`

Gets the total duration of the aggregation operation.

- **Type**: `TimeSpan`
- **Remarks**: Represents the elapsed time from the start of aggregation to the completion of all requests.

## Usage

### Example 1: Basic Aggregation
