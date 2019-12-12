# AnalyticsService

Central service for collecting, aggregating, and reporting analytics data about gateway traffic, performance, and health. Provides real-time and historical insights into route usage patterns, error rates, response times, and system health.

## API

### `AnalyticsService`
Initializes a new instance of the analytics service. No parameters are required for construction.

### `async Task<GatewayHealthReport> GetHealthReportAsync()`
Retrieves a comprehensive health report of the gateway, including success and error rates, average response times, and overall system status.

- **Returns**: `Task<GatewayHealthReport>` containing health metrics and status.
- **Throws**: May throw if data collection is unavailable or corrupted.

### `async Task<PerformanceTrend> GetPerformanceTrendAsync()`
Fetches a trend analysis of gateway performance over time, including request volume, response times, and error trends.

- **Returns**: `Task<PerformanceTrend>` with historical performance data.
- **Throws**: May throw if historical data is inaccessible or incomplete.

### `async Task<List<RouteAnalytics>> GetTopRoutesByVolumeAsync()`
Returns a list of routes ordered by request volume, indicating the most frequently accessed endpoints.

- **Returns**: `Task<List<RouteAnalytics>>` sorted by descending request count.
- **Throws**: May throw if route analytics data is unavailable.

### `async Task<List<RouteAnalytics>> GetProblematicRoutesAsync()`
Identifies routes with elevated error rates or anomalies, useful for troubleshooting and prioritizing fixes.

- **Returns**: `Task<List<RouteAnalytics>>` filtered by error severity.
- **Throws**: May throw if error tracking is disabled or corrupted.

### `async Task<List<RouteAnalytics>> GetSlowestRoutesAsync()`
Lists routes ranked by average response time, highlighting performance bottlenecks.

- **Returns**: `Task<List<RouteAnalytics>>` sorted by descending average response time.
- **Throws**: May throw if timing data is missing or inconsistent.

### `DateTime Timestamp`
Gets the timestamp associated with the current analytics snapshot or report generation.

- **Type**: `DateTime`
- **Access**: Read-only

### `string HealthStatus`
Gets the overall health classification (e.g., "Healthy", "Degraded", "Critical") based on current metrics.

- **Type**: `string`
- **Access**: Read-only

### `double SuccessRate`
Gets the percentage of successful requests over the monitored period.

- **Type**: `double`
- **Access**: Read-only
- **Range**: 0.0 to 100.0

### `double ErrorRate`
Gets the percentage of failed requests over the monitored period.

- **Type**: `double`
- **Access**: Read-only
- **Range**: 0.0 to 100.0

### `long TotalRequests`
Gets the total number of requests processed during the monitored period.

- **Type**: `long`
- **Access**: Read-only

### `long SuccessfulRequests`
Gets the count of successfully processed requests.

- **Type**: `long`
- **Access**: Read-only

### `long FailedRequests`
Gets the count of failed requests.

- **Type**: `long`
- **Access**: Read-only

### `double AverageResponseTimeMs`
Gets the average response time in milliseconds across all requests.

- **Type**: `double`
- **Access**: Read-only

### `string Period`
Gets the time window or label used for the current analytics collection (e.g., "LastHour", "Today").

- **Type**: `string`
- **Access**: Read-only

### `DateTime CollectionTime`
Gets the time when the current analytics data was collected.

- **Type**: `DateTime`
- **Access**: Read-only

### `List<PerformanceSample> Samples`
Gets the raw performance measurement samples used to compute trends and averages.

- **Type**: `List<PerformanceSample>`
- **Access**: Read-only

## Usage
