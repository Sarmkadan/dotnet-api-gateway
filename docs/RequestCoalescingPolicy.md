# RequestCoalescingPolicy

A configuration class that defines rules for coalescing (grouping) multiple identical HTTP requests into a single request to reduce backend load. It controls which requests can be coalesced, how long to wait for additional requests, and how to uniquely identify coalescible requests.

## API

### `Id`
A unique identifier for the coalescing policy instance. Used for logging and policy lookup.

### `Enabled`
Determines whether the coalescing policy is active. When `false`, all coalescing behavior is bypassed.

### `TimeoutMs`
The maximum time in milliseconds to wait for additional requests to coalesce before executing the first request. Must be a non-negative value.

### `MaxQueuedRequests`
The maximum number of requests that can be queued for coalescing. Requests exceeding this limit are processed immediately without coalescing. Must be a non-negative value.

### `CoalescibleMethods`
An array of HTTP methods (e.g., `GET`, `POST`) that are eligible for coalescing. Requests using other methods are processed immediately.

### `IncludeQueryString`
Indicates whether the query string should be included when generating the coalescing key. When `true`, requests with different query strings are treated as distinct; when `false`, query strings are ignored.

### `Validate()`
Validates the policy configuration. Throws an exception if any required property is invalid (e.g., `TimeoutMs` is negative or `MaxQueuedRequests` is negative).

### `IsCoalescible(HttpRequestMessage request)`
Determines whether the given HTTP request can be coalesced based on the policy rules.
- **Parameters**: `request` – The HTTP request to evaluate.
- **Returns**: `true` if the request is coalescible; otherwise, `false`.

### `GenerateCoalescingKey(HttpRequestMessage request)`
Generates a unique key for the request to determine if it can be coalesced with others.
- **Parameters**: `request` – The HTTP request for which to generate the key.
- **Returns**: A string key representing the coalescible request. Identical requests produce the same key.

## Usage

### Example 1: Basic Configuration
