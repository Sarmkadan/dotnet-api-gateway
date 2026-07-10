# JsonUtilityTests

JsonUtilityTests contains a set of unit‑test methods that verify the behavior of the JSON serialization and deserialization utilities in the `dotnet-api-gateway` project. The class also exposes simple data members (`Name`, `Age`, `OptionalField`) used as test fixtures.

## API

### Public fields / properties
- **Name** (`string`)  
  Holds a test name value. No parameters; get/set directly. Does not throw.

- **Age** (`int`)  
  Holds a test age value. No parameters; get/set directly. Does not throw.

- **OptionalField** (`string?`)  
  Holds an optional string that may be null. No parameters; get/set directly. Does not throw.

### Test methods
All test methods return `void` and are intended to be invoked by a test runner. They perform assertions internally; an exception is thrown only when an assertion fails or when the underlying utility throws for invalid input.

- **Serialize_ValidObject_ReturnsJsonString**  
  Serializes a valid instance of the test fixture to a JSON string and asserts that the result is non‑null and well‑formed. No parameters. Throws if serialization fails or the assertion does not hold.

- **Serialize_WithNullableFieldNull_OmitsField**  
  Serializes an instance where `OptionalField` is `null` and asserts that the resulting JSON does not contain a property for that field. No parameters. Throws if the field is present or serialization fails.

- **SerializePretty_ValidObject_ReturnsFormattedJson**  
  Serializes a valid instance with pretty‑print formatting and asserts that the output contains indentation (e.g., newline and spaces). No parameters. Throws if formatting is missing or serialization fails.

- **Deserialize_ValidJson_ReturnsObject**  
  Deserializes a known‑good JSON string into an instance of the test fixture and asserts that all properties match the expected values. No parameters. Throws if deserialization fails or the object does not match expectations.

- **Deserialize_InvalidJson_ThrowsException**  
  Passes malformed JSON to the deserializer and asserts that an exception is thrown. No parameters. Throws if no exception is raised.

- **Deserialize_EmptyString_ReturnsNull**  
  Deserializes an empty string and asserts that the result is `null`. No parameters. Throws if a non‑null value is returned.

- **Deserialize_WhitespaceString_ReturnsNull**  
  Deserializes a string containing only whitespace and asserts that the result is `null`. No parameters. Throws if a non‑null value is returned.

- **DeserializeSafe_ValidJson_ReturnsObject**  
  Calls the “safe” deserialization wrapper with valid JSON and asserts that the returned object matches the expected values. No parameters. Throws if the wrapper fails to produce the correct object.

- **DeserializeSafe_InvalidJson_ReturnsNull**  
  Calls the safe deserialization wrapper with invalid JSON and asserts that the result is `null`. No parameters. Throws if a non‑null value is returned.

- **ParseDynamic_ValidJson_ReturnsJsonElement**  
  Parses valid JSON into a `System.Text.Json.JsonElement` using the dynamic parse helper and asserts that the element is not undefined. No parameters. Throws if parsing fails or the element is undefined.

- **ParseDynamic_InvalidJson_ReturnsNull**  
  Passes invalid JSON to the dynamic parse helper and asserts that the result is `null`. No parameters. Throws if a non‑null value is returned.

- **ParseDynamic_ValidArray_ReturnsArray**  
  Parses a valid JSON array and asserts that the resulting `JsonElement` has `ValueKind.Array`. No parameters. Throws if the result is not an array.

- **IsValidJson_VariousInputs_ReturnsExpected**  
  Tests the JSON validity checker with a variety of strings (valid, invalid, empty, whitespace) and asserts that the boolean return matches the expected outcome for each input. No parameters. Throws if any assertion fails.

- **MergeJson_BothValidJson_ReturnsMergedJson**  
  Merges two valid JSON objects and asserts that the result contains properties from both sources, with overlapping properties taken from the second object. No parameters. Throws if merging fails or the result is incorrect.

- **MergeJson_FirstJsonInvalid_ReturnsFirstJson**  
  When the first JSON string is invalid, the merge operation returns the first string unchanged. No parameters. Throws if the result differs from the first input.

- **MergeJson_SecondJsonInvalid_ReturnsFirstJson**  
  When the second JSON string is invalid, the merge operation returns the first string unchanged. No parameters. Throws if the result differs from the first input.

- **Serialize_CaseSensitiveEnumeration_SerializesAsString**  
  Serializes an enumeration with case‑sensitive naming and asserts that the serialized value matches the exact enum member name (including case). No parameters. Throws if the serialization does not preserve case sensitivity.

## Usage

The following examples show how to instantiate the test class and invoke its members manually (for demonstration; in practice the methods are executed by a test runner).

```csharp
// Example 1: Verify serialization omits null optional field
var test = new JsonUtilityTests
{
    Name = "Bob",
    Age = 42,
    OptionalField = null   // explicitly null
};
test.Serialize_WithNullableFieldNull_OmitsField(); // passes if field omitted
```

```csharp
// Example 2: Check safe deserialization of invalid JSON returns null
var test2 = new JsonUtilityTests();
test2.DeserializeSafe_InvalidJson_ReturnsNull(); // passes if result is null
```

## Notes

- The class holds no static state; each test method relies only on the instance fields (`Name`, `Age`, `OptionalField`). To avoid cross‑test contamination, create a new instance for each test or reset the fields between calls.
- All test methods are side‑effect free with respect to external resources; they only use in‑memory JSON operations.
- Thread safety: Because there is no shared mutable state, multiple threads can safely invoke methods on separate instances of `JsonUtilityTests`. Sharing a single instance across threads is not recommended unless the fields are treated as read‑only for the duration of the invocation.
- Edge cases covered by the tests include empty strings, whitespace‑only strings, null optional fields, invalid JSON payloads, and case‑sensitive enumeration handling. The merge operations demonstrate the defined behavior when one of the inputs is invalid (the valid input is returned unchanged).
