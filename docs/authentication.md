# Authentication

This document describes how the gateway authenticates incoming requests: the
JWT Bearer model, the configuration surface, and the current state of runtime
enforcement.

> **Status:** The JWT validation building blocks exist (policy model, validation
> service, configuration options), but the request pipeline does not yet call
> them. Until that wiring is added, route-level authentication is **not enforced**
> at runtime. See [Current limitations](#current-limitations).

## Components

| Component | Location | Role |
|---|---|---|
| `AuthenticationPolicy` | `Models/AuthenticationPolicy.cs` | Per-route auth requirements (enabled flag, issuer, audience, secret, algorithms, scopes, roles, clock skew) |
| `GatewayRoute.AuthenticationPolicy` | `Models/GatewayRoute.cs` | Optional policy attached to a route |
| `JwtValidationService` | `Services/JwtValidationService.cs` | Validates a Bearer token against a policy and returns a `ClientIdentity` |
| `ClientIdentity` | `Models/` | Identity extracted from the token: subject, name, email, scopes, roles, `IssuedAt` |
| `AuthenticationException` | `Exceptions/AuthenticationException.cs` | Thrown on any authentication failure; maps to HTTP 401 with error code `UNAUTHORIZED` |
| `JwtValidationOptions` | `Configuration/DotnetApiGatewayOptions.cs` | Global JWT settings under the `DotnetApiGateway:JwtValidation` section |
| `GatewayConstants` | `Constants/GatewayConstants.cs` | `Bearer` scheme name, default clock skew (60 s), `Authorization` header name, `UNAUTHORIZED` code |

`JwtValidationService` is registered as a singleton in
`Configuration/ServiceCollectionExtensions.cs`.

## Request flow (intended)

1. `GatewayMiddleware` copies the raw `Authorization` header into
   `RequestContext.AuthToken` and stores the context in `HttpContext.Items`.
2. For a route with an enabled `AuthenticationPolicy`, the token is taken from
   the `Authorization: Bearer <token>` header. `HeaderUtility.ExtractBearerToken`
   provides this extraction; nothing calls it in the pipeline yet.
3. `JwtValidationService.ValidateTokenAsync(token, policy)` validates the token
   and returns a `ClientIdentity`.
4. Any failure throws `AuthenticationException` (HTTP 401). Scope and role
   checks, when configured, are applied to the returned identity.

Steps 2 to 4 are the intended design. Only the service and the policy model
exist in the codebase today. See [Current limitations](#current-limitations).

## Authentication policy

`AuthenticationPolicy` properties:

| Property | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Turns authentication on for the route |
| `Type` | `Bearer` | Only `Bearer` is validated by `JwtValidationService` |
| `JwtIssuer` | `null` | Expected `iss` claim. When set, issuer is validated |
| `JwtAudience` | `null` | Expected `aud` claim. When set, audience is validated |
| `JwtSecret` | `null` | Symmetric key (UTF-8 bytes) used as `IssuerSigningKey` |
| `JwtAlgorithms` | `["HS256"]` | Allowed signing algorithms. Must contain at least one entry |
| `AllowedScopes` | `[]` | Scopes the client must have (see note below) |
| `AllowedRoles` | `[]` | Roles the client must have (see note below) |
| `ValidateExpiration` | `true` | Validates `nbf` and `exp` |
| `ValidateSignature` | `true` | Verifies the token signature |
| `ClockSkewSeconds` | `60` | Tolerance applied to `exp`. Must be between 0 and 300 |

`AuthenticationPolicy.Validate()` enforces these rules when `Enabled` is true:

- Bearer policies need `JwtSecret` or `JwtIssuer`.
- `JwtAlgorithms` must not be empty.
- `ClockSkewSeconds` must be in the range 0 to 300.

### Validation behaviour

`JwtValidationService` builds `TokenValidationParameters` from the policy:

- Issuer and audience are checked only when `JwtIssuer` / `JwtAudience` are set.
- `exp` is checked with `ClockSkewSeconds` added to the expiry. `nbf` gets a
  fixed 1-second tolerance. `ClockSkew` itself is set to zero, so the custom
  lifetime validator is the only place skew is applied.
- If `ValidateSignature` is `false`, the signature is not verified. The token is
  only parsed. Use this for local testing only.
- Claims are read from the raw JWT payload, not the mapped `ClaimsPrincipal`.
  This keeps short names such as `sub`, `scope`, and `role` working.

Claim mapping used by `ExtractClientIdentity`:

| `ClientIdentity` field | Claims read |
|---|---|
| `Id` | `sub`, else `nameid`, else a new GUID |
| `Subject` | `sub` |
| `Name` | `name`, `unique_name`, `ClaimTypes.Name` |
| `Email` | `email`, `ClaimTypes.Email` |
| `Scopes` | `scope` or `scp`, space-separated |
| `Roles` | `role`, `roles`, `ClaimTypes.Role` (multi-valued) |
| `IssuedAt` | `iat` |

## Configuration

Global settings live under `DotnetApiGateway:JwtValidation`:

```json
{
  "DotnetApiGateway": {
    "JwtValidation": {
      "Enabled": true,
      "Issuer": "https://auth.example.com/",
      "Audience": "api-gateway",
      "SecretKey": "<set via environment or secret store>"
    }
  }
}
```

`DotnetApiGatewayOptions.Validate` requires `SecretKey` or `Issuer` when
`JwtValidation.Enabled` is true.

Do not commit real secrets. Supply `SecretKey` through an environment variable
(`DotnetApiGateway__JwtValidation__SecretKey`) or a secret store.

## Current limitations

These are gaps between the design and the code as it stands:

1. **No runtime enforcement.** `ValidateTokenAsync` has no callers outside
   tests and examples. Routes with `AuthenticationPolicy.Enabled = true` are
   forwarded without token checks.
2. **Global options are not used.** `JwtValidationOptions` is bound but nothing
   reads it to build an `AuthenticationPolicy`.
3. **`AllowedScopes` and `AllowedRoles` are not checked.** They are stored and
   reported by `HasScopeRequirements()` / `HasRoleRequirements()`, but no code
   compares them against `ClientIdentity.Scopes` or `ClientIdentity.Roles`.
4. **`JwtAlgorithms` is not applied.** The policy stores the list, but
   `BuildValidationParameters` does not restrict the accepted algorithms.

Until items 1 to 3 are resolved, do not rely on the gateway for authorization.
Enforce authentication in the upstream services, or keep the gateway's
authentication routes behind a network boundary.

## Error responses

Authentication failures throw `AuthenticationException` (error code
`UNAUTHORIZED`, HTTP `401`). `ExceptionMappingMiddleware` catches it and writes
the response through `WriteErrorResponse`. Check that file for the exact body
shape.

## Logging

`RequestLoggingMiddleware` treats `Authorization`, `X-API-Key`, and `Cookie` as
sensitive headers. Do not log raw tokens when adding new logging around auth.

## Testing

- Unit tests: `tests/dotnet-api-gateway.Tests/JwtValidationServiceTests.cs`
  with helpers in `JwtValidationServiceTestsExtensions.cs`. These cover
  expired tokens, `nbf`, issuer and audience mismatch, and signature failures.
- Test helpers `CreateTestToken` and `CreateTestPolicy` (in
  `JwtValidationServiceTestsExtensions.cs`) generate HS256-signed tokens with a
  fixed test secret.
- Example: `examples/JwtAuthenticationExample.cs`.

Run the tests with:

```bash
dotnet test --filter "FullyQualifiedName~JwtValidationServiceTests"
```

## Related documents

- [Configuration reference](configuration-reference.md)
- [Architecture](ARCHITECTURE.md)
- [Per-component notes](AuthenticationPolicy.md)
