# GatewayExceptionExtensions

The `GatewayExceptionExtensions` static class provides a set of extension methods for the `System.Exception` type, designed to standardize the handling, classification, and reporting of exceptions within the API gateway's request pipeline. These methods facilitate the identification of client-side versus server-side errors, allow for consistent JSON serialization of exception details for API responses, and integrate seamlessly with `Microsoft.Extensions.Logging` for structured diagnostic recording.

## API

### IsClientError(this Exception exception)
Determines whether the given exception represents a client-side (4xx) error within the gateway's context.
- **Returns:** `true` if the exception is classified as a client error; otherwise, `false`.

### IsServerError(this Exception exception)
Determines whether the given exception represents a server-side (5xx) error within the gateway's context.
- **Returns:** `true` if the exception is classified as a server error; otherwise, `false`.

### ToJson(this Exception exception)
Serializes the provided exception into a JSON string, ensuring that information is formatted appropriately for API responses or audit logs.
- **Returns:** A `string` containing the serialized exception data.

### Log(this Exception exception, ILogger logger)
Logs the exception detail using the provided `ILogger` instance, automatically selecting the appropriate severity level based on the exception type.
- **Parameters:** `logger`: The `ILogger` instance to be used for recording the exception.

## Usage

### Example 1: Classifying and Serializing an Exception for API Response
```csharp
try
{
    await _gatewayService.ProcessRequestAsync(context);
}
catch (Exception ex)
{
    if (ex.IsClientError())
    {
        var errorJson = ex.ToJson();
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(errorJson);
    }
}
```

### Example 2: Automatic Logging of Gateway Exceptions
```csharp
catch (Exception ex)
{
    // Automatically logs the exception at the appropriate severity level
    ex.Log(_logger);
    
    throw; // Re-throw to allow global exception handler to finalize processing
}
```

## Notes

- **Thread Safety:** These extension methods are inherently thread-safe, as they do not maintain or mutate any shared state. They operate on the provided `Exception` instance, which is effectively immutable after it has been thrown.
- **Null Arguments:** Calling these methods on a null `Exception` instance will throw a `NullReferenceException`. The `Log` method will throw an `ArgumentNullException` if the provided `logger` instance is null.
