# ApiVersioningService

The `ApiVersioningService` is a core utility within the `dotnet-api-gateway` project responsible for managing HTTP API version resolution and path normalization. It provides mechanisms to detect the requested API version from incoming requests, strip version identifiers from request paths to facilitate routing to version-agnostic controllers, and generate standardized error responses when version negotiation fails.

## API

### `public ApiVersioningService`
Initializes a new instance of the `ApiVersioningService` class. This constructor sets up the internal state required for version parsing and path manipulation. It does not accept parameters and does not throw exceptions under normal operating conditions.

### `public bool TryResolveVersion`
Attempts to resolve the API version from the current execution context (typically derived from the request path or headers).
*   **Parameters**: None explicitly listed in the signature; operates on the current context available to the service instance.
*   **Return Value**: Returns `true` if a valid version was successfully identified and resolved; otherwise, returns `false`.
*   **Exceptions**: This method does not throw exceptions; failure to resolve is indicated by the boolean return value.

### `public string StripVersionFromPath`
Removes the version segment from a given URL path string, returning the normalized path suitable for internal routing.
*   **Parameters**: Accepts the raw request path containing the version identifier (e.g., `/api/v1/users`).
*   **Return Value**: Returns a `string` representing the path with the version segment removed (e.g., `/api/users`). If no version segment is found, the original path may be returned unchanged depending on implementation specifics.
*   **Exceptions**: May throw `ArgumentNullException` if the provided path is null.

### `public static object BuildVersionErrorResponse`
Generates a standardized error response object indicating that a valid API version could not be determined or is unsupported.
*   **Parameters**: None explicitly listed in the signature; typically utilizes default error messaging or context-derived details internally.
*   **Return Value**: Returns an `object` representing the structured error response (e.g., a DTO or anonymous type) ready for serialization.
*   **Exceptions**: Does not typically throw exceptions unless memory allocation fails.

## Usage

### Example 1: Resolving Version and Normalizing Path
This example demonstrates how to use the service to detect a version and clean the path for downstream routing logic.

```csharp
public async Task HandleRequestAsync(HttpContext context)
{
    var versioningService = new ApiVersioningService();
    string rawPath = context.Request.Path.Value;

    // Attempt to resolve the version
    if (versioningService.TryResolveVersion())
    {
        // Version found, strip it from the path for routing
        string normalizedPath = versioningService.StripVersionFromPath(rawPath);
        
        // Proceed with routing using normalizedPath
        await RouteRequestAsync(normalizedPath);
    }
    else
    {
        // Version resolution failed, return standard error
        var errorResponse = ApiVersioningService.BuildVersionErrorResponse();
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}
```

### Example 2: Generating Error Responses for Middleware
This example shows how the static method can be utilized within middleware to immediately return a response when versioning requirements are not met.

```csharp
public class ApiVersionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var service = new ApiVersioningService();

        if (!service.TryResolveVersion())
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400;
            
            // Use the static helper to build the consistent error payload
            var errorPayload = ApiVersioningService.BuildVersionErrorResponse();
            
            await context.Response.WriteAsJsonAsync(errorPayload);
            return;
        }

        await _next(context);
    }
}
```

## Notes

*   **Thread Safety**: The instance methods `TryResolveVersion` and `StripVersionFromPath` rely on the current request context. While the `ApiVersioningService` class itself does not expose mutable static state, instances should generally be scoped to the request lifetime to ensure context accuracy. The static method `BuildVersionErrorResponse` is thread-safe as it does not maintain state.
*   **Path Format Expectations**: `StripVersionFromPath` expects a specific path format (typically `/v{number}` or similar). Passing a null or empty string to this method may result in an exception or unexpected return values; callers should validate input paths before invocation.
*   **Resolution Logic**: `TryResolveVersion` returns `false` rather than throwing an exception when a version is missing. This design pattern requires callers to explicitly handle the `false` case, usually by invoking `BuildVersionErrorResponse`.
*   **Return Type Variance**: `BuildVersionErrorResponse` returns `object`. Consumers must ensure the returned object is compatible with the configured JSON serializer or cast it to the expected response DTO type if strong typing is required.
