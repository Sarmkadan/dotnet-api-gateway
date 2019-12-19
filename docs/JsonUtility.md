# JsonUtility
Static helper class that wraps System.Text.Json to provide convenient JSON serialization, deserialization, validation, and merging utilities for the dotnet-api-gateway project.

## API
### Serialize<T>(T value)
Serializes an object to a JSON string.  
**Parameters**  
- `value`: The object instance to serialize.  
**Return value**  
- JSON representation of `value`; returns `null` if `value` is `null`.  
**Throws**  
- `JsonException` if serialization fails (e.g., unsupported type or circular reference).

### SerializePretty<T>(T value)
Serializes an object to an indented (pretty‑printed) JSON string.  
**Parameters**  
- `value`: The object instance to serialize.  
**Return value**  
- Pretty‑printed JSON string; returns `null` if `value` is `null`.  
**Throws**  
- `JsonException` if serialization fails.

### Deserialize<T>(string json)
Deserializes a JSON string into an instance of `T`.  
**Parameters**  
- `json`: JSON input; if `null`, empty, or whitespace the method returns the default value for `T?`.  
**Return value**  
- Deserialized object of type `T?`; returns `null` when the input is invalid or missing and `T` is a reference type or nullable value type.  
**Throws**  
- `JsonException` if `json` is malformed and cannot be mapped to `T`.

### DeserializeSafe<T>(string json)
Attempts to deserialize JSON, returning a default value instead of throwing on failure.  
**Parameters**  
- `json`: JSON input.  
**Return value**  
- Deserialized object of type `T?`; returns `default(T?)` when `json` is `null`, empty, whitespace, or invalid.  
**Throws**  
- None; parsing errors are swallowed and result is the default value.

### ParseDynamic(string json)
Parses JSON into a `System.Text.Json.JsonElement` for dynamic inspection.  
**Parameters**  
- `json`: JSON string to parse.  
**Return value**  
- `JsonElement` representing the root; returns `null` if `json` is `null` or empty.  
**Throws**  
- `JsonException` if `json` is not valid JSON.

### IsValidJson(string json)
Determines whether a string contains valid JSON.  
**Parameters**  
- `json`: Input to test.  
**Return value**  
- `true` if `json` is parsable as JSON; otherwise `false`.  
**Throws**  
- None; returns `false` on any exception.

### MergeJson(string jsonLeft, string jsonRight)
Merges two JSON objects, preferring values from `jsonRight` on duplicate keys.  
**Parameters**  
- `jsonLeft`: First JSON object.  
- `jsonRight`: Second JSON object whose properties overwrite conflicts in `jsonLeft`.  
**Return value**  
- Merged JSON string; returns `null` if both inputs are `null` or empty.  
**Throws**  
- `JsonException` if either input is not a valid JSON object.

## Usage
Example 1: Serializing and deserializing a POCO.
```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var p = new Person { Name = "Ada", Age = 30 };
string json = JsonUtility.SerializePretty<Person>(p);
// json contains formatted JSON
Person? restored = JsonUtility.Deserialize<Person>(json);
// restored.Name == "Ada"
```

Example 2: Safe parsing and merging.
```csharp
string left = @"{ ""id"": 1, ""name\": ""foo"" }";
string right = @"{ ""name\": \"bar\", ""value\": 42 }";

if (JsonUtility.IsValidJson(left) && JsonUtility.IsValidJson(right))
{
    JsonElement? leftElem = JsonUtility.ParseDynamic(left);
    JsonElement? rightElem = JsonUtility.ParseDynamic(right);
    // leftElem/rightElem can be inspected dynamically …
    string merged = JsonUtility.MergeJson(left, right);
    // merged ≈ { "id":1, "name":"bar", "value":42 }
}
else
{
    // handle invalid input
}
```

## Notes
- All members are static and thread‑safe; they rely on the concurrent‑safe System.Text.Json implementation.  
- `Serialize*` methods return `null` for a `null` input value and do not throw `ArgumentNullException`.  
- `Deserialize` returns the default value for `T?` on null/empty/invalid input; `DeserializeSafe` never throws and also returns the default on any parsing error.  
- `ParseDynamic` returns `null` for null/empty strings; otherwise throws `JsonException` on malformed JSON.  
- `IsValidJson` treats null, empty, or whitespace strings as invalid (`false`).  
- `MergeJson` expects both arguments to represent JSON objects; supplying non‑object JSON may cause a `JsonException`. The merge operation does not preserve property ordering.  
- Nullable value types (e.g., `int?`) are supported; deserializing missing or invalid properties yields `null`.
