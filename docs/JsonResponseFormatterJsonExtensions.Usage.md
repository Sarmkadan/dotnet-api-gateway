## Usage

### Serializing a SuccessResponse

```csharp
using DotNetApiGateway.Formatters;
using DotNetApiGateway.Models;

var response = new SuccessResponse<string>("Data loaded successfully", "Hello, World!");

// Compact JSON
string compactJson = response.ToJson(); // {"message":"Data loaded successfully","data":"Hello, World!"}

// Pretty-printed JSON
string prettyJson = response.ToJson(indented: true);
/***
{
  "message": "Data loaded successfully",
  "data": "Hello, World!"
}
***/
```

### Deserializing an ErrorResponse

```csharp
using DotNetApiGateway.Formatters;
using DotNetApiGateway.Models;

string json = "{\"error\":\"Not found\",\"message\":\"The requested resource was not found\",\"code\":\"NOT_FOUND\"}";

// Using FromJson - returns null on failure
ErrorResponse? error = ErrorResponseJsonExtensions.FromJson(json);
if (error != null)
{
    Console.WriteLine($"Error: {error.Message} (Code: {error.Code})");
}

// Using TryFromJson - returns boolean indicating success
if (ErrorResponseJsonExtensions.TryFromJson(json, out ErrorResponse? errorResult))
{
    Console.WriteLine($"Error: {errorResult.Message} (Code: {errorResult.Code})");
}
else
{
    Console.WriteLine("Failed to parse error response");
}
```

### Working with PaginatedResponse

```csharp
using DotNetApiGateway.Formatters;
using DotNetApiGateway.Models;

var paginated = new PaginatedResponse<User>(
    new[] { new User("Alice"), new User("Bob") },
    1,
    10,
    100
);

string json = paginated.ToJson();

// Deserialize back
if (PaginatedResponseJsonExtensions.TryFromJson<User>(json, out var deserialized))
{
    Console.WriteLine($"Page {deserialized.Page} of {deserialized.TotalPages} - {deserialized.Items.Length} items");
}
```

## Notes

- **Thread Safety**: The extension methods are thread-safe. The shared `JsonSerializerOptions` instance is immutable after construction, and all serialization/deserialization operations are stateless.

- **Null Handling**: Serialization methods throw `ArgumentNullException` for null inputs. Deserialization methods return `null` on failure rather than throwing, making them suitable for parsing potentially invalid JSON from external sources.

- **Naming Convention**: JSON properties use camelCase naming policy, consistent with common REST API conventions.

- **Enum Handling**: Enums are serialized using their string names via `JsonStringEnumConverter`.

- **Null Values**: Properties with null values are omitted from the JSON output due to `DefaultIgnoreCondition.WhenWritingNull`.

- **Performance**: The `GetOptions(bool)` method creates a shallow copy of the shared options for each call, allowing independent configuration of indentation without affecting the shared state.
