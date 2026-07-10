# AggregatedRequest

`AggregatedRequest` represents a single sub-request within an aggregated API gateway call. It encapsulates the details needed to issue an HTTP request to a downstream service, including the target path, HTTP method, headers, query parameters, body, and timeout. The `Optional` flag allows the gateway to tolerate failures for this request without failing the entire aggregation. The `Validate` method ensures that the request is well-formed before it is dispatched.

## API

### `public string Id`

A unique identifier for this aggregated request. Used for correlation and logging. Must not be null or empty when the request is validated.

### `public string Alias`

A human-readable name or label for the request. Typically used in error messages or for debugging. Can be null or empty; validation behavior depends on the implementation.

### `public string Path`

The relative URL path of the downstream endpoint (e.g., `/api/users`). Must not be null or empty when validated.

### `public string Method`

The HTTP method to use (e.g., `GET`, `POST`, `PUT`, `DELETE`). Validation typically checks that the value is a known HTTP method. Case-insensitive comparison is recommended.

### `public Dictionary<string, string>? Headers`

Optional collection of HTTP headers to include in the downstream request. Keys are header names, values are header values. If null, no custom headers are added.

### `public Dictionary<string, string>? QueryParameters`

Optional collection of query string parameters. Keys are parameter names, values are parameter values. If null, no query parameters are appended.

### `public string? Body`

Optional request body content, typically used with methods like `POST` or `PUT`. Can be null for methods that do not require a body.

### `public int? TimeoutSeconds`

Optional timeout in seconds for the downstream request. If null, a default timeout (defined elsewhere) is used. Must be a positive integer when set; validation may reject zero or negative values.

### `public bool Optional`

When `true`, the gateway will not treat a failure of this request as a fatal error for the overall aggregation. The aggregated response may still succeed even if this request fails.

### `public void Validate`

Validates the current state of the `AggregatedRequest`. Throws an exception if any required fields are missing or invalid (e.g., null or empty `Id` or `Path`, unsupported HTTP method, non-positive `TimeoutSeconds`). The exact exception type is implementation-defined (commonly `InvalidOperationException` or `ArgumentException`). This method does not return a value.

## Usage

### Example 1: Creating and validating a simple GET request

```csharp
var request = new AggregatedRequest
{
    Id = Guid.NewGuid().ToString(),
    Alias = "GetUser",
    Path = "/api/users/42",
    Method = "GET",
    Optional = false,
    TimeoutSeconds = 5
};

request.Validate(); // Throws if any required field is invalid
```

### Example 2: Building a POST request with headers and query parameters

```csharp
var request = new AggregatedRequest
{
    Id = "req-001",
    Alias = "CreateOrder",
    Path = "/orders",
    Method = "POST",
    Headers = new Dictionary<string, string>
    {
        ["Content-Type"] = "application/json",
        ["Authorization"] = "Bearer token123"
    },
    QueryParameters = new Dictionary<string, string>
    {
        ["source"] = "web"
    },
    Body = "{\"productId\": 99, \"quantity\": 2}",
    TimeoutSeconds = 10,
    Optional = true
};

request.Validate();
```

## Notes

- **Validation**: The `Validate` method should be called before the request is dispatched. It checks that `Id` and `Path` are not null or empty, that `Method` is a recognized HTTP verb, and that `TimeoutSeconds` (if set) is greater than zero. The exact validation rules are implementation-specific.
- **Case sensitivity**: The `Method` property is typically compared case-insensitively. The `Headers` dictionary keys are usually treated as case-insensitive per HTTP standards, but this depends on the consuming code.
- **Null vs. empty collections**: A null `Headers` or `QueryParameters` is treated as “no custom values”. An empty dictionary is functionally equivalent but may have different serialization behavior.
- **Thread safety**: `AggregatedRequest` is not thread-safe. Its properties are mutable and intended to be set before validation and dispatch. Concurrent reads and writes from multiple threads can lead to inconsistent state. If shared across threads, external synchronization is required.
- **Optional flag**: When `Optional` is `true`, the gateway may still log the failure but will not propagate it to the aggregated response. This is useful for non-critical data enrichment calls.
- **TimeoutSeconds**: A null value means the gateway’s default timeout is applied. Setting a very large value may cause resource exhaustion; the gateway may enforce an upper bound.
