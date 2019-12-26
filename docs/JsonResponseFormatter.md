# JsonResponseFormatter

A utility class for standardizing JSON response formatting in ASP.NET Core applications, providing strongly-typed methods to generate consistent success, error, validation, and paginated responses with built-in serialization and metadata handling.

## API

### `FormatSuccess<T>(T data)`
Formats a successful response containing typed data.

- **Parameters**:
  - `data` (T): The data payload to include in the response.
- **Return value**: A JSON string representing a successful response with the provided data.
- **Throws**: `System.Text.Json.JsonException` if serialization of the data fails.

### `FormatSuccess()`
Formats a successful response without a data payload.

- **Return value**: A JSON string representing a successful empty response.
- **Throws**: Never throws.

### `FormatError(string errorCode, string message)`
Formats an error response with a machine-readable error code and human-readable message.

- **Parameters**:
  - `errorCode` (string): A unique identifier for the error type.
  - `message` (string): A descriptive message explaining the error.
- **Return value**: A JSON string representing an error response.
- **Throws**: Never throws.

### `FormatValidationError(string message, Dictionary<string, object>? details)`
Formats a validation error response with optional detailed validation failures.

- **Parameters**:
  - `message` (string): A descriptive message about the validation failure.
  - `details` (Dictionary<string, object>?): Optional dictionary of field-specific error details.
- **Return value**: A JSON string representing a validation error response.
- **Throws**: Never throws.

### `FormatPaginated<T>(List<T> data, PaginationMetadata? pagination)`
Formats a paginated response containing a list of items and optional pagination metadata.

- **Parameters**:
  - `data` (List<T>): The list of items to include in the response.
  - `pagination` (PaginationMetadata?): Optional pagination metadata (e.g., total count, page size).
- **Return value**: A JSON string representing a paginated response.
- **Throws**: `System.Text.Json.JsonException` if serialization of the data or metadata fails.

### `FormatBytes<T>(T data)`
Serializes the provided data to a UTF-8 encoded byte array.

- **Parameters**:
  - `data` (T): The data to serialize.
- **Return value**: A byte array representing the JSON-encoded data.
- **Throws**: `System.Text.Json.JsonException` if serialization fails.

### `Success` (property)
Indicates whether the response represents a success or failure.

- **Type**: `bool`
- **Access**: Read-only
- **Notes**: Present in all response types.

### `Data` (property)
The typed data payload of the response.

- **Type**: `T?`
- **Access**: Read-only
- **Notes**: `null` for empty or error responses.

### `Message` (property)
A human-readable message describing the response.

- **Type**: `string?`
- **Access**: Read-only
- **Notes**: May be `null` for machine-readable responses.

### `Timestamp` (property)
The UTC timestamp when the response was generated.

- **Type**: `DateTime`
- **Access**: Read-only
- **Notes**: Always in UTC.

### `ErrorCode` (property)
A machine-readable error code.

- **Type**: `string`
- **Access**: Read-only
- **Notes**: Present only in error responses.

### `StatusCode` (property)
The HTTP status code associated with the response.

- **Type**: `int`
- **Access**: Read-only
- **Notes**: Present only in error responses.

### `Details` (property)
A dictionary of additional error details (e.g., validation failures).

- **Type**: `Dictionary<string, object>?`
- **Access**: Read-only
- **Notes**: Present only in validation error responses.

### `Pagination` (property)
Pagination metadata for paginated responses.

- **Type**: `PaginationMetadata?`
- **Access**: Read-only
- **Notes**: Present only in paginated responses.

## Usage

### Example 1: Success Response with Data
