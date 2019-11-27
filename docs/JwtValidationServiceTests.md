# JwtValidationServiceTests

Unit tests for `JwtValidationService`, validating JWT token processing behavior including signature verification, issuer/audience validation, expiration checks, scope/role extraction, and error handling scenarios.

## API

### `ValidateTokenAsync_ValidToken_ReturnsClientIdentity`
Validates a correctly formatted and signed JWT token, ensuring the service returns a populated `ClientIdentity` with claims extracted from the token.

### `ValidateTokenAsync_TokenWithScopes_ExtractsScopesCorrectly`
Validates that a JWT token containing `scope` claims is correctly parsed and the scopes are extracted into the `ClientIdentity.Scopes` collection.

### `ValidateTokenAsync_TokenWithRoles_ExtractsRolesCorrectly`
Validates that a JWT token containing `role` claims is correctly parsed and the roles are extracted into the `ClientIdentity.Roles` collection.

### `ValidateTokenAsync_DisabledPolicy_ThrowsAuthenticationException`
Ensures that when token validation policies (e.g., issuer, audience, signature) are disabled, the method still throws `AuthenticationException` due to missing required claims or invalid structure.

### `ValidateTokenAsync_EmptyToken_ThrowsAuthenticationException`
Verifies that passing an empty string as the JWT token results in an `AuthenticationException` being thrown.

### `ValidateTokenAsync_WhitespaceToken_ThrowsAuthenticationException`
Confirms that a token consisting only of whitespace triggers an `AuthenticationException`.

### `ValidateTokenAsync_InvalidTokenSignature_ThrowsAuthenticationException`
Validates that a token with an invalid or malformed signature is rejected with an `AuthenticationException`.

### `ValidateTokenAsync_ExpiredToken_ThrowsAuthenticationException`
Ensures that a token with an `exp` claim in the past is rejected with an `AuthenticationException`.

### `ValidateTokenAsync_FutureToken_ThrowsAuthenticationException`
Confirms that a token with an `exp` claim set to a future time exceeding acceptable clock skew is still rejected if the skew threshold is not configured.

### `ValidateTokenAsync_InvalidIssuer_ThrowsAuthenticationException`
Validates that a token with an `iss` claim not matching the expected issuer is rejected with an `AuthenticationException`.

### `ValidateTokenAsync_InvalidAudience_ThrowsAuthenticationException`
Ensures that a token with an `aud` claim not matching the expected audience is rejected with an `AuthenticationException`.

### `DecodeToken_ValidToken_ReturnsJwtToken`
Tests that a valid JWT string can be decoded into a `JwtSecurityToken` without validation, returning the parsed token structure.

### `DecodeToken_InvalidTokenFormat_ThrowsAuthenticationException`
Confirms that a malformed or non-JWT string passed to `DecodeToken` results in an `AuthenticationException`.

### `ValidateTokenAsync_NoSignatureValidation_SkipsSignatureCheck`
Validates that when signature validation is disabled via policy, the method skips signature verification and processes the token successfully.

### `ValidateTokenAsync_WithClockSkew_AcceptsNearExpiredToken`
Ensures that a token with an `exp` claim slightly in the past (within configured clock skew) is accepted as valid.

### `ValidateTokenAsync_NoIssuerValidation_SkipsIssuerCheck`
Validates that when issuer validation is disabled via policy, the method skips issuer verification and processes the token successfully.

### `ValidateTokenAsync_GeneratesIdFromNameIdIfNoSub_UsesNameId`
Ensures that when a token lacks a `sub` claim, the `nameid` claim is used as the unique identifier in the returned `ClientIdentity`.

### `ValidateTokenAsync_WrongAuthType_ThrowsAuthenticationException`
Confirms that a token not intended for this service (e.g., wrong `aud` or `azp`) is rejected with an `AuthenticationException`.

## Usage

### Example 1: Validating a JWT with Scopes and Roles
