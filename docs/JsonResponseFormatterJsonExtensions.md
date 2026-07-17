# JsonResponseFormatterJsonExtensions

Extension methods for serializing and deserializing API response types to and from JSON strings. These methods provide a convenient way to convert between .NET response objects and their JSON representations using consistent serialization settings.

## API

### ToJson<T>(SuccessResponse<T>, bool)

Serializes a `SuccessResponse<T>` instance to a JSON string.

- **Parameters:**
  - `value`: The response instance to serialize.
  - `indented` (optional): Whether to format the JSON with indentation for readability. Defaults to `false`.
- **Returns:** A JSON string representation of the response.
- **Throws:** `ArgumentNullException` if `value` is null.

### ToJson(ErrorResponse, bool)

Serializes an `ErrorResponse` instance to a JSON string.

- **Parameters:**
  - `value`: The error response instance to serialize.
  - `indented` (optional): Whether to format the JSON with indentation for readability. Defaults to `false`.
- **Returns:** A JSON string representation of the error response.
- **Throws:** `ArgumentNullException` if `value` is null.

### ToJson<T>(PaginatedResponse<T>, bool)

Serializes a `PaginatedResponse<T>` instance to a JSON string.

- **Parameters:**
  - `value`: The paginated response instance to serialize.
  - `indented` (optional): Whether to format the JSON with indentation for readability. Defaults to `false`.
- **Returns:** A JSON string representation of the paginated response.
- **Throws:** `ArgumentNullException` if `value` is null.

### FromJson<T>(string)

Deserializes a JSON string into a `SuccessResponse<T>` instance.

- **Type Parameters:**
  - `T`: The type of data in the response.
- **Parameters:**
  - `json`: The JSON string to deserialize.
- **Returns:** The deserialized response instance, or `null` if the JSON is invalid or deserialization fails.
- **Throws:** `ArgumentException` if `json` is null or empty.

### FromJson(string)

Deserializes a JSON string into an `ErrorResponse` instance.

- **Parameters:**
  - `json`: The JSON string to deserialize.
- **Returns:** The deserialized error response instance, or `null` if the JSON is invalid or deserialization fails.
- **Throws:** `ArgumentException` if `json` is null or empty.

### TryFromJson<T>(string, out SuccessResponse<T>?)

Attempts to deserialize a JSON string into a `SuccessResponse<T>` instance.

- **Type Parameters:**
  - `T`: The type of data in the response.
- **Parameters:**
  - `json`: The JSON string to deserialize.
  - `value`: Output parameter that receives the deserialized response if successful.
- **Returns:** `true` if deserialization succeeded; otherwise, `false`.
- **Throws:** `ArgumentException` if `json` is null or empty.

### TryFromJson(string, out ErrorResponse?)

Attempts to deserialize a JSON string into an `ErrorResponse` instance.

- **Parameters:**
  - `json`: The JSON string to deserialize.
  - `value`: Output parameter that receives the deserialized error response if successful.
- **Returns:** `true` if deserialization succeeded; otherwise, `false`.
- **Throws:** `ArgumentException` if `json` is null or empty.

### TryFromJson<T>(string, out PaginatedResponse<T>?)

Attempts to deserialize a JSON string into a `PaginatedResponse<T>` instance.

- **Type Parameters:**
  - `T`: The type of data items in the response.
- **Parameters:**
  - `json`: The JSON string to deserialize.
  - `value`: Output parameter that receives the deserialized paginated response if successful.
- **Returns:** `true` if deserialization succeeded; otherwise, `false`.
- **Throws:** `ArgumentException` if `json` is null or empty.
