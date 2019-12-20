# UrlUtility

Utility class providing common URL manipulation and parsing operations used throughout the dotnet-api-gateway project.

## API

### `public static string CombineUrl(string baseUrl, string relativePath)`

Combines a base URL with a relative path, ensuring proper handling of slashes and edge cases.

- **Parameters**
  - `baseUrl`: The base URL (may or may not end with a slash)
  - `relativePath`: The relative path to append (may or may not start with a slash)
- **Return value**: A new string representing the combined URL
- **Throws**: `ArgumentNullException` if either parameter is null

### `public static Dictionary<string, string> ParseQueryString(string query)`

Parses a URL query string into a dictionary of key-value pairs.

- **Parameters**
  - `query`: The query string portion of a URL (including the leading `?`)
- **Return value**: A `Dictionary<string, string>` containing the parsed parameters. Empty dictionary if the input is null or empty.
- **Throws**: `ArgumentException` if the query string is malformed

### `public static string BuildQueryString(Dictionary<string, string> parameters)`

Constructs a query string from a dictionary of parameters.

- **Parameters**
  - `parameters`: Dictionary of key-value pairs to include in the query string
- **Return value**: A string representing the query string (including the leading `?`), or an empty string if the dictionary is null or empty
- **Throws**: `ArgumentNullException` if the dictionary is null

### `public static string? GetHostname(string url)`

Extracts the hostname from a URL.

- **Parameters**
  - `url`: The URL to parse
- **Return value**: The hostname portion of the URL, or null if the URL is null, empty, or malformed
- **Throws**: None

### `public static int GetPort(string url)`

Extracts the port number from a URL.

- **Parameters**
  - `url`: The URL to parse
- **Return value**: The port number, or -1 if the URL is null, empty, malformed, or does not specify a port
- **Throws**: None

### `public static bool IsValidUrl(string url)`

Determines whether a string is a valid URL.

- **Parameters**
  - `url`: The string to validate
- **Return value**: `true` if the string is a valid URL; otherwise, `false`
- **Throws**: None

### `public static string SanitizeUrl(string url)`

Removes potentially harmful or unnecessary characters from a URL while preserving its structure.

- **Parameters**
  - `url`: The URL to sanitize
- **Return value**: A sanitized version of the input URL
- **Throws**: `ArgumentNullException` if the input is null

### `public static string GetPath(string url)`

Extracts the path portion from a URL.

- **Parameters**
  - `url`: The URL to parse
- **Return value**: The path portion of the URL, or an empty string if the URL is null, empty, or malformed
- **Throws**: None

### `public static bool HasQueryParameter(string url, string parameterName)`

Checks whether a URL contains a specific query parameter.

- **Parameters**
  - `url`: The URL to check
  - `parameterName`: The name of the query parameter to search for
- **Return value**: `true` if the parameter exists in the URL; otherwise, `false`
- **Throws**: `ArgumentNullException` if either parameter is null

## Usage

```csharp
// Example 1: Combining URLs and building query strings
var baseUrl = "https://api.example.com";
var relativePath = "/users";
var fullUrl = UrlUtility.CombineUrl(baseUrl, relativePath); // "https://api.example.com/users"

var queryParams = new Dictionary<string, string>
{
    { "page", "1" },
    { "limit", "10" }
};
var queryString = UrlUtility.BuildQueryString(queryParams); // "?page=1&limit=10"
var finalUrl = fullUrl + queryString; // "https://api.example.com/users?page=1&limit=10"

// Example 2: Parsing and validating URLs
var testUrl = "https://example.com:8080/path?key=value";
var isValid = UrlUtility.IsValidUrl(testUrl); // true
var hostname = UrlUtility.GetHostname(testUrl); // "example.com"
var port = UrlUtility.GetPort(testUrl); // 8080
var path = UrlUtility.GetPath(testUrl); // "/path"
var hasKeyParam = UrlUtility.HasQueryParameter(testUrl, "key"); // true
```

## Notes

- **Thread safety**: All methods are thread-safe as they do not maintain any shared state and operate purely on input parameters.
- **Edge cases**:
  - `CombineUrl` handles cases where the base URL or relative path may or may not include trailing/leading slashes.
  - `ParseQueryString` returns an empty dictionary for null or empty input rather than throwing.
  - `GetPort` returns -1 for URLs without an explicit port rather than throwing.
  - `SanitizeUrl` preserves the URL structure while removing potentially harmful characters.
  - `HasQueryParameter` performs case-sensitive comparison for parameter names.
