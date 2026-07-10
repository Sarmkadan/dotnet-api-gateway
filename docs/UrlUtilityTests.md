# UrlUtilityTests

`UrlUtilityTests` is a collection of unit‑test methods that verify the behavior of the `UrlUtility` helper class in the `dotnet-api-gateway` project. Each test focuses on a specific scenario such as URL combination, query‑string parsing, sanitization, validation, hostname extraction, port resolution, and query‑parameter detection.

## API

| Method | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `CombineUrl_BothPartsHaveSlashes_ProducesNoDoubleSlash` | Verifies that when both the base URL and the path contain trailing/leading slashes, the result contains exactly one slash between them. | None | `void` | May throw an exception if the assertion inside the test fails (test framework). |
| `CombineUrl_NeitherPartHasSlash_JoinsWithSingleSlash` | Checks that a base URL without a trailing slash and a path without a leading slash are joined with a single slash. | None | `void` | May throw an exception if the assertion fails. |
| `CombineUrl_EmptyPath_ReturnsBaseUrl` | Ensures that supplying an empty path returns the base URL unchanged. | None | `void` | May throw an exception if the assertion fails. |
| `CombineUrl_NullBase_ReturnsPath` | Confirms that a `null` base URL results in the path being returned as‑is. | None | `void` | May throw an exception if the assertion fails. |
| `ParseQueryString_WithEncodedValues_DecodesCorrectly` | Validates that percent‑encoded query‑string values are properly decoded into their original strings. | None | `void` | May throw an exception if the assertion fails. |
| `ParseQueryString_WithDuplicateKeys_KeepsFirstValue` | Ensures that when a query string contains duplicate keys, only the first value is retained. | None | `void` | May throw an exception if the assertion fails. |
| `ParseQueryString_EmptyString_ReturnsEmptyDictionary` | Checks that an empty query string yields an empty dictionary. | None | `void` | May throw an exception if the assertion fails. |
| `ParseQueryString_WithLeadingQuestionMark_ParsesCorrectly` | Verifies that a leading `?` in the query string is ignored and the remaining string is parsed correctly. | None | `void` | May throw an exception if the assertion fails. |
| `SanitizeUrl_WithSensitiveTokenParam_ReplacesWithAsterisks` | Tests that a query parameter named `token` (or similar sensitive name) is replaced with asterisks in the sanitized URL. | None | `void` | May throw an exception if the assertion fails. |
| `SanitizeUrl_WithNonSensitiveParams_PreservesAllValues` | Confirms that non‑sensitive query parameters are left unchanged during sanitization. | None | `void` | May throw an exception if the assertion fails. |
| `SanitizeUrl_WithApiKeyParam_MasksIt` | Ensures that an `apiKey` parameter is masked (e.g., replaced with `****`) in the sanitized output. | None | `void` | May throw an exception if the assertion fails. |
| `IsValidUrl_VariousSchemes_ReturnsExpected` | Validates that the `IsValidUrl` method correctly identifies valid URLs across different schemes (http, https, ftp, etc.). | None | `void` | May throw an exception if the assertion fails. |
| `GetHostname_FullUrl_ExtractsHostOnly` | Checks that the hostname is correctly extracted from a full URL, excluding scheme, port, path, and query. | None | `void` | May throw an exception if the assertion fails. |
| `GetHostname_NullUrl_ReturnsNull` | Verifies that passing a `null` URL to `GetHostname` returns `null`. | None | `void` | May throw an exception if the assertion fails. |
| `GetPort_HttpsUrlWithNoExplicitPort_Returns443` | Ensures that an HTTPS URL without an explicit port defaults to port 443. | None | `void` | May throw an exception if the assertion fails. |
| `GetPort_HttpUrlWithNoExplicitPort_Returns80` | Ensures that an HTTP URL without an explicit port defaults to port 80. | None | `void` | May throw an exception if the assertion fails. |
| `GetPort_HttpUrlWithExplicitPort_ReturnsThatPort` | Confirms that when a port is explicitly specified in an HTTP URL, that port is returned. | None | `void` | May throw an exception if the assertion fails. |
| `HasQueryParameter_ExistingParameter_ReturnsTrue` | Validates that `HasQueryParameter` returns `true` when the specified key exists in the query string. | None | `void` | May throw an exception if the assertion fails. |
| `HasQueryParameter_MissingParameter_ReturnsFalse` | Validates that `HasQueryParameter` returns `false` when the specified key is absent. | None | `void` | May throw an exception if the assertion fails. |
| `BuildQueryString_EmptyDictionary_ReturnsEmptyString` | Checks that building a query string from an empty dictionary yields an empty string. | None | `void` | May throw an exception if the assertion fails. |

## Usage

The following examples illustrate how the functionality exercised by these tests can be used in application code. They reference the `UrlUtility` class, which is the subject of the tests.

```csharp
using MyApp.Utilities; // namespace containing UrlUtility

// Example 1: Safely combine a base URL and a relative path.
string baseUrl = "https://api.example.com/v1/";
string relativePath = "/users";
string combined = UrlUtility.CombineUrl(baseUrl, relativePath);
// combined == "https://api.example.com/v1/users"

// Example 2: Parse a query string and retrieve a value.
string query = "?token=abc%20def&sort=asc";
var parameters = UrlUtility.ParseQueryString(query);
string token = parameters["token"]; // token ==> def"?token=abc%20def&sort=asc".**  
`parameters["token"]` yields `"abc def"` after decoding.

## Notes

- All test methods are stateless and accept no parameters; they rely solely on the behavior of the static `UrlUtility` members. Consequently, they are inherently thread‑safe—multiple test runners can invoke them concurrently without side effects.
- The tests do not perform any I/O, modify global state, or depend on external resources, so they are deterministic and safe to run in parallel test suites.
- Edge cases covered by the tests include:
  - Normalization of duplicate slashes when combining URLs.
  - Handling of `null` or empty inputs for base URLs and paths.
  - Correct decoding of percent‑encoded characters in query strings.
  - Preservation of the first value when duplicate query keys appear.
  - Proper masking of sensitive query parameters (`token`, `apiKey`) during URL sanitization.
  - Default port inference for `http` (80) and `https` (443) schemes when no explicit port is present.
  - Safe extraction of hostnames and detection of query parameters.
- If any assertion within a test fails, the test framework will throw an exception (typically `AssertFailedException` or similar), signalling the test failure. No other exceptions are expected from the test methods themselves.
