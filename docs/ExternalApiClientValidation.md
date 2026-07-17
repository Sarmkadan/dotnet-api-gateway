# ExternalApiClientValidation

Provides static validation methods for the components of an external API client configuration. Each validation operation is available in three forms: a `Validate` method that returns a list of error messages, an `IsValid` method that returns a boolean, and an `EnsureValid` method that throws an exception when validation fails. The class covers endpoint URLs, HTTP methods, request data, request content, request headers, and cancellation tokens.

## API

### General Validation

- **`Validate`**  
  Returns a list of validation errors for the overall external API client configuration.  
  **Returns:** `IReadOnlyList<string>` – an empty list if the configuration is valid.

- **`IsValid`**  
  Indicates whether the overall external API client configuration is valid.  
  **Returns:** `bool` – `true` if no validation errors exist.

- **`EnsureValid`**  
  Validates the overall external API client configuration and throws an exception if any errors are found.  
  **Throws:** `InvalidOperationException` – when validation fails, with a message containing the aggregated error list.

### Endpoint Validation

- **`ValidateEndpoint`**  
  Validates the specified endpoint URL.  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidEndpoint`**  
  Checks whether the specified endpoint URL is valid.  
  **Returns:** `bool` – `true` if valid.

- **`EnsureValidEndpoint`**  
  Validates the specified endpoint URL and throws if invalid.  
  **Throws:** `InvalidOperationException` – when validation fails.

### Request Data Validation

- **`ValidateRequestData<TRequest>`**  
  Validates the specified request data object of type `TRequest`.  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidRequestData<TRequest>`**  
  Checks whether the specified request data object is valid.  
  **Returns:** `bool` – `true` if valid.

- **`EnsureValidRequestData<TRequest>`**  
  Validates the specified request data object and throws if invalid.  
  **Throws:** `InvalidOperationException` – when validation fails.

### HTTP Method Validation

- **`ValidateHttpMethod`**  
  Validates the specified HTTP method string (e.g., GET, POST).  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidHttpMethod`**  
  Checks whether the specified HTTP method string is valid.  
  **Returns:** `bool` – `true` if valid.

- **`EnsureValidHttpMethod`**  
  Validates the specified HTTP method string and throws if invalid.  
  **Throws:** `InvalidOperationException` – when validation fails.

### Request Content Validation

- **`ValidateRequestContent`**  
  Validates the specified request content string.  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidRequestContent`**  
  Checks whether the specified request content string is valid.  
  **Returns:** `bool` – `true` if valid.

- **`EnsureValidRequestContent`**  
  Validates the specified request content string and throws if invalid.  
  **Throws:** `InvalidOperationException` – when validation fails.

### Request Headers Validation

- **`ValidateRequestHeaders`**  
  Validates the specified request headers collection.  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidRequestHeaders`**  
  Checks whether the specified request headers collection is valid.  
  **Returns:** `bool` – `true` if valid.

- **`EnsureValidRequestHeaders`**  
  Validates the specified request headers collection and throws if invalid.  
  **Throws:** `InvalidOperationException` – when validation fails.

### Cancellation Token Validation

- **`ValidateCancellationToken`**  
  Validates the specified cancellation token.  
  **Returns:** `IReadOnlyList<string>` – error messages, or an empty list if valid.

- **`IsValidCancellationToken`**  
  Checks whether the specified cancellation token is valid.  
  **Returns:** `bool` – `true` if valid.

## Usage

### Example 1: Validating endpoint and HTTP method before making a request

```csharp
using static ExternalApiClientValidation;

string endpoint = "https://api.example.com/users";
string httpMethod = "POST";

if (!IsValidEndpoint(endpoint))
{
    var errors = ValidateEndpoint(endpoint);
    Console.WriteLine($"Endpoint errors: {string.Join(", ", errors)}");
}

if (!IsValidHttpMethod(httpMethod))
{
    var errors = ValidateHttpMethod(httpMethod);
    Console.WriteLine($"HTTP method errors: {string.Join(", ", errors)}");
}

// Proceed with request only if both are valid
if (IsValidEndpoint(endpoint) && IsValidHttpMethod(httpMethod))
{
    // Make the API call
}
```

### Example 2: Validating request data and headers with EnsureValid

```csharp
using ExternalApiClientValidation;

var requestData = new { Name = "John", Age = 30 };
var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer token123",
    ["Content-Type"] = "application/json"
};

try
{
    EnsureValidRequestData(requestData);
    EnsureValidRequestHeaders(headers);
    Console.WriteLine("Request data and headers are valid.");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## Notes

- All validation methods treat `null` or empty strings as invalid for endpoint, HTTP method, and request content.  
- The `ValidateRequestData<TRequest>` method may perform reflection-based checks (e.g., required properties) depending on the implementation.  
- `ValidateCancellationToken` typically checks that the token is not in a canceled state.  
- The `EnsureValid` variants throw `InvalidOperationException` with a message that aggregates all error messages from the corresponding `Validate` method.  
- All members are static and stateless; they do not modify any shared state and are thread-safe. Concurrent calls from multiple threads will not produce race conditions.  
- The generic methods (`ValidateRequestData<TRequest>`, etc.) are safe to use with any reference or value type, but validation rules may vary by type.
