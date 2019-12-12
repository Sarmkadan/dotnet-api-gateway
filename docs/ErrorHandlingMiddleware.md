# ErrorHandlingMiddleware

A middleware component for ASP.NET Core pipelines that intercepts unhandled exceptions during HTTP request processing, normalizes them into a consistent JSON error response, and writes that response to the outgoing HTTP context. It prevents exception details from leaking to clients while providing structured, machine-readable error payloads.

## API

### ErrorHandlingMiddleware

```csharp
public ErrorHandlingMiddleware(RequestDelegate next)
```

Constructs the middleware with a reference to the next delegate in the pipeline.

- **Parameters:**
  - `next` (`RequestDelegate`): The delegate representing the remainder of the request pipeline. Must not be null.
- **Return value:** A new instance of `ErrorHandlingMiddleware`.
- **Exceptions:** None thrown directly from the constructor.

### InvokeAsync

```csharp
public async Task InvokeAsync(HttpContext context)
```

Invokes the middleware logic for a given HTTP context. Wraps the downstream pipeline execution in a try/catch block; if an unhandled exception occurs, it sets the response status code to 500, writes a JSON error body containing `ErrorCode`, `Message`, `Timestamp`, and optionally `Details`, and suppresses further exception propagation.

- **Parameters:**
  - `context` (`HttpContext`): The HTTP context for the current request. Must not be null.
- **Return value:** A `Task` representing the asynchronous operation.
- **Exceptions:** Does not throw. All captured exceptions are handled internally and converted to HTTP 500 responses.

### ErrorCode

```csharp
public string ErrorCode { get; set; }
```

A short, stable string identifier for the error category (e.g., `"INTERNAL_ERROR"`, `"VALIDATION_FAILED"`). Intended for programmatic consumption by API clients.

### Message

```csharp
public string Message { get; set; }
```

A human-readable description of the error. In production scenarios this should contain a generic message rather than the original exception text, to avoid information disclosure.

### Timestamp

```csharp
public DateTime Timestamp { get; set; }
```

The UTC instant at which the error was captured. Set automatically during error handling.

### Details

```csharp
public Dictionary<string, object>? Details { get; set; }
```

An optional dictionary of additional contextual data about the error (e.g., validation errors keyed by field name, correlation IDs). May be `null` when no supplementary information is available.

## Usage

### Example 1: Basic registration in the pipeline

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapGet("/", () =>
{
    throw new InvalidOperationException("Something went wrong");
});

app.Run();
```

A request to `/` produces a 500 response with a JSON body similar to:

```json
{
  "errorCode": "INTERNAL_ERROR",
  "message": "An unexpected error occurred.",
  "timestamp": "2025-03-15T10:42:00Z",
  "details": null
}
```

### Example 2: Customizing the error response shape via subclassing

```csharp
public class CustomErrorMiddleware : ErrorHandlingMiddleware
{
    public CustomErrorMiddleware(RequestDelegate next) : base(next) { }

    public override async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await base.InvokeAsync(context);
        }
        catch
        {
            // The base class already handles the response; customization
            // can occur here before or after the base logic if needed.
            throw; // re-thrown to let base complete its handling
        }
    }
}

// Registration:
app.UseMiddleware<CustomErrorMiddleware>();
```

This pattern allows extending the error payload or logging behavior without duplicating the core error-normalization logic.

## Notes

- **Thread safety:** The middleware instance is typically created once at application startup and reused across all requests. The `InvokeAsync` method does not mutate instance state; the `ErrorCode`, `Message`, `Timestamp`, and `Details` properties belong to the response model object created per-request and are not shared across concurrent invocations. The type is safe for concurrent request processing under normal ASP.NET Core hosting.
- **Response commitment:** Once `InvokeAsync` writes the JSON error response, the HTTP response is effectively finalized. Any subsequent middleware or endpoint logic attempting to modify the response after an exception has been caught may encounter an `InvalidOperationException` if the response has already started.
- **Exception filters:** The middleware catches all exceptions indiscriminately. If certain exception types should produce different status codes (e.g., 400 for validation, 404 for not-found), the middleware must be extended or replaced. The base implementation always returns 500.
- **Details dictionary:** When `Details` is populated, its values must be serializable by the configured JSON serializer (typically `System.Text.Json`). Cyclic references or non-serializable types will cause the error response itself to fail, potentially dropping the connection.
- **Ordering:** This middleware should be placed early in the pipeline to catch exceptions from downstream components. Placing it too late may allow exceptions to escape to the server’s default error handler.
