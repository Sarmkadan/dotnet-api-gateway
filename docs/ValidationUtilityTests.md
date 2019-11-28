# ValidationUtilityTests

Unit test class for `ValidationUtility` providing test coverage for common validation scenarios used throughout the `dotnet-api-gateway` project. Each test verifies the behavior of specific validation methods under various input conditions, ensuring correctness and reliability of validation logic in production code.

## API

### `IsValidEmail_VariousInputs_ReturnsExpected`
Tests the `ValidationUtility.IsValidEmail` method with a range of valid and invalid email strings. Verifies that the method returns `true` for syntactically correct email addresses and `false` otherwise. No exceptions are expected under normal test conditions.

### `IsValidUrl_VariousUrls_ReturnsExpected`
Validates the behavior of `ValidationUtility.IsValidUrl` across various URL formats including HTTP, HTTPS, and malformed strings. Ensures correct parsing and validation of URL structures. No exceptions are expected during execution.

### `IsValidIpAddress_VariousIps_ReturnsExpected`
Exercises `ValidationUtility.IsValidIpAddress` with IPv4 and IPv6 addresses, both valid and invalid. Confirms accurate detection of IP address formats. No exceptions are expected.

### `IsValidUuid_VariousUuids_ReturnsExpected`
Tests `ValidationUtility.IsValidUuid` using valid and invalid UUID strings (v1–v5 and malformed formats). Verifies correct identification of UUID compliance. No exceptions are expected.

### `IsNullOrEmpty_VariousStrings_ReturnsExpected`
Validates `ValidationUtility.IsNullOrEmpty` behavior for `null`, empty, and non-empty strings. Ensures consistent return values across edge cases. No exceptions are expected.

### `IsValidLength_VariousLengths_ReturnsExpected`
Tests `ValidationUtility.IsValidLength` with strings of varying lengths and specified minimum/maximum bounds. Confirms correct length validation logic. No exceptions are expected.

### `IsAlphanumeric_VariousStrings_ReturnsExpected`
Exercises `ValidationUtility.IsAlphanumeric` with strings containing letters, digits, and special characters. Verifies that only alphanumeric strings are accepted. No exceptions are expected.

### `IsAsciiOnly_VariousStrings_ReturnsExpected`
Tests `ValidationUtility.IsAsciiOnly` with ASCII and non-ASCII character strings. Ensures accurate detection of ASCII-only content. No exceptions are expected.

### `IsValidPort_VariousPorts_ReturnsExpected`
Validates `ValidationUtility.IsValidPort` using port numbers within and outside the valid range (0–65535). Confirms correct port validation. No exceptions are expected.

### `IsValidHttpMethod_VariousMethods_ReturnsExpected`
Tests `ValidationUtility.IsValidHttpMethod` with standard HTTP methods (e.g., GET, POST) and invalid values. Ensures correct HTTP method validation. No exceptions are expected.

### `IsValidHttpStatusCode_VariousStatusCodes_ReturnsExpected`
Exercises `ValidationUtility.IsValidHttpStatusCode` using valid and invalid HTTP status codes (100–599). Verifies accurate status code validation. No exceptions are expected.

### `IsNull_WithNull_ReturnsTrue`
Tests `ValidationUtility.IsNull` with a `null` reference. Confirms that `true` is returned. No exceptions are expected.

### `IsNull_WithObject_ReturnsFalse`
Tests `ValidationUtility.IsNull` with a non-null object. Confirms that `false` is returned. No exceptions are expected.

### `IsValidType_WithCorrectType_ReturnsTrue`
Validates `ValidationUtility.IsValidType` when the input object matches the expected type. Ensures correct type validation. No exceptions are expected.

### `IsValidType_WithWrongType_ReturnsFalse`
Tests `ValidationUtility.IsValidType` when the input object does not match the expected type. Confirms that `false` is returned. No exceptions are expected.

### `IsNullOrEmpty_WithNullCollection_ReturnsTrue`
Tests `ValidationUtility.IsNullOrEmpty` with a `null` collection. Confirms that `true` is returned. No exceptions are expected.

### `IsNullOrEmpty_WithEmptyCollection_ReturnsTrue`
Tests `ValidationUtility.IsNullOrEmpty` with an empty collection. Confirms that `true` is returned. No exceptions are expected.

### `IsNullOrEmpty_WithPopulatedCollection_ReturnsFalse`
Tests `ValidationUtility.IsNullOrEmpty` with a non-empty collection. Confirms that `false` is returned. No exceptions are expected.

### `HasRequiredKeys_WithAllKeys_ReturnsTrue`
Validates `ValidationUtility.HasRequiredKeys` when all required keys are present in a dictionary. Ensures correct key presence detection. No exceptions are expected.

### `HasRequiredKeys_WithMissingKey_ReturnsFalse`
Tests `ValidationUtility.HasRequiredKeys` when one or more required keys are missing. Confirms that `false` is returned. No exceptions are expected.

## Usage
