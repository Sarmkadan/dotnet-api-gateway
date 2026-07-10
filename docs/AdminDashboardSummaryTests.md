# AdminDashboardSummaryTests

The `AdminDashboardSummaryTests` class contains unit tests for validating the functionality of the admin dashboard summary metrics in the `dotnet-api-gateway` project. These tests ensure that the metrics collection, aggregation, and reporting logic—such as request counts, success rates, status code distributions, and per-route averages—behave as expected under various conditions.

## API

### `GetMetrics_AfterRequests_ReportsCorrectCounts`
**Purpose**: Verifies that the metrics system accurately reports the total number of requests processed after multiple API calls.
**Parameters**: None.
**Return Value**: None.
**Throws**: Fails the test if the reported request count does not match the expected value.

### `GetMetrics_NoRequests_ReturnsZeroSuccessRate`
**Purpose**: Ensures that the success rate metric returns `0` when no requests have been processed, preventing division-by-zero or incorrect default values.
**Parameters**: None.
**Return Value**: None.
**Throws**: Fails the test if the success rate is not `0` in the absence of requests.

### `GetMetrics_StatusCodeDistribution_TracksEachCode`
**Purpose**: Confirms that the metrics system correctly tracks and aggregates HTTP status codes across multiple requests, including edge cases like no requests or multiple occurrences of the same code.
**Parameters**: None.
**Return Value**: None.
**Throws**: Fails the test if the status code distribution does not match the expected counts.

### `GetMetrics_RouteMetrics_CalculatesPerRouteAverage`
**Purpose**: Validates that the metrics system computes the average response time (or other per-route metrics) for each route individually, ensuring isolation between routes.
**Parameters**: None.
**Return Value**: None.
**Throws**: Fails the test if the per-route averages are incorrect or if metrics leak between routes.

## Usage

### Example 1: Testing Request Counts and Success Rates
