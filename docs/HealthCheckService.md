# HealthCheckService

The `HealthCheckService` provides health‑checking capabilities for the API gateway, allowing callers to probe individual downstream targets, aggregate the status of all configured targets, and retrieve a snapshot of the gateway’s overall health.

## API

### `public async Task<bool> CheckTargetHealthAsync()`
- **Purpose**: Performs a health check against a single target (the target configureduring construction or via configuration).  
- **Parameters**: None.  
- **Return Value**: `true` if the target reports a healthy state; `false` otherwise.  
- **Exceptions**: May throw `HttpRequestException` or derived network‑related exceptions if the target cannot be reached; may throw `OperationCanceledException` if an internal timeout is triggered; may throw `ObjectDisposedException` if the service has been disposed.

### `public async Task<Dictionary<string, bool>> CheckAllTargetsAsync()`
- **Purpose**: Executes health checks against all registered targets and returns a mapping of target identifiers to their health status.  
- **Parameters**: None.  
- **Return Value**: A dictionary where each key is a target name (or identifier) and the value is `true` for healthy, `false` for unhealthy.  
- **Exceptions**: May throw `HttpRequestException` for any failed individual check; may throw `OperationCanceledException` on timeout; may throw `ObjectDisposedException` if the service is disposed.

### `public GatewayHealth GetGatewayHealth()`
- **Purpose**: Retrieves a snapshot of the gateway’s overall health, aggregating internal state such as uptime, version, and any additional details.  
- **Parameters**: None.  
- **Return Value**: An instance of `GatewayHealth` containing the current health summary.  
- **Exceptions**: May throw `ObjectDisposedException` if called after `Dispose`.

### `public void Dispose()`
- **Purpose**: Releases any unmanaged resources (e.g., HTTP clients, timers) used by the service.  
- **Parameters**: None.  
- **Return Value**: None.  
- **Exceptions**: None defined; calling `Dispose` multiple times is safe.

### `public bool IsHealthy { get; }`
- **Purpose**: Indicates whether the service currently considers the gateway healthy based on the most recent health check.  
- **Parameters**: None.  
- **Return Value**: `true` if healthy, `false` otherwise.  
- **Exceptions**: None.

### `public DateTime Timestamp { get; }`
- **Purpose**: The UTC time at which the last health evaluation was performed.  
- **Parameters**: None.  
- **Return Value**: A `DateTime` representing the evaluation timestamp.  
- **Exceptions**: None.

### `public TimeSpan Uptime { get; }`
- **Purpose**: The duration the gateway has been running since startup.  
- **Parameters**: None.  
- **Return Value**: A `TimeSpan` indicating elapsed time.  
- **Exceptions**: None.

### `public string Version { get; }`
- **Purpose**: The version identifier of the gateway (e.g., assembly informational version).  
- **Parameters**: None.  
- **Return Value**: A string representing the version.  
- **Exceptions**: None.

### `public Dictionary<string, object> Details { get; }`
- **Purpose**: Additional diagnostic information collected during the last health check (e.g., dependency versions, configuration flags).  
- **Parameters**: None.  
- **Return Value**: A read‑only dictionary where keys are detail names and values are arbitrary objects.  
- **Exceptions**: None.

## Usage

```csharp
using (var healthService = new HealthCheckService())
{
    // Check a single target
    bool targetOk = await healthService.CheckTargetHealthAsync();
    Console.WriteLine($"Target healthy: {targetOk}");

    // Check all targets and report failures
    var allResults = await healthService.CheckAllTargetsAsync();
    foreach (var kvp in allResults)
    {
        Console.WriteLine($"{kvp.Key}: {(kvp.Value ? "Healthy" : "Unhealthy")}");
    }

    // Retrieve a summary of gateway health
    GatewayHealth summary = healthService.GetGatewayHealth();
    Console.WriteLine($"Gateway version: {summary.Version}");
    Console.WriteLine($"Uptime: {summary.Uptime}");
}
```

```csharp
// Example of polling health at intervals without disposing prematurely
var healthService = new HealthCheckService();
try
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (healthService.IsHealthy)
        {
            // Perform work that requires a healthy gateway
        }
        else
        {
            // Log degraded state or trigger alerts
        }

        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
finally
{
    healthService.Dispose();
}
```

## Notes

- The service is **not thread‑safe** for concurrent modification; however, calling the asynchronous health‑check methods from multiple threads is safe as long as the instance is not disposed while operations are in progress.  
- Invoking `CheckTargetHealthAsync` or `CheckAllTargetsAsync` after `Dispose` will result in an `ObjectDisposedException`.  
- Property getters (`IsHealthy`, `Timestamp`, `Uptime`, `Version`, `Details`) return values captured at the moment of the most recent completed health check; they are not updated automatically and may become stale if no check has been performed since the last call.  
- The `Details` dictionary may contain `null` values or types that are not serializable; callers should perform null‑checking and type‑casting as appropriate for their scenario.  
- If a health check throws an exception, the corresponding target is considered unhealthy, and the exception propagates to the caller; the service’s internal state is left unchanged except for the timestamp of the attempt.  
- The `GatewayHealth` returned by `GetGatewayHealth` is a snapshot; modifications to the returned object do not affect the service’s internal state.
