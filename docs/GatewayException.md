# GatewayException
The `GatewayException` type is designed to handle and propagate exceptions that occur within the API gateway, providing a standardized way to represent and communicate error information. This allows for more effective error handling and logging, enabling developers to diagnose and resolve issues more efficiently.

## API
### Constructors
* `GatewayException`: Initializes a new instance of the `GatewayException` class.
* `GatewayException`: Initializes a new instance of the `GatewayException` class.

### Properties
* `ErrorCode`: Gets the error code associated with the exception.
* `StatusCode`: Gets the HTTP status code associated with the exception.

## Usage
The following examples demonstrate how to use the `GatewayException` type in a C# application:
```csharp
// Example 1: Throwing a GatewayException
try
{
    // Code that may throw an exception
    if (someCondition)
    {
        throw new GatewayException();
    }
}
catch (GatewayException ex)
{
    Console.WriteLine($"Error code: {ex.ErrorCode}, Status code: {ex.StatusCode}");
}

// Example 2: Creating a custom GatewayException
GatewayException customException = new GatewayException();
Console.WriteLine($"Error code: {customException.ErrorCode}, Status code: {customException.StatusCode}");
```

## Notes
When using the `GatewayException` type, consider the following edge cases and thread-safety remarks:
* The `ErrorCode` and `StatusCode` properties are read-only and can be safely accessed from multiple threads.
* The `GatewayException` constructors do not throw any exceptions, but derived classes may introduce additional exceptions.
* When creating custom exceptions derived from `GatewayException`, ensure that the base class constructors are properly called to maintain consistency in error code and status code propagation.
* In a multi-threaded environment, it is essential to synchronize access to shared instances of `GatewayException` to prevent concurrent modifications and ensure thread safety.
