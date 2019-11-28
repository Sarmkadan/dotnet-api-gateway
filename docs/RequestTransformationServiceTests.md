# RequestTransformationServiceTests

A test suite that verifies the behavior of the request and response transformation service in the API gateway. It ensures that header manipulation, query parameter modification, path rewriting, rule ordering, and validation logic all function correctly under the documented contract. The tests cover both request-phase and response-phase rules, confirming that disabled rules are skipped and that response rules are not mistakenly applied to requests.

## API

### public void ApplyRequestRules_AddHeader_AppendsHeaderWhenMissing
Tests that when a request lacks a specific header, an "Add" rule appends it with the configured value. The resulting request contains the new header alongside any pre-existing headers.

### public void ApplyRequestRules_AddHeader_DoesNotOverwriteExistingHeader
Verifies that an "Add" rule leaves an already-present header untouched. The original value remains, and no duplicate is introduced.

### public void ApplyRequestRules_SetHeader_ReplacesExistingHeader
Confirms that a "Set" rule overwrites the value of an existing header. After application, the header reflects the rule’s configured value rather than the original.

### public void ApplyRequestRules_RemoveHeader_DeletesHeader
Ensures that a "Remove" rule strips a specified header entirely from the request. The header is absent in the transformed request.

### public void ApplyRequestRules_SetQueryParam_AppendsParamToUri
Validates that a "Set" rule for a query parameter adds the parameter to the request URI’s query string. If the parameter is absent, it is appended; if present, it is overwritten.

### public void ApplyRequestRules_RemoveQueryParam_DropsParamFromUri
Checks that a "Remove" rule targeting a query parameter eliminates it from the request URI. The resulting URI no longer contains that parameter.

### public void ApplyRequestRules_RewritePathPrefix_ReplacesMatchingPrefix
Tests path-rewriting logic: when the request path starts with a configured prefix, that prefix is replaced with the rule’s target value. Non-matching paths are left unchanged.

### public void ApplyRequestRules_DisabledRule_IsSkipped
Confirms that a transformation rule marked as disabled is ignored during request processing. The request passes through without the rule’s effect.

### public void ApplyRequestRules_ResponsePhaseRule_IsNotAppliedToRequest
Ensures that rules designated for the response phase are not executed against the outgoing request. Only request-phase rules affect the request.

### public void ApplyResponseRules_SetHeader_InjectsHeaderIntoResponse
Verifies that a response-phase "Set" rule adds or overwrites a header in the response. The downstream client receives the injected header.

### public void ApplyResponseRules_RemoveHeader_DeletesHeaderFromResponse
Tests that a response-phase "Remove" rule deletes a specified header from the response before it reaches the client.

### public void ApplyRequestRules_OrderedRules_AreAppliedInOrder
Validates that when multiple request rules are configured, they execute in the defined order. The final request state reflects sequential application, not arbitrary ordering.

### public void TransformationRule_Validate_ThrowsWhenKeyEmpty
Ensures that validation throws an appropriate exception when a rule’s key (e.g., header name or query parameter name) is null or empty.

### public void TransformationRule_Validate_ThrowsWhenValueMissingForAdd
Confirms that validation rejects an "Add" or "Set" rule that lacks a required value. An exception is thrown to prevent misconfiguration.

### public void TransformationRule_Validate_DoesNotThrowForRemove
Verifies that a "Remove" rule passes validation without a value, since removal operations do not require one.

## Usage

```csharp
// Example 1: Applying request rules in sequence and asserting the outcome
var service = new RequestTransformationService();
var rules = new List<TransformationRule>
{
    new() { Phase = RulePhase.Request, Action = RuleAction.AddHeader, Key = "X-Correlation-Id", Value = "abc-123" },
    new() { Phase = RulePhase.Request, Action = RuleAction.SetQueryParam, Key = "filter", Value = "active" }
};

var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
request.Headers.Add("X-Existing", "keep-me");

service.ApplyRequestRules(request, rules);

// request now contains "X-Correlation-Id: abc-123", "X-Existing: keep-me",
// and the URI has "?filter=active" appended.
```

```csharp
// Example 2: Validating rules before use and handling response transformations
var rule = new TransformationRule
{
    Phase = RulePhase.Response,
    Action = RuleAction.SetHeader,
    Key = "X-RateLimit-Remaining",
    Value = "42"
};

// Validate before deployment
TransformationRule.Validate(rule); // succeeds

var response = new HttpResponseMessage();
service.ApplyResponseRules(response, new[] { rule });

// response now carries the X-RateLimit-Remaining header with value "42".
```

## Notes

- **Rule ordering**: Request rules are applied in the order they appear in the collection. A later rule can overwrite or remove modifications made by an earlier rule. Maintain rule sequences carefully to avoid unintended interactions.
- **Phase isolation**: Response-phase rules are never applied to requests, and request-phase rules are never applied to responses. Passing a rule with the wrong phase to a method results in it being silently skipped.
- **Disabled rules**: A rule with its disabled flag set to `true` is ignored entirely during both request and response processing. Validation may still be performed on disabled rules if invoked explicitly.
- **Validation**: `TransformationRule.Validate` throws on misconfiguration (empty key, missing value for Add/Set) but does not throw for Remove rules that omit a value. Callers should validate rules at configuration time to fail fast.
- **Thread safety**: The transformation methods operate on the provided `HttpRequestMessage` or `HttpResponseMessage` instances and do not share mutable state across invocations. They are safe to call concurrently as long as distinct message objects are supplied.
