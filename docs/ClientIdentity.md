# ClientIdentity

Represents the authenticated identity of a client in the API gateway. It encapsulates the core identity attributes (ID, subject, name, email), authorization metadata (scopes, roles, claims), and temporal validity (issued at, expires at). Instances are typically created by the gateway’s authentication middleware and made available to downstream handlers for authorization decisions, logging, or user context propagation.

## API

### Properties

- **`public string Id`**  
  The unique identifier of the client (e.g., a client ID from an OAuth2 client registration). This value is always present and non-null.

- **`public string? Subject`**  
  The subject claim of the identity, often representing the user or service principal. May be `null` if the identity is anonymous or the subject was not provided.

- **`public string? Name`**  
  A human-readable display name for the client or user. May be `null`.

- **`public string? Email`**  
  The email address associated with the identity. May be `null`.

- **`public string[] Scopes`**  
  The set of OAuth2 scopes granted to this identity. Returns an empty array if no scopes are present. The array is never `null`.

- **`public string[] Roles`**  
  The set of roles assigned to this identity. Returns an empty array if no roles are present. The array is never `null`.

- **`public Dictionary<string, object> Claims`**  
  A dictionary of additional claims associated with the identity. Keys are claim names (case-sensitive), values are the claim data. The dictionary is never `null` but may be empty.

- **`public DateTime? ExpiresAt`**  
  The UTC timestamp when this identity expires, or `null` if the identity does not expire.

- **`public DateTime IssuedAt`**  
  The UTC timestamp when this identity was issued.

- **`public bool IsExpired`**  
  Returns `true` if the identity has expired (i.e., `ExpiresAt` is not null and the current UTC time is later than `ExpiresAt`); otherwise `false`. If `ExpiresAt` is null, this property always returns `false`.

### Methods

- **`public bool HasScope(string scope)`**  
  Determines whether the identity possesses the specified scope.  
  **Parameters:** `scope` – the scope name to check.  
  **Returns:** `true` if `Scopes` contains the given scope (case-sensitive); otherwise `false`.  
  **Throws:** `ArgumentNullException` if `scope` is `null`.

- **`public bool HasRole(string role)`**  
  Determines whether the identity possesses the specified role.  
  **Parameters:** `role` – the role name to check.  
  **Returns:** `true` if `Roles` contains the given role (case-sensitive); otherwise `false`.  
  **Throws:** `ArgumentNullException` if `role` is `null`.

- **`public bool HasAnyScopeOf(params string[] scopes)`**  
  Determines whether the identity possesses at least one of the specified scopes.  
  **Parameters:** `scopes` – one or more scope names to check.  
  **Returns:** `true` if any of the given scopes is present in `Scopes`; otherwise `false`.  
  **Throws:** `ArgumentNullException` if `scopes` is `null` or any element is `null`.

- **`public bool HasAllScopesOf(params string[] scopes)`**  
  Determines whether the identity possesses all of the specified scopes.  
  **Parameters:** `scopes` – one or more scope names to check.  
  **Returns:** `true` if every given scope is present in `Scopes`; otherwise `false`.  
  **Throws:** `ArgumentNullException` if `scopes` is `null` or any element is `null`.

- **`public bool HasAnyRoleOf(params string[] roles)`**  
  Determines whether the identity possesses at least one of the specified roles.  
  **Parameters:** `roles` – one or more role names to check.  
  **Returns:** `true` if any of the given roles is present in `Roles`; otherwise `false`.  
  **Throws:** `ArgumentNullException` if `roles` is `null` or any element is `null`.

- **`public T? GetClaim<T>(string key)`**  
  Retrieves a claim value by its key and attempts to cast it to the specified type.  
  **Type parameter:** `T` – the expected type of the claim value.  
  **Parameters:** `key` – the claim name (case-sensitive).  
  **Returns:** The claim value cast to `T`, or `default(T)` if the claim does not exist or cannot be cast to `T`.  
  **Throws:** `ArgumentNullException` if `key` is `null`.

## Usage

### Example 1: Basic authorization check

```csharp
public IActionResult GetSensitiveData(ClientIdentity identity)
{
    if (identity.IsExpired)
        return Unauthorized("Identity has expired.");

    if (!identity.HasScope("data:read"))
        return Forbid("Insufficient scope.");

    if (!identity.HasRole("admin"))
        return Forbid("Admin role required.");

    return Ok($"Hello, {identity.Name ?? "anonymous"}.");
}
```

### Example 2: Using claims and scope checks

```csharp
public IActionResult ProcessOrder(ClientIdentity identity, OrderRequest request)
{
    // Check that the identity has all required scopes
    if (!identity.HasAllScopesOf("orders:write", "payments:process"))
        return Forbid("Missing required scopes.");

    // Retrieve a custom claim
    var tenantId = identity.GetClaim<string>("tenant_id");
    if (tenantId == null)
        return BadRequest("Tenant claim missing.");

    // Verify at least one of the allowed roles
    if (!identity.HasAnyRoleOf("manager", "supervisor"))
        return Forbid("Insufficient role.");

    return Ok($"Order processed for tenant {tenantId}.");
}
```

## Notes

- **Immutability and thread safety:** `ClientIdentity` instances are designed to be immutable after construction. All properties return fixed values, and the collections (`Scopes`, `Roles`, `Claims`) are read-only copies. This makes instances inherently thread-safe and suitable for caching or sharing across concurrent requests.
- **Null handling:** Properties that are nullable (`Subject`, `Name`, `Email`, `ExpiresAt`) may be `null` when the corresponding information was not provided during authentication. The `Scopes`, `Roles`, and `Claims` collections are never `null` but may be empty. All methods throw `ArgumentNullException` if a required string parameter is `null`.
- **Case sensitivity:** Scope, role, and claim key comparisons are case-sensitive. Ensure that the values used in checks match the exact casing used during authentication.
- **Expiration:** The `IsExpired` property evaluates expiration at the time of access. It does not cache the result; repeated calls may return different values as time passes. For consistent behavior across a single request, evaluate `IsExpired` once and store the result if needed.
- **GetClaim<T> behavior:** The method returns `default(T)` (e.g., `null` for reference types, `0` for numeric types) when the claim key is missing or the stored value cannot be cast to `T`. It does not throw an exception for type mismatches. To distinguish between a missing claim and a `null` claim value, use a nullable reference type or check `Claims.ContainsKey(key)` first.
