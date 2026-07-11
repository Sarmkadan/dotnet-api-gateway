# ConditionalAggregationTarget

Represents a target endpoint that can be conditionally included in an aggregation pipeline based on a JSON path expression evaluated against the upstream response.

## API

### Id
- **Type**: `string`
- **Purpose**: Unique identifier for the target within the aggregation configuration.
- **Remarks**: Should be non‑empty and unique among all targets; used for logging and debugging.
- **Throws**: Setting to `null` or whitespace does not throw, but validation will fail.

### UpstreamUrl
- **Type**: `string`
- **Purpose**: The HTTP endpoint that will be invoked for this target.
- **Remarks**: Must be a valid absolute or relative URL; the gateway will prepend the base address if needed.
- **Throws**: Validation will fail if `null`, empty, or not a well‑formed URL.

### JsonPathCondition
- **Type**: `string?`
- **Purpose**: Optional JSON Path expression used to decide whether the target should be included.
- **Remarks**: If `null` or empty, the target is always included. The expression is evaluated against the aggregated JSON payload prior to invoking the target.
- **Throws**: Validation will fail if the string is non‑null but not a valid JSON Path syntax.

### Method
- **Type**: `HttpMethod`
- **Purpose**: The HTTP verb to use when calling `UpstreamUrl`.
- **Remarks**: Common values are `GET`, `POST`, `PUT`, `DELETE`, `PATCH`. The gateway uses this to build the outgoing request.
- **Throws**: Validation will fail if the value is `null`.

### Headers
- **Type**: `Dictionary<string, string>?`
- **Purpose**: Optional collection of HTTP headers to send with the request.
- **Remarks**: Keys are header names; values are header values. If `null`, no custom headers are added. Header names are case‑insensitive per HTTP spec.
- **Throws**: Validation will fail if any key or value is `null` or empty string.

### Body
- **Type**: `string?`
- **Purpose**: Optional request body to send with `POST`, `PUT`, `PATCH` requests.
- **Remarks**: Typically a JSON string; ignored for methods that do not define a body (e.g., `GET`). If `null`, no body is sent.
- **Throws**: Validation will fail if the string is non‑null but exceeds the configured maximum payload size (checked elsewhere).

### TimeoutSeconds
- **Type**: `int`
- **Purpose**: Maximum time in seconds to wait for a response from `UpstreamUrl`.
- **Remarks**: Must be greater than zero. A typical value is `30`. Exceeding this timeout results in a `TaskCanceledException` from the underlying HTTP client.
- **Throws**: Validation will fail if the value is less than or equal to zero.

### Optional
- **Type**: `bool`
- **Purpose**: Indicates whether the target’s failure should be tolerated.
- **Remarks**: When `true`, errors invoking the target are logged but do not cause the overall aggregation to fail. When `false`, any error propagates upward.
- **Throws**: No validation associated; any `bool` value is acceptable.

### Validate
- **Signature**: `public void Validate()`
- **Purpose**: Verifies that all required members are correctly configured before the target is used.
- **Parameters**: None.
- **Return**: `void`.
- **Throws**:
  - `ArgumentException` if `Id` is null or whitespace.
  - `ArgumentException` if `UpstreamUrl` is null, empty, or not a valid URL.
  - `ArgumentException` if `Method` is null.
  - `ArgumentException` if `TimeoutSeconds` ≤ 0.
  - `ArgumentException` if `Headers` contains a null/empty key or value.
  - `ArgumentException` if `JsonPathCondition` is non‑null but not a valid JSON Path expression.
  - `InvalidOperationException` if the combination of `Method` and `Body` is invalid (e.g., body supplied for `GET`).

## Usage

```csharp
var target = new ConditionalAggregationTarget
{
    Id = "user-details",
    UpstreamUrl = "/api/users/{id}",
    Method = HttpMethod.Get,
    TimeoutSeconds = 10,
    Optional = false,
    Headers = new Dictionary<string, string>
    {
        ["Accept"] = "application/json"
    }
};

target.Validate(); // throws if configuration is invalid
```

```csharp
var conditionalTarget = new ConditionalAggregationTarget
{
    Id = "order-enrichment",
    UpstreamUrl = "https://inventory.service/orders",
    Method = HttpMethod.Post,
    Body = @"{ ""orderId"": ""${.orderId}"" }",
    JsonPathCondition = "$.orderStatus == 'Pending'",
    TimeoutSeconds = 15,
    Optional = true
};

conditionalTarget.Validate();
// The target will only be invoked when the aggregated JSON contains
// an orderStatus field equal to "Pending".
```

## Notes

- All mutable properties can be changed after construction; therefore the type is **not thread‑safe**. Concurrent modifications without external synchronization may lead to race conditions.
- Validation does **not** perform DNS resolution or actual HTTP calls; it only checks logical consistency of the configuration.
- When `Headers` is `null`, the gateway sends no custom headers; setting it to an empty dictionary yields the same result but is considered valid.
- The `Body` property is ignored for HTTP methods that do not permit a payload (e.g., `GET`, `HEAD`, `DELETE`). Supplying a body for such methods will cause `Validate` to throw an `InvalidOperationException`.
- `JsonPathCondition` evaluation occurs after all upstream responses have been merged into a single JSON document; malformed JSON at that point will cause the condition to treat the target as excluded rather than throwing.
- The `Optional` flag influences error handling only; it does not affect whether the request is made. A target marked optional will still be invoked unless `JsonPathCondition` excludes it.
