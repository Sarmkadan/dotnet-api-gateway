# ValidationUtility

`ValidationUtility` is a static utility class that provides a collection of lightweight, self-contained validation methods for common data formats, network identifiers, HTTP constructs, and generic type checks. It is intended for use in API gateways and middleware pipelines where fast, allocation-light input validation is required before routing or processing requests.

## API

### `IsValidEmail`
```csharp
public static bool IsValidEmail(string email)
```
Validates whether the supplied string conforms to a standard email address format. Returns `true` if the string is a well-formed email; otherwise `false`. Does not throw.

### `IsValidUrl`
```csharp
public static bool IsValidUrl(string url)
```
Checks whether the given string represents a syntactically valid absolute or relative URL. Returns `true` for valid URLs, `false` otherwise. Does not throw.

### `IsValidIpAddress`
```csharp
public static bool IsValidIpAddress(string ip)
```
Determines whether the string is a valid IPv4 or IPv6 address. Returns `true` if parsing succeeds; `false` if the format is invalid or unsupported. Does not throw.

### `IsValidUuid`
```csharp
public static bool IsValidUuid(string uuid)
```
Validates that the string is a properly formatted UUID (version-agnostic). Returns `true` for valid UUID strings, `false` otherwise. Does not throw.

### `IsNullOrEmpty`
```csharp
public static bool IsNullOrEmpty(string value)
```
Returns `true` if the string is `null` or has zero length. Equivalent to `string.IsNullOrEmpty`. Does not throw.

### `IsValidLength`
```csharp
public static bool IsValidLength(string value, int minLength, int maxLength)
```
Checks that the length of the string falls within the inclusive range `[minLength, maxLength]`. Returns `true` if the length is within bounds; `false` if the string is `null` or its length is outside the range. Throws `ArgumentOutOfRangeException` if `minLength` is negative or `maxLength` is less than `minLength`.

### `IsAlphanumeric`
```csharp
public static bool IsAlphanumeric(string value)
```
Returns `true` if the string consists exclusively of letters and/or digits. Returns `false` for `null`, empty strings, or strings containing any non-alphanumeric characters. Does not throw.

### `IsAsciiOnly`
```csharp
public static bool IsAsciiOnly(string value)
```
Checks that every character in the string falls within the ASCII range (0–127). Returns `true` for ASCII-only strings; `false` for `null`, empty strings, or strings containing non-ASCII characters. Does not throw.

### `IsValidPort`
```csharp
public static bool IsValidPort(int port)
```
Validates that the integer represents a valid TCP/UDP port number (0–65535). Returns `true` for values in range; `false` otherwise. Does not throw.

### `IsValidHttpMethod`
```csharp
public static bool IsValidHttpMethod(string method)
```
Checks whether the string matches one of the standard HTTP methods (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS, etc.). Returns `true` for recognized methods; `false` for `null`, empty, or unrecognized values. Does not throw.

### `IsValidHttpStatusCode`
```csharp
public static bool IsValidHttpStatusCode(int statusCode)
```
Returns `true` if the integer is a valid HTTP status code (100–599). Returns `false` for values outside this range. Does not throw.

### `IsNull`
```csharp
public static bool IsNull(object obj)
```
Returns `true` if the supplied object is `null`; otherwise `false`. Does not throw.

### `IsValidType<T>`
```csharp
public static bool IsValidType<T>(object obj)
```
Checks whether the given object is non-null and of the specified type `T`. Returns `true` if the object is an instance of `T`; `false` if the object is `null` or of a different type. Does not throw.

### `IsNullOrEmpty<T>`
```csharp
public static bool IsNullOrEmpty<T>(IEnumerable<T> collection)
```
Returns `true` if the collection is `null` or contains no elements. Works with any `IEnumerable<T>`. Does not throw.

### `HasRequiredKeys<TKey, TValue>`
```csharp
public static bool HasRequiredKeys<TKey, TValue>(IDictionary<TKey, TValue> dictionary, params TKey[] requiredKeys)
```
Checks that the dictionary is non-null and contains all of the specified keys. Returns `true` if every required key is present; `false` if the dictionary is `null` or any required key is missing. Throws `ArgumentNullException` if `requiredKeys` is `null`.

## Usage

### Example 1: Validating an incoming HTTP request envelope
```csharp
public bool ValidateRequestEnvelope(RequestEnvelope envelope)
{
    if (ValidationUtility.IsNullOrEmpty(envelope.CorrelationId) ||
        !ValidationUtility.IsValidUuid(envelope.CorrelationId))
    {
        return false;
    }

    if (!ValidationUtility.IsValidHttpMethod(envelope.Method))
    {
        return false;
    }

    if (!ValidationUtility.HasRequiredKeys(envelope.Headers, "Authorization", "Content-Type"))
    {
        return false;
    }

    if (!ValidationUtility.IsValidUrl(envelope.CallbackUrl))
    {
        return false;
    }

    return true;
}
```

### Example 2: Sanitizing downstream service configuration
```csharp
public void ApplyServiceConfig(ServiceConfig config)
{
    if (ValidationUtility.IsNull(config))
    {
        throw new ArgumentNullException(nameof(config));
    }

    if (!ValidationUtility.IsValidPort(config.Port))
    {
        throw new ArgumentOutOfRangeException(
            nameof(config.Port), $"Port {config.Port} is not a valid port number.");
    }

    if (!ValidationUtility.IsValidIpAddress(config.Host) &&
        !ValidationUtility.IsValidUrl(config.Host))
    {
        throw new ArgumentException(
            $"Host '{config.Host}' is neither a valid IP address nor a valid URL.");
    }

    if (!ValidationUtility.IsValidLength(config.ServiceName, 1, 128) ||
        !ValidationUtility.IsAsciiOnly(config.ServiceName))
    {
        throw new ArgumentException(
            "Service name must be 1–128 ASCII characters.");
    }
}
```

## Notes

- All methods are static and stateless, making them safe to call concurrently from any thread without synchronization.
- Methods that accept strings treat `null` explicitly: `IsNullOrEmpty` returns `true` for `null`; format validators (`IsValidEmail`, `IsValidUrl`, `IsValidIpAddress`, `IsValidUuid`, `IsAlphanumeric`, `IsAsciiOnly`) return `false` for `null` inputs rather than throwing.
- `IsValidLength` is the only method that throws on invalid arguments (negative lengths or inverted min/max). All other methods return `false` for out-of-range or malformed input.
- `IsValidHttpMethod` uses a fixed set of recognized methods. Non-standard or extension methods (e.g., custom WebDAV verbs) will cause it to return `false`.
- `IsValidHttpStatusCode` covers the full range of defined and unassigned status codes (1xx–5xx). Codes outside 100–599, including negative numbers, return `false`.
- `HasRequiredKeys` performs an exact key presence check using the dictionary's native comparer. It does not validate values associated with those keys.
- `IsValidType<T>` performs a simple runtime type check. It does not consider assignability via inheritance or interface implementation beyond exact type matching (behavior depends on implementation of the type-check operator used internally; refer to source for specifics).
