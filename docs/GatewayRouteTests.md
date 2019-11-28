# GatewayRouteTests

`GatewayRouteTests` contains a suite of unit tests that verify the behavior of the `GatewayRoute` type in the `dotnet-api-gateway` project. Each test method focuses on a specific aspect of route matching, validation, or method support, ensuring that the gateway correctly interprets path patterns, enforces constraints, and reacts appropriately to invalid configurations.

## API

| Member | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `MatchesPath_ExactStaticPath_ReturnsTrue` | Confirms that an exact static path pattern matches the same input string. | none | `void` | Throws if the match result is not `true`. |
| `MatchesPath_WildcardSegment_MatchesAnyValue` | Verifies that a wildcard segment (`*`) matches any single path segment, regardless of its value. | none | `void` | Throws if the wildcard does not produce a match. |
| `MatchesPath_ParameterSegment_MatchesVariableId` | Ensures that a parameter segment (e.g., `{id}`) captures a variable value and reports a successful match. | none | `void` | Throws if the parameter segment fails to match or capture the value. |
| `MatchesPath_TooManySegments_ReturnsFalse` | Checks that a route pattern with fewer segments than the input path returns `false`. | none | `void` | Throws if the match incorrectly returns `true`. |
| `MatchesPath_TooFewSegments_ReturnsFalse` | Checks that a route pattern with more segments than the input path returns `false`. | none | `void` | Throws if the match incorrectly returns `true`. |
| `MatchesPath_StaticSegmentMismatch_ReturnsFalse` | Validates that a static segment mismatch between pattern and input yields `false`. | none | `void` | Throws if the mismatch does not result in a `false` match. |
| `MatchesPath_StaticSegmentsCaseInsensitive_ReturnsTrue` | Ensures that static segment comparison is case‑insensitive, returning `true` for differing case. | none | `void` | Throws if case‑insensitive matching fails. |
| `SupportsMethod_CaseInsensitive_ReturnsExpected` | Tests that HTTP method support checks are case‑insensitive and return the expected boolean. | none | `void` | Throws if the method support result does not match the expectation. |
| `Validate_AllValid_DoesNotThrow` | Calls `GatewayRoute.Validate` with a fully correct configuration and asserts that no exception is thrown. | none | `void` | Throws if validation unexpectedly raises an exception. |
| `Validate_MissingName_ThrowsArgumentException` | Confirms that supplying a null or empty `Name` causes `Validate` to throw `ArgumentException`. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_WhitespaceName_ThrowsArgumentException` | Ensures that a `Name` consisting only of whitespace triggers `ArgumentException`. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_MissingPathPattern_ThrowsArgumentException` | Verifies that a missing or empty `PathPattern` results in `ArgumentException` during validation. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_NoAllowedMethods_ThrowsArgumentException` | Checks that an empty `AllowedMethods` collection causes `Validate` to throw `ArgumentException`. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_NoTargets_ThrowsArgumentException` | Ensures that an empty `Targets` list leads to `ArgumentException` when validating. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_TimeoutBelowMinimum_ThrowsArgumentException` | Confirms that a `Timeout` value below the allowed minimum throws `ArgumentException`. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_TimeoutAboveMaximum_ThrowsArgumentException` | Confirms that a `Timeout` value above the allowed maximum throws `ArgumentException`. | none | `void` | Throws if validation does not throw `ArgumentException`. |
| `Validate_TimeoutAtBoundary300_DoesNotThrow` | Asserts that a `Timeout` exactly at the boundary value (300 seconds) passes validation without throwing. | none | `void` | Throws if validation unexpectedly raises an exception at the boundary. |

## Usage

The test class is intended to be executed by a unit‑test framework (e.g., xUnit, NUnit, MSTest). Below are two typical ways to invoke its members.

```csharp
using Xunit;
using DotNetApiGateway.Tests; // namespace containing GatewayRouteTests

public class GatewayRouteTestsRunner
{
    [Fact]
    public void RunExactStaticPathTest()
    {
        var test = new GatewayRouteTests();
        test.MatchesPath_ExactStaticPath_ReturnsTrue(); // passes if the route matches correctly
    }

    [Fact]
    public void RunValidationTests()
    {
        var test = new GatewayRouteTests();
        test.Validate_AllValid_DoesNotThrow();          // should not throw
        Assert.Throws<ArgumentException>(() => test.Validate_MissingName_ThrowsArgumentException());
    }
}
```

If using a framework that relies on method naming conventions (e.g., NUnit’s `[Test]` attribute), the same calls can be placed directly inside test methods without additional wrappers.

## Notes

- All test methods are **stateless**; they do not modify shared fields or rely on external resources, making them safe to run concurrently in parallel test executions.
- The methods return `void`; any failure is communicated by throwing an exception (typically an `AssertionException` from the test framework) when the expected condition is not met.
- Edge cases covered include:
  - Case‑insensitive matching for static segments and HTTP methods.
  - Boundary values for the `Timeout` property (minimum, maximum, and exact limit).
  - Validation of whitespace‑only or empty strings for required fields.
- No thread‑safety guarantees are required beyond the inherent safety of pure methods; however, if the class were to be extended with mutable state, external synchronization would be necessary.
