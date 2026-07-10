# ApiVersioningServiceTests

Unit test suite for the `ApiVersioningService` class, validating its ability to resolve API versions from incoming HTTP requests using multiple strategies (URL path segments, custom headers, query parameters, and media type negotiation). The tests also cover default version fallback, version requirement enforcement, unsupported version rejection, policy disablement, and path-stripping behavior.

## API

### public void TryResolveVersion_UrlPath_ExtractsVersionFromPath
Verifies that when a version segment is present in the request URL path, the service correctly extracts and resolves it as the active API version.

### public void TryResolveVersion_UrlPath_CaseInsensitive
Ensures that URL path version extraction is case-insensitive, so that segments such as `/v1/` and `/V1/` are treated identically.

### public void TryResolveVersion_Header_ExtractsVersionFromHeader
Confirms that the service can read and resolve a version value from a standard or configured HTTP header in the request.

### public void TryResolveVersion_Header_CustomHeaderName
Validates that version resolution works when the version header is supplied under a non-default, custom header name as specified in the service configuration.

### public void TryResolveVersion_QueryParam_ExtractsVersionFromQuery
Tests extraction of the requested API version from a query string parameter (e.g., `?api-version=2.0`).

### public void TryResolveVersion_MediaType_ExtractsVersionFromAcceptHeader
Verifies that the service can parse a version parameter embedded in the `Accept` header media type (e.g., `application/json; v=2.0`) and resolve it correctly.

### public void TryResolveVersion_NoVersionInRequest_UsesDefaultVersion
When no version is specified through any strategy, the service falls back to a preconfigured default version. This test confirms that behavior.

### public void TryResolveVersion_RequireVersion_ReturnsFalseWhenMissing
When the service is configured to require an explicit version and none is present in the request, the resolution method returns `false`.

### public void TryResolveVersion_UnsupportedVersion_ReturnsFalse
If the request specifies a version that is not in the list of supported API versions, the method returns `false`.

### public void TryResolveVersion_SupportedVersion_ReturnsTrue
When a valid, supported version is successfully resolved from the request, the method returns `true`.

### public void TryResolveVersion_PolicyDisabled_AlwaysReturnsTrue
When the versioning policy is entirely disabled in configuration, the resolution method short-circuits and unconditionally returns `true` without performing any version extraction.

### public void StripVersionFromPath_RemovesVersionSegment
Tests that the service removes the version segment from a URL path (e.g., `/v1/resource` becomes `/resource`).

### public void StripVersionFromPath_NestedPath_RemovesVersionSegmentOnly
Confirms that only the version segment is stripped from a nested path, leaving deeper segments intact (e.g., `/v1/a/b/c` becomes `/a/b/c`).

### public void StripVersionFromPath_NoVersionSegment_ReturnsOriginalPath
When the path does not contain a recognizable version segment, the original path is returned unchanged.

### public void StripVersionFromPath_StripDisabled_ReturnsOriginalPath
If path stripping is disabled in the service configuration, the method returns the original path without modification, even if a version segment is present.

### public void TryResolveVersion_MultipleStrategies_UrlPathWinsFirst
When multiple version resolution strategies are active and more than one provides a version, the URL path strategy takes precedence over headers, query parameters, and media type negotiation.

## Usage

```csharp
// Example 1: Resolving a version from a URL path and stripping it for downstream routing
var service = new ApiVersioningService(new ApiVersioningOptions
{
    DefaultVersion = "1.0",
    SupportedVersions = new[] { "1.0", "2.0" },
    VersionStrategy = VersionStrategy.UrlPath
});

bool resolved = service.TryResolveVersion(httpContext, out var version);
if (resolved)
{
    string strippedPath = service.StripVersionFromPath(httpContext.Request.Path);
    // strippedPath is now ready for downstream forwarding without the version segment
}
```

```csharp
// Example 2: Using a custom header with required version enforcement
var service = new ApiVersioningService(new ApiVersioningOptions
{
    RequireExplicitVersion = true,
    SupportedVersions = new[] { "1.0", "2.0", "3.0" },
    VersionStrategy = VersionStrategy.Header,
    CustomVersionHeaderName = "X-MyApi-Version"
});

bool resolved = service.TryResolveVersion(httpContext, out var version);
if (!resolved)
{
    // Return 400 Bad Request — version missing or unsupported
    httpContext.Response.StatusCode = 400;
    return;
}
// Proceed with version-specific pipeline
```

## Notes

- The test suite assumes the `ApiVersioningService` implementation follows a strict precedence order when multiple strategies are active: URL path first, then header, query parameter, and media type last. Any deviation in implementation will cause `TryResolveVersion_MultipleStrategies_UrlPathWinsFirst` to fail.
- Path stripping is purely a string manipulation operation and does not validate the semantic correctness of the resulting path. Tests only verify removal of the version segment literal.
- Thread safety is not directly tested; the service is expected to be stateless with respect to individual requests, making it safe for concurrent use across HTTP requests provided configuration is not mutated at runtime.
- When versioning policy is disabled, the service bypasses all extraction and validation logic. Tests confirm that `TryResolveVersion` returns `true` immediately, and `StripVersionFromPath` returns the original path unaltered.
- The case-insensitivity test for URL paths applies only to the version segment identifier (e.g., `/v1` vs `/V1`). It does not extend to version values supplied through headers or query parameters unless explicitly configured in the service.
