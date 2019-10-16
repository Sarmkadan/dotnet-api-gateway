# Configuration Reference

This document provides a comprehensive reference for configuring the .NET API Gateway via `appsettings.json` or environment variables.

## Table of Contents

-   [Gateway Configuration](#gateway-configuration)
-   [Route Configuration](#route-configuration)
    -   [Authentication Policy](#authentication-policy)
    -   [Cache Policy](#cache-policy)
    -   [Circuit Breaker Policy](#circuit-breaker-policy)
    -   [Rate Limit Policy](#rate-limit-policy)
    -   [Request Coalescing Policy](#request-coalescing-policy)

## Gateway Configuration

These settings are defined under the `"Gateway"` section in `appsettings.json` and control the overall behavior of the API Gateway.

```json
{
  "Gateway": {
    "ApplicationName": "DotNetApiGateway",
    "Version": "1.0.0",
    "MaxRequestBodySize": 10485760, // 10 * 1024 * 1024 bytes (10 MB)
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentRequests": 100,
    "EnableCors": true,
    "EnableCompression": true,
    "EnableLogging": true,
    "LogLevel": "Information",
    "EnableMetrics": true,
    "EnableHealthCheck": true,
    "HealthCheckPath": "/health"
  }
}
```

| Key                     | Type     | Default Value              | Description                                                                                                                                                 |
| :---------------------- | :------- | :------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ApplicationName`       | `string` | `"DotNetApiGateway"`       | The name of the API Gateway application. Used for logging and metrics.                                                                                      |
| `Version`               | `string` | `"1.0.0"`                  | The version of the API Gateway.                                                                                                                             |
| `MaxRequestBodySize`    | `int`    | `10485760` (10 MB)         | Maximum allowed size for an incoming request body in bytes. Requests exceeding this limit will be rejected.                                                 |
| `DefaultTimeoutSeconds` | `int`    | `30`                       | Default timeout in seconds for upstream HTTP requests. Can be overridden per route. (Min: 1, Max: 300)                                                    |
| `MaxConcurrentRequests` | `int`    | `100`                      | Maximum number of concurrent requests the gateway will process. Additional requests will be queued or rejected. (Min: 1)                                    |
| `EnableCors`            | `bool`   | `true`                     | Enables Cross-Origin Resource Sharing (CORS) globally for the gateway.                                                                                      |
| `EnableCompression`     | `bool`   | `true`                     | Enables HTTP response compression (e.g., GZIP) for suitable responses.                                                                                      |
| `EnableLogging`         | `bool`   | `true`                     | Enables or disables general logging for the gateway.                                                                                                        |
| `LogLevel`              | `string` | `"Information"`            | The minimum logging level to output (e.g., "Trace", "Debug", "Information", "Warning", "Error", "Critical").                                             |
| `EnableMetrics`         | `bool`   | `true`                     | Enables or disables the collection and exposition of operational metrics.                                                                                   |
| `EnableHealthCheck`     | `bool`   | `true`                     | Enables or disables the built-in health check endpoint.                                                                                                     |
| `HealthCheckPath`       | `string` | `"/health"`                | The URL path for the health check endpoint.                                                                                                                 |

## Route Configuration

Routes are defined as a list under the `"Routes"` section in `appsettings.json`. Each route specifies how incoming requests are matched, processed, and forwarded to upstream services.

```json
{
  "Routes": [
    {
      "Id": "my-first-route",
      "Path": "/api/users/{id}",
      "Methods": ["GET"],
      "TargetUrl": "http://localhost:5001/users/{id}",
      "AuthenticationPolicy": { ... }, // See Authentication Policy
      "CachePolicy": { ... },          // See Cache Policy
      "CircuitBreakerPolicy": { ... }, // See Circuit Breaker Policy
      "RateLimitPolicy": { ... },      // See Rate Limit Policy
      "RequestCoalescingPolicy": { ... } // See Request Coalescing Policy
    }
  ]
}
```

| Key                      | Type                      | Description                                                                                                                                                                                                                                                        |
| :----------------------- | :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Id`                     | `string`                  | A unique identifier for the route.                                                                                                                                                                                                                                 |
| `Path`                   | `string`                  | The incoming URL path pattern to match (e.g., `/api/users/{id}`). Can include path parameters.                                                                                                                                                                     |
| `Methods`                | `string[]`                | An array of HTTP methods (e.g., `["GET", "POST"]`) that this route will match. If empty, all methods are matched.                                                                                                                                                  |
| `TargetUrl`              | `string`                  | The URL of the upstream service to forward the request to. Path parameters from the `Path` can be used here (e.g., `http://localhost:5001/users/{id}`).                                                                                                               |
| `AuthenticationPolicy`   | `AuthenticationPolicy`    | (Optional) Configuration for JWT validation and authorization for this specific route.                                                                                                                                                                             |
| `CachePolicy`            | `CachePolicy`             | (Optional) Configuration for caching responses for this route.                                                                                                                                                                                                     |
| `CircuitBreakerPolicy`   | `CircuitBreakerPolicy`    | (Optional) Configuration for applying the circuit breaker pattern to the upstream calls made by this route.                                                                                                                                                        |
| `RateLimitPolicy`        | `RateLimitPolicy`         | (Optional) Configuration for applying rate limiting to requests matching this route.                                                                                                                                                                               |
| `RequestCoalescingPolicy`| `RequestCoalescingPolicy` | (Optional) Configuration for coalescing duplicate concurrent requests for this route. Ensures only one upstream call is made for identical requests arriving simultaneously.                                                                                           |

---

### Authentication Policy

Configured within a `GatewayRoute` under the `"AuthenticationPolicy"` section.

```json
{
  "AuthenticationPolicy": {
    "Enabled": true,
    "Type": "Bearer",
    "JwtIssuer": "your-auth-server",
    "JwtAudience": "your-api",
    "JwtSecret": "your-super-secret-key-that-is-at-least-256-bits",
    "JwtAlgorithms": ["HS256"],
    "AllowedScopes": ["read:users", "write:users"],
    "AllowedRoles": ["admin", "user"],
    "ValidateExpiration": true,
    "ValidateSignature": true,
    "ClockSkewSeconds": 60
  }
}
```

| Key                | Type       | Default Value                            | Description                                                                                                                                                                                                                                                                |
| :----------------- | :--------- | :--------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`          | `bool`     | `false`                                  | Enables or disables JWT authentication for this route.                                                                                                                                                                                                                     |
| `Type`             | `enum`     | `Bearer`                                 | The type of authentication. Currently, only `Bearer` (JWT) is supported.                                                                                                                                                                                                   |
| `JwtIssuer`        | `string`   | `null`                                   | The expected issuer of the JWT. Required if `JwtSecret` is not provided.                                                                                                                                                                                                   |
| `JwtAudience`      | `string`   | `null`                                   | The expected audience of the JWT.                                                                                                                                                                                                                                          |
| `JwtSecret`        | `string`   | `null`                                   | The secret key used to sign the JWT. Required if `JwtIssuer` is not provided. Must be at least 256 bits (32 bytes) for HS256.                                                                                                                                               |
| `JwtAlgorithms`    | `string[]` | `["HS256"]`                              | An array of allowed JWT signing algorithms (e.g., `["HS256", "RS256"]`).                                                                                                                                                                                                   |
| `AllowedScopes`    | `string[]` | `[]`                                     | An array of scopes. The JWT must contain at least one of these scopes for the request to be authorized.                                                                                                                                                                    |
| `AllowedRoles`     | `string[]` | `[]`                                     | An array of roles. The JWT must contain at least one of these roles for the request to be authorized.                                                                                                                                                                      |
| `ValidateExpiration`| `bool`     | `true`                                   | Validates the `exp` (expiration) and `nbf` (not before) claims of the JWT. If `false`, tokens will not be checked for validity period.                                                                                                                                      |
| `ValidateSignature`| `bool`     | `true`                                   | Validates the cryptographic signature of the JWT.                                                                                                                                                                                                                          |
| `ClockSkewSeconds` | `int`      | `60`                                     | The allowable clock skew in seconds when validating `exp` and `nbf` claims. (Between 0 and 300)                                                                                                                                                                         |

---

### Cache Policy

Configured within a `GatewayRoute` under the `"CachePolicy"` section.

```json
{
  "CachePolicy": {
    "Enabled": true,
    "DurationSeconds": 300,
    "Strategy": "CacheControl", // or "InMemory"
    "CacheableStatusCodes": ["200"],
    "CacheableHttpMethods": ["GET", "HEAD"],
    "VaryByQueryString": true,
    "VaryByHeaders": false,
    "VaryHeaders": [],
    "MaxEntriesInCache": 1000
  }
}
```

| Key                  | Type       | Default Value   | Description                                                                                                                                                                    |
| :------------------- | :--------- | :-------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`            | `bool`     | `false`         | Enables or disables response caching for this route.                                                                                                                           |
| `DurationSeconds`    | `int`      | `300`           | The duration in seconds that a response should be cached. (Between 1 and 3600)                                                                                                 |
| `Strategy`           | `enum`     | `CacheControl`  | The caching strategy to use. `CacheControl` uses HTTP cache headers. `InMemory` uses an in-memory store.                                                                      |
| `CacheableStatusCodes`| `string[]` | `["200"]`       | An array of HTTP status codes for which responses can be cached.                                                                                                               |
| `CacheableHttpMethods`| `string[]` | `["GET", "HEAD"]`| An array of HTTP methods for which responses can be cached.                                                                                                                    |
| `VaryByQueryString`  | `bool`     | `true`          | If `true`, the cache key will include query string parameters.                                                                                                                 |
| `VaryByHeaders`      | `bool`     | `false`         | If `true`, the cache key will include specified `VaryHeaders`.                                                                                                                 |
| `VaryHeaders`        | `string[]` | `[]`            | An array of HTTP header names to include in the cache key if `VaryByHeaders` is `true`.                                                                                        |
| `MaxEntriesInCache`  | `int`      | `1000`          | Maximum number of entries to store in the in-memory cache for this policy. (Between 1 and 10000)                                                                               |

---

### Circuit Breaker Policy

Configured within a `GatewayRoute` under the `"CircuitBreakerPolicy"` section.

```json
{
  "CircuitBreakerPolicy": {
    "Enabled": true,
    "FailureThreshold": 5,
    "SuccessThreshold": 2,
    "TimeoutSeconds": 60,
    "FailureStatusCodes": [500, 502, 503, 504],
    "MaxRetries": 3,
    "RetryDelayMilliseconds": 100
  }
}
```

| Key                    | Type     | Default Value        | Description                                                                                                                                                           |
| :--------------------- | :------- | :------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`              | `bool`   | `true`               | Enables or disables the circuit breaker for this route.                                                                                                               |
| `FailureThreshold`     | `int`    | `5`                  | The number of consecutive failures that will trip the circuit to the "Open" state. (Min: 1)                                                                           |
| `SuccessThreshold`     | `int`    | `2`                  | The number of consecutive successes required in the "Half-Open" state to reset the circuit to "Closed". (Min: 1)                                                      |
| `TimeoutSeconds`       | `int`    | `60`                 | The duration in seconds the circuit will stay in the "Open" state before transitioning to "Half-Open". (Between 10 and 600)                                            |
| `FailureStatusCodes`   | `int[]`  | `[500, 502, 503, 504]`| An array of HTTP status codes that are considered failures by the circuit breaker.                                                                                    |
| `MaxRetries`           | `int`    | `3`                  | The maximum number of times to retry a failed request before considering it a full failure and potentially tripping the circuit. (Between 0 and 10)                  |
| `RetryDelayMilliseconds`| `int`    | `100`                | The delay in milliseconds between retry attempts. (Between 10 and 5000)                                                                                               |

---

### Rate Limit Policy

Configured within a `GatewayRoute` under the `"RateLimitPolicy"` section.

```json
{
  "RateLimitPolicy": {
    "Enabled": true,
    "RequestsPerMinute": 1000,
    "RequestsPerHour": 50000,
    "Strategy": "TokenBucket", // or "FixedWindow", "SlidingWindow"
    "KeyGenerator": "ClientIp", // or "AuthenticatedUser", "RouteId", "CustomHeader"
    "BypassForAuthenticatedUsers": false,
    "BurstSize": 10
  }
}
```

| Key                         | Type       | Default Value     | Description                                                                                                                                                                                                                                                                             |
| :-------------------------- | :--------- | :---------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`                   | `bool`     | `true`            | Enables or disables rate limiting for this route.                                                                                                                                                                                                                                       |
| `RequestsPerMinute`         | `int`      | `1000`            | The maximum number of requests allowed per minute. (Min: 1)                                                                                                                                                                                                                             |
| `RequestsPerHour`           | `int`      | `50000`           | The maximum number of requests allowed per hour. Must be greater than or equal to `RequestsPerMinute`. (Min: 1)                                                                                                                                                                        |
| `Strategy`                  | `enum`     | `TokenBucket`     | The rate limiting algorithm to use: `TokenBucket` (default), `FixedWindow`, `SlidingWindow`.                                                                                                                                                                                            |
| `KeyGenerator`              | `string`   | `"ClientIp"`      | Determines how the rate limit is applied. Options: `ClientIp`, `AuthenticatedUser`, `RouteId`, or `CustomHeader:YourHeaderName`.                                                                                                                                                     |
| `BypassForAuthenticatedUsers`| `bool`     | `false`           | If `true`, authenticated users (requests with valid JWTs) will bypass rate limits.                                                                                                                                                                                                      |
| `BurstSize`                 | `int`      | `10`              | For `TokenBucket` strategy, this is the maximum number of requests that can be handled instantaneously (the bucket capacity). Must be between 1 and `RequestsPerMinute`.                                                                                                            |

---

### Request Coalescing Policy

Configured within a `GatewayRoute` under the `"RequestCoalescingPolicy"` section.

```json
{
  "RequestCoalescingPolicy": {
    "Enabled": true,
    "TimeoutMs": 5000,
    "MaxQueuedRequests": 200,
    "CoalescibleMethods": ["GET", "HEAD"],
    "IncludeQueryString": true
  }
}
```

| Key                  | Type       | Default Value | Description                                                                                                                                                                                                                                                                                                                                    |
| :------------------- | :--------- | :------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`            | `bool`     | `true`        | Enables or disables request coalescing for this route.                                                                                                                                                                                                                                                                                         |
| `TimeoutMs`          | `int`      | `5000`        | The maximum milliseconds a follower request will wait to join an in-flight coalesced request before falling back to an independent execution. (Between 100 and 30,000)                                                                                                                                                                     |
| `MaxQueuedRequests`  | `int`      | `200`         | The maximum number of follower requests allowed to queue behind a single in-flight leader request. Requests beyond this limit execute independently. (Between 1 and 10,000)                                                                                                                                                                 |
| `CoalescibleMethods` | `string[]` | `["GET", "HEAD"]` | An array of HTTP methods eligible for coalescing. Only idempotent methods should be coalesced (e.g., `GET`, `HEAD`).                                                                                                                                                                                                                           |
| `IncludeQueryString` | `bool`     | `true`        | If `true`, query-string parameters are included when computing the coalescing key. Disable only if query parameters do not affect the response (e.g., for simple resource fetching where query params are solely for tracking).                                                                                                              |
