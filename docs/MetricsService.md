# MetricsService

The `MetricsService` collects, aggregates, and exposes telemetry data for HTTP requests processed by the API Gateway. It tracks request counts, response times, status codes, and per-route metrics to provide visibility into gateway performance and usage patterns.

## API

### `void RecordRequest(HttpRequestMessage request, HttpResponseMessage? response, TimeSpan duration)`
Records a completed request with its response and processing duration. The duration is measured from request arrival to response dispatch.
- **Parameters**
  - `request`: The incoming HTTP request message.
  - `response`: The outgoing HTTP response message, or `null` if the request failed before a response was generated.
  - `duration`: The time taken to process the request, in milliseconds.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentOutOfRangeException`: If `duration` is negative.

### `Task RecordRequestAsync(HttpRequestMessage request, HttpResponseMessage? response, TimeSpan duration)`
Asynchronously records a completed request with its response and processing duration. The duration is measured from request arrival to response dispatch.
- **Parameters**
  - `request`: The incoming HTTP request message.
  - `response`: The outgoing HTTP response message, or `null` if the request failed before a response was generated.
  - `duration`: The time taken to process the request, in milliseconds.
- **Returns**
  A `Task` representing the asynchronous operation.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentOutOfRangeException`: If `duration` is negative.

### `GatewayMetrics GetMetrics()`
Returns a snapshot of aggregated gateway-wide metrics, including counts, rates, and response times.
- **Returns**
  A `GatewayMetrics` object containing total requests, success/failure counts, success rate, average response time, uptime, start time, and status code distribution.

### `RouteMetrics? GetRouteMetrics(string routePattern)`
Returns metrics for a specific route pattern, or `null` if no requests have been recorded for that route.
- **Parameters**
  - `routePattern`: The route pattern (e.g., `"/users/{id}"`) to query.
- **Returns**
  A `RouteMetrics` object if the route exists; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If `routePattern` is `null` or empty.

### `Task<RouteMetrics?> GetRouteMetricsAsync(string routePattern)`
Asynchronously returns metrics for a specific route pattern, or `null` if no requests have been recorded for that route.
- **Parameters**
  - `routePattern`: The route pattern (e.g., `"/users/{id}"`) to query.
- **Returns**
  A `Task<RouteMetrics?>` resolving to a `RouteMetrics` object if the route exists; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If `routePattern` is `null` or empty.

### `Task<long> GetTotalRequestCountAsync()`
Asynchronously returns the total number of requests recorded since the service started.
- **Returns**
  A `Task<long>` resolving to the total request count.

### `Task<long> GetSuccessfulRequestCountAsync()`
Asynchronously returns the number of successful requests recorded since the service started.
- **Returns**
  A `Task<long>` resolving to the successful request count.

### `Task<long> GetFailedRequestCountAsync()`
Asynchronously returns the number of failed requests recorded since the service started.
- **Returns**
  A `Task<long>` resolving to the failed request count.

### `Task<double> GetAverageResponseTimeAsync()`
Asynchronously returns the average response time in milliseconds for all recorded requests.
- **Returns**
  A `Task<double>` resolving to the average response time, or `0` if no requests have been recorded.

### `void Reset()`
Resets all recorded metrics to zero and restarts the uptime timer. Useful for testing or resetting state between test runs.
- **Throws**
  - `InvalidOperationException`: If called concurrently with other operations that modify state.

### `long TotalRequests`
Gets the total number of requests recorded since the service started.
- **Type**
  `long`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `long SuccessfulRequests`
Gets the number of successful requests recorded since the service started.
- **Type**
  `long`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `long FailedRequests`
Gets the number of failed requests recorded since the service started.
- **Type**
  `long`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `double SuccessRate`
Gets the success rate as a value between 0.0 and 1.0.
- **Type**
  `double`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()` or when no requests have been recorded.

### `double AverageResponseTimeMs`
Gets the average response time in milliseconds for all recorded requests.
- **Type**
  `double`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()` or when no requests have been recorded.

### `TimeSpan Uptime`
Gets the duration since the service started.
- **Type**
  `TimeSpan`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `DateTime StartTime`
Gets the timestamp when the service started.
- **Type**
  `DateTime`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `Dictionary<int, long> StatusCodeDistribution`
Gets a dictionary mapping HTTP status codes to the number of responses with that code.
- **Type**
  `Dictionary<int, long>`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `List<RouteMetrics> RouteMetrics`
Gets a list of metrics for all recorded routes.
- **Type**
  `List<RouteMetrics>`
- **Throws**
  - `InvalidOperationException`: If accessed concurrently with `Reset()`.

### `double GetRequestsPerSecond(TimeSpan window)`
Calculates the average requests per second over the specified time window.
- **Parameters**
  - `window`: The duration over which to calculate the rate.
- **Returns**
  The average requests per second, or `0` if no requests fall within the window.
- **Throws**
  - `ArgumentOutOfRangeException`: If `window` is zero or negative.

## Usage
