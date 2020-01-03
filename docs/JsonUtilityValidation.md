# JsonUtilityValidation

A utility class providing validation and safety checks for JSON serialization and deserialization operations in .NET applications, particularly within the `dotnet-api-gateway` project. It offers methods to validate JSON strings, objects, and dynamic content, ensuring correctness before processing or throwing exceptions when invalid states are detected.

## API

### `IReadOnlyList<string> Validate<T>(T obj)`
Validates a strongly-typed object `obj` for JSON serialization compatibility. Returns a list of validation messages indicating issues such as unsupported types or circular references. Throws `ArgumentNullException` if `obj` is `null`.

### `IReadOnlyList<string> ValidatePretty<T>(T obj)`
Similar to `Validate<T>`, but formats validation messages with additional context for readability. Returns a list of human-readable error messages. Throws `ArgumentNullException` if `obj` is `null`.

### `IReadOnlyList<string> ValidateDeserialize(string json)`
Validates whether the provided JSON string `json` can be deserialized into a valid .NET object. Returns a list of deserialization issues or empty if valid. Throws `ArgumentNullException` if `json` is `null`.

### `IReadOnlyList<string> ValidateDeserializeSafe(string json)`
Validates JSON string `json` for deserialization safety without throwing exceptions. Returns a list of validation messages; empty if valid. Never throws exceptions.

### `IReadOnlyList<string> ValidateParseDynamic(string json)`
Validates whether the JSON string `json` can be parsed into a `dynamic` type. Returns a list of parsing issues or empty if valid. Throws `ArgumentNullException` if `json` is `null`.

### `IReadOnlyList<string> ValidateIsValidJson(string json)`
Validates whether the string `json` is syntactically valid JSON. Returns a list of syntax errors or empty if valid. Throws `ArgumentNullException` if `json` is `null`.

### `IReadOnlyList<string> ValidateMergeJson(string target, string source)`
Validates whether two JSON strings, `target` and `source`, can be safely merged without conflicts or invalid structures. Returns a list of merge issues or empty if valid. Throws `ArgumentNullException` if either string is `null`.

### `bool IsValid<T>(T obj)`
Checks if the object `obj` can be serialized to JSON without errors. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if `obj` is `null`.

### `bool IsValidPretty<T>(T obj)`
Similar to `IsValid<T>`, but returns a formatted boolean result with additional context in validation messages. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if `obj` is `null`.

### `bool IsValidDeserialize(string json)`
Checks if the JSON string `json` can be deserialized into a valid .NET object. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if `json` is `null`.

### `bool IsValidDeserializeSafe(string json)`
Checks if the JSON string `json` can be deserialized safely without throwing exceptions. Returns `true` if valid, `false` otherwise. Never throws exceptions.

### `bool IsValidParseDynamic(string json)`
Checks if the JSON string `json` can be parsed into a `dynamic` type. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if `json` is `null`.

### `bool IsValidIsValidJson(string json)`
Checks if the string `json` is syntactically valid JSON. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if `json` is `null`.

### `bool IsValidMergeJson(string target, string source)`
Checks if two JSON strings, `target` and `source`, can be safely merged without conflicts. Returns `true` if valid, `false` otherwise. Throws `ArgumentNullException` if either string is `null`.

### `void EnsureValid<T>(T obj)`
Validates the object `obj` for JSON serialization compatibility and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `obj` is `null`.

### `void EnsureValidPretty<T>(T obj)`
Similar to `EnsureValid<T>`, but uses formatted validation messages. Throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `obj` is `null`.

### `void EnsureValidDeserialize(string json)`
Validates the JSON string `json` for deserialization and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `json` is `null`.

### `void EnsureValidDeserializeSafe(string json)`
Validates the JSON string `json` for deserialization safety and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `json` is `null`.

### `void EnsureValidParseDynamic(string json)`
Validates the JSON string `json` for dynamic parsing and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `json` is `null`.

### `void EnsureValidIsValidJson(string json)`
Validates the string `json` for JSON syntax and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if `json` is `null`.

### `void EnsureValidMergeJson(string target, string source)`
Validates the merge of two JSON strings, `target` and `source`, and throws an `InvalidOperationException` if validation fails. Throws `ArgumentNullException` if either string is `null`.

## Usage
