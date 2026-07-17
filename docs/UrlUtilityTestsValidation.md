# UrlUtilityTestsValidation

A static validation utility class for testing URL manipulation operations in the dotnet-api-gateway. Provides validation results and enforcement methods for common URL handling functions including combination, sanitization, parsing, and query string operations. Used to verify URL utility behavior during testing scenarios.

## API

### ValidateCombineUrl
**Purpose:** Returns validation messages for URL combination logic.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates that URL parts are correctly combined without duplication or malformed segments.

### ValidateIsValidUrl
**Purpose:** Returns validation messages for URL format validation.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Checks whether URLs conform to expected format standards.

### ValidateGetHostname
**Purpose:** Returns validation messages for hostname extraction logic.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates correct parsing of hostnames from various URL formats.

### ValidateGetPort
**Purpose:** Returns validation messages for port extraction logic.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates port number retrieval including default port handling.

### ValidateHasQueryParameter
**Purpose:** Returns validation messages for query parameter existence checks.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates detection of specific query parameters in URLs.

### ValidateSanitizeUrl
**Purpose:** Returns validation messages for URL sanitization operations.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates removal of unsafe or malformed URL components.

### ValidateCombineUrlResult
**Purpose:** Returns validation messages for the final result of URL combination.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates the output format and correctness of combined URLs.

### ValidateParseQueryString
**Purpose:** Returns validation messages for query string parsing logic.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates proper decomposition of query strings into key-value pairs.

### ValidateBuildQueryString
**Purpose:** Returns validation messages for query string construction logic.  
**Return Value:** `IReadOnlyList<string>` containing validation error or success messages.  
**Notes:** Validates proper encoding and formatting of query string parameters.

### IsValidCombineUrl
**Purpose:** Indicates whether URL combination validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidIsValidUrl
**Purpose:** Indicates whether URL format validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidGetHostname
**Purpose:** Indicates whether hostname extraction validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidGetPort
**Purpose:** Indicates whether port extraction validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidHasQueryParameter
**Purpose:** Indicates whether query parameter check validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidSanitizeUrl
**Purpose:** Indicates whether URL sanitization validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidCombineUrlResult
**Purpose:** Indicates whether combined URL result validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidParseQueryString
**Purpose:** Indicates whether query string parsing validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### IsValidBuildQueryString
**Purpose:** Indicates whether query string building validation passed.  
**Return Value:** `bool` - true if validation succeeded, false otherwise.

### EnsureValidCombineUrl
**Purpose:** Throws an exception if URL combination validation failed.  
**Exceptions:** `InvalidOperationException` when `IsValidCombineUrl` is false.

### EnsureValidIsValidUrl
**Purpose:** Throws an exception if URL format validation failed.  
**Exceptions:** `InvalidOperationException` when `IsValidIsValidUrl` is false.

## Usage

```csharp
// Validate URL operations during gateway configuration testing
[Test]
public void TestUrlCombinationLogic()
{
    var baseUrl = "https://api.example.com/v1/";
    var relativePath = "users/profile";
    
    UrlUtilityTestsValidation.ValidateCombineUrl = new[] { "Base URL must not end with slash" };
    UrlUtilityTestsValidation.IsValidCombineUrl = false;
    
    Assert.IsFalse(UrlUtilityTestsValidation.IsValidCombineUrl);
    CollectionAssert.Contains(UrlUtilityTestsValidation.ValidateCombineUrl, "Base URL must not end with slash");
    
    // After fixing the implementation
    UrlUtilityTestsValidation.IsValidCombineUrl = true;
    UrlUtilityTestsValidation.EnsureValidCombineUrl(); // No exception thrown
}
```

```csharp
// Verify query string parsing and building in request processing
[Test]
public void TestQueryStringOperations()
{
    var queryString = "name=John&age=30&active=true";
    
    UrlUtilityTestsValidation.ValidateParseQueryString = new[] { "Successfully parsed 3 parameters" };
    UrlUtilityTestsValidation.IsValidParseQueryString = true;
    
    Assert.IsTrue(UrlUtilityTestsValidation.IsValidParseQueryString);
    
    UrlUtilityTestsValidation.ValidateBuildQueryString = new[] { "Encoded special characters correctly" };
    UrlUtilityTestsValidation.IsValidBuildQueryString = true;
    
    UrlUtilityTestsValidation.EnsureValidBuildQueryString(); // Confirms valid state
}
```

## Notes

All validation properties are static and thread-safe for read operations. The `EnsureValid*` methods throw `InvalidOperationException` when corresponding validation fails, making them suitable for guard clauses in critical code paths. Edge cases include handling of null or empty URL inputs, URLs with missing schemes, and query strings with duplicate keys. Validation lists may contain multiple messages for complex validation scenarios. Boolean validity flags provide quick status checks without accessing detailed message collections.
