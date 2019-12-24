# RequestTransformationController

A controller in the `dotnet-api-gateway` project responsible for applying request transformations such as modifying headers, body content, and query parameters before forwarding requests to downstream services. It provides endpoints to test and validate transformation logic without affecting live traffic.

## API

### `RequestTransformationController`
The controller class exposing endpoints for testing request transformations.

### `IActionResult TestHeaderTransformation()`
Applies header transformations defined in `HeadersToAdd` and `HeadersToRemove` to the incoming request headers stored in `InputHeaders`. Returns:
- `200 OK` with the transformed headers if successful.
- `400 Bad Request` if `InputHeaders` is null or empty.
Throws: No exceptions are thrown; only HTTP status codes are returned.

### `IActionResult TestBodyTransformation()`
Applies body transformations defined in `TransformationRules` to the incoming request body stored in `InputBody`. Returns:
- `200 OK` with the transformed body if successful.
- `400 Bad Request` if `InputBody` is null or empty.
- `400 Bad Request` if `TransformationRules` is null or empty.
Throws: No exceptions are thrown; only HTTP status codes are returned.

### `IActionResult TestQueryParamTransformation()`
Applies query parameter transformations defined in `ParamMapping` and `ParamsToRemove` to the incoming request parameters stored in `InputParams`. Returns:
- `200 OK` with the transformed parameters if successful.
- `400 Bad Request` if `InputParams` is null or empty.
- `400 Bad Request` if `ParamMapping` is null or empty and `ParamsToRemove` is null.
Throws: No exceptions are thrown; only HTTP status codes are returned.

### `Dictionary<string, string> InputHeaders`
Gets or sets the incoming request headers to be transformed. Must not be null when invoking transformation methods.

### `Dictionary<string, string>? HeadersToAdd`
Gets or sets a dictionary of headers to add to the request, where keys are header names and values are header values. If null, no headers are added.

### `List<string>? HeadersToRemove`
Gets or sets a list of header names to remove from the request. If null, no headers are removed.

### `string? InputBody`
Gets or sets the incoming request body to be transformed. If null, body transformations cannot be applied.

### `Dictionary<string, object>? TransformationRules`
Gets or sets a dictionary of transformation rules for the request body, where keys are JSON paths and values are replacement values. If null, no body transformations are applied.

### `Dictionary<string, string> InputParams`
Gets or sets the incoming request query parameters to be transformed. Must not be null when invoking transformation methods.

### `Dictionary<string, string>? ParamMapping`
Gets or sets a dictionary mapping original parameter names to new parameter names. If null, no parameter renaming occurs.

### `List<string>? ParamsToRemove`
Gets or sets a list of parameter names to remove from the request. If null, no parameters are removed.

## Usage

```csharp
// Example 1: Testing header transformations
var controller = new RequestTransformationController
{
    InputHeaders = new Dictionary<string, string> { { "X-Original", "value" } },
    HeadersToAdd = new Dictionary<string, string> { { "X-New", "added" } },
    HeadersToRemove = new List<string> { "X-Original" }
};
var result = controller.TestHeaderTransformation();
var transformedHeaders = result.Value; // Contains { "X-New": "added" }

// Example 2: Testing body transformations
var controller = new RequestTransformationController
{
    InputBody = "{\"name\":\"old\"}",
    TransformationRules = new Dictionary<string, object> { { "$.name", "new" } }
};
var result = controller.TestBodyTransformation();
var transformedBody = result.Value; // Contains "{\"name\":\"new\"}"
```

## Notes

- Transformations are applied in the order: removal, addition (for headers), and then body/query parameter transformations.
- If `HeadersToAdd` contains a header already present in `InputHeaders`, the existing value is overwritten.
- If `ParamMapping` maps a parameter to an existing name, the behavior is undefined; avoid such mappings.
- The controller is not thread-safe. Concurrent access to public properties or methods may lead to race conditions. External synchronization is required if used in a multi-threaded context.
