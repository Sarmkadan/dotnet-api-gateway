# RouteTarget

Represents a single upstream service endpoint within the API gateway routing table. Each `RouteTarget` encapsulates the connection details, health state, and forwarding behaviour for a backend service instance. The class is used by the gateway's routing engine to select healthy targets and to construct the final forward URL.

## API

### Properties

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | Unique identifier for this target (e.g., a GUID or a human-readable key). |
| `Name` | `string` | Human-readable name of the target service. |
| `BaseUrl` | `string` | Base URL of the upstream service (scheme and host, e.g., `https://api.example.com`). |
| `Port` | `int?` | Optional port override. When `null`, the port is inferred from the scheme in `BaseUrl`. |
| `TimeoutSeconds` | `int?` | Optional request timeout in seconds. When `null`, a system-wide default is used. |
| `Weight` | `int` | Load‑balancing weight. Higher values receive proportionally more traffic. Must be non‑negative. |
| `IsHealthy` | `bool` | Indicates whether the target is currently considered healthy. Updated by `UpdateHealthStatus`. |
| `HealthCheckPath` | `string?` | Optional path segment appended to `BaseUrl` for health checks (e.g., `/health`). When `null`, no health check is performed. |
| `HealthCheckIntervalSeconds` | `int` | Interval between health checks, in seconds. Ignored when `HealthCheckPath` is `null`. |
| `TransformHeaders` | `Dictionary<string, string>` | Headers to add, remove, or override when forwarding requests to this target. Keys are header names, values are header values. An empty dictionary means no transformation. |
| `StripPathPrefix` | `bool` | When `true`, the matched route prefix is removed from the request path before forwarding. |
| `LastHealthCheckAt` | `DateTime` | Timestamp of the most recent health check attempt. Defaults to `DateTime.MinValue` if never checked. |
| `LastHealthCheckError` | `string?` | Error message from the last failed health check, or `null` if the last check succeeded or no check has been performed. |

### Methods

#### `void Validate()`

Validates the current configuration of the target. Throws an `InvalidOperationException` if any required field is missing or invalid (e.g., `Id` is null or empty, `BaseUrl` is not a valid absolute URI, `Weight` is negative). Does not modify the instance.

**Parameters:** None  
**Returns:** Nothing  
**Throws:** `InvalidOperationException` – when validation fails.

#### `string GetForwardUrl()`

Constructs the full forward URL for a request to this target. Combines `BaseUrl`, optional `Port`, and the request path (after applying `StripPathPrefix` logic). The request path is provided externally by the gateway's routing context; this method is typically called with an implicit path argument from the pipeline.

**Parameters:** None (the request path is obtained from the current HTTP context or a stored field – not exposed in this API).  
**Returns:** A `string` containing the absolute URL to forward to.  
**Throws:** `InvalidOperationException` – if `BaseUrl` is not set or the constructed URL is invalid.

#### `void UpdateHealthStatus()`

Triggers a health check against the target using `HealthCheckPath` and `BaseUrl`. Updates `IsHealthy`, `LastHealthCheckAt`, and `LastHealthCheckError` based on the result. If `HealthCheckPath` is `null`, the method does nothing.

**Parameters:** None  
**Returns:** Nothing  
**Throws:** Not expected to throw under normal operation; network errors are captured in `LastHealthCheckError` and `IsHealthy` is set to `false`.

## Usage

### Example 1: Configuring and validating a target

```csharp
var target = new RouteTarget
{
    Id = "svc-orders-v1",
    Name = "Orders Service v1",
    BaseUrl = "https://orders.internal:8443",
    Port = null,               // use default from scheme
    TimeoutSeconds = 30,
    Weight = 10,
    HealthCheckPath = "/health",
    HealthCheckIntervalSeconds = 15,
    TransformHeaders = new Dictionary<string, string>
    {
        ["X-Forwarded-For"] = "true"
    },
    StripPathPrefix = true
};

try
{
    target.Validate();
    Console.WriteLine("Target configuration is valid.");
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Validation failed: {ex.Message}");
}
```

### Example 2: Health check and forwarding

```csharp
// Assume 'target' is already configured and validated.
target.UpdateHealthStatus();

if (target.IsHealthy)
{
    string forwardUrl = target.GetForwardUrl();
    Console.WriteLine($"Forwarding request to: {forwardUrl}");
}
else
{
    Console.WriteLine($"Target is unhealthy. Last error: {target.LastHealthCheckError}");
}
```

## Notes

- **Nullable properties:** `Port`, `TimeoutSeconds`, `HealthCheckPath`, and `LastHealthCheckError` may be `null`. Code consuming these properties should handle the `null` case appropriately (e.g., using the null‑conditional operator or providing defaults).
- **Default values:** `Weight` defaults to `0` (meaning no traffic is routed unless explicitly set). `HealthCheckIntervalSeconds` defaults to `0` (no interval). `IsHealthy` defaults to `false`. `LastHealthCheckAt` defaults to `DateTime.MinValue`.
- **Validation:** `Validate()` should be called after all property assignments and before the target is added to the routing table. It does not check runtime state such as network reachability.
- **Thread safety:** This type is not thread‑safe. Concurrent reads and writes to properties (especially `IsHealthy`, `LastHealthCheckAt`, `LastHealthCheckError`) from multiple threads may result in inconsistent state. External synchronization (e.g., a lock or `ConcurrentDictionary` of targets) is required when the same `RouteTarget` instance is accessed from the gateway’s health‑check loop and request‑forwarding pipeline.
- **`GetForwardUrl()`** relies on the current HTTP request context; calling it outside of an active request (e.g., in a background thread) may produce incorrect results or throw. The method is designed for use within the gateway’s middleware pipeline.
- **`UpdateHealthStatus()`** performs an HTTP call synchronously. In high‑throughput scenarios, consider offloading health checks to a dedicated background service to avoid blocking request processing.
