# AuthenticationPolicy

The `AuthenticationPolicy` class defines the configuration and requirements for authenticating requests within the API Gateway. It encapsulates settings for various authentication mechanisms, primarily focusing on JWT validation parameters such as issuer, audience, secrets, and algorithms, while also specifying authorization constraints like required scopes and roles. This object serves as the central definition for determining whether a request meets the security criteria for a specific route or service.

## API

### `Id`
```csharp
public string Id { get; set; }
```
Gets or sets the unique identifier for this authentication policy. This value is used to reference the policy within gateway configuration files or routing rules.

### `Enabled`
```csharp
public bool Enabled { get; set; }
```
Gets or sets a value indicating whether this policy is currently active. If set to `false`, the gateway will bypass authentication checks associated with this policy.

### `Type`
```csharp
public AuthenticationType Type { get; set; }
```
Gets or sets the specific authentication mechanism employed by this policy (e.g., JWT, API Key, OAuth2). The value determines which other properties are relevant for validation.

### `JwtIssuer`
```csharp
public string? JwtIssuer { get; set; }
```
Gets or sets the expected issuer (`iss`) claim for JWT tokens. If populated, the token's issuer must match this string exactly during validation.

### `JwtAudience`
```csharp
public string? JwtAudience { get; set; }
```
Gets or sets the expected audience (`aud`) claim for JWT tokens. Validation fails if the token does not contain this audience value.

### `JwtSecret`
```csharp
public string? JwtSecret { get; set; }
```
Gets or sets the secret key used to verify the signature of HMAC-based JWTs. This property should be populated securely and never logged.

### `JwtAlgorithms`
```csharp
public string[] JwtAlgorithms { get; set; }
```
Gets or sets the list of allowed signing algorithms (e.g., "HS256", "RS256"). Tokens signed with an algorithm not present in this array will be rejected.

### `AllowedScopes`
```csharp
public string[] AllowedScopes { get; set; }
```
Gets or sets the list of OAuth2 scopes required for access. A valid token must contain at least one of these scopes if the list is not empty.

### `AllowedRoles`
```csharp
public string[] AllowedRoles { get; set; }
```
Gets or sets the list of user roles permitted to access the resource. A valid token must assert at least one of these roles if the list is not empty.

### `ValidateExpiration`
```csharp
public bool ValidateExpiration { get; set; }
```
Gets or sets a value indicating whether the token's expiration time (`exp`) claim should be enforced. If `true`, expired tokens are rejected.

### `ValidateSignature`
```csharp
public bool ValidateSignature { get; set; }
```
Gets or sets a value indicating whether the cryptographic signature of the token must be verified. Disabling this is generally unsafe for production environments.

### `ClockSkewSeconds`
```csharp
public int ClockSkewSeconds { get; set; }
```
Gets or sets the allowable time discrepancy in seconds between the server clock and the token issuer's clock. This prevents rejection of valid tokens due to minor time synchronization differences.

### `Validate()`
```csharp
public void Validate()
```
Validates the internal consistency of the policy configuration.
*   **Purpose**: Ensures that required fields are populated based on the selected `Type` and that conflicting settings are not present.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: Throws `InvalidOperationException` if the configuration is invalid (e.g., `ValidateSignature` is true but `JwtSecret` is null for symmetric algorithms).

### `RequiresAuthentication`
```csharp
public bool RequiresAuthentication { get; }
```
Gets a value indicating whether this policy enforces authentication. Returns `true` if the policy is enabled and configured to require valid credentials; otherwise, returns `false`.

### `HasScopeRequirements`
```csharp
public bool HasScopeRequirements { get; }
```
Gets a value indicating whether the policy imposes specific scope constraints. Returns `true` if `AllowedScopes` contains one or more entries.

### `HasRoleRequirements`
```csharp
public bool HasRoleRequirements { get; }
```
Gets a value indicating whether the policy imposes specific role constraints. Returns `true` if `AllowedRoles` contains one or more entries.

## Usage

### Example 1: Configuring a Standard JWT Policy
This example demonstrates initializing a policy for a standard HMAC-SHA256 signed JWT with specific issuer and audience constraints.

```csharp
var policy = new AuthenticationPolicy
{
    Id = "user-service-jwt",
    Enabled = true,
    Type = AuthenticationType.Jwt,
    JwtIssuer = "https://auth.example.com",
    JwtAudience = "api-gateway",
    JwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET"),
    JwtAlgorithms = new[] { "HS256" },
    ValidateExpiration = true,
    ValidateSignature = true,
    ClockSkewSeconds = 60,
    AllowedScopes = new[] { "read:users", "write:users" },
    AllowedRoles = Array.Empty<string>()
};

// Ensure configuration is valid before applying
policy.Validate();

if (policy.HasScopeRequirements)
{
    Console.WriteLine($"Policy requires one of the following scopes: {string.Join(", ", policy.AllowedScopes)}");
}
```

### Example 2: Conditional Logic Based on Policy Requirements
This example shows how to inspect policy properties to determine processing logic without manually checking array counts.

```csharp
public async Task<bool> AuthorizeRequestAsync(AuthenticationPolicy policy, ClaimsPrincipal user)
{
    if (!policy.Enabled || !policy.RequiresAuthentication)
    {
        return true; // No auth required
    }

    if (policy.HasRoleRequirements)
    {
        bool hasRole = policy.AllowedRoles.Any(role => user.IsInRole(role));
        if (!hasRole) return false;
    }

    if (policy.HasScopeRequirements)
    {
        // Custom scope validation logic
        var userScopes = user.FindAll("scope").Select(c => c.Value);
        bool hasScope = policy.AllowedScopes.Any(s => userScopes.Contains(s));
        if (!hasScope) return false;
    }

    return true;
}
```

## Notes

*   **Configuration Consistency**: The `Validate()` method must be called after populating the object and before using it in the request pipeline. It ensures that properties like `JwtSecret` are present when `ValidateSignature` is enabled, preventing runtime failures during token verification.
*   **Thread Safety**: Instances of `AuthenticationPolicy` are mutable. While the properties themselves are simple types or arrays, the object is not thread-safe for concurrent modification. It is recommended to configure the policy once during application startup and treat it as read-only during request processing. If arrays (`JwtAlgorithms`, `AllowedScopes`, `AllowedRoles`) need to be modified at runtime, external locking mechanisms must be used.
*   **Nullable Handling**: Properties `JwtIssuer`, `JwtAudience`, and `JwtSecret` are nullable. Logic consuming these properties must account for `null` values, particularly when `Type` is set to a non-JWT mechanism where these fields may remain unset.
*   **Empty Arrays vs Null**: The boolean helpers `HasScopeRequirements` and `HasRoleRequirements` rely on the contents of their respective arrays. An empty array results in `false`, whereas a populated array results in `true`. Ensure arrays are initialized to empty collections rather than `null` to avoid `NullReferenceException` when accessing `AllowedScopes` or `AllowedRoles` directly.
