# DotnetApiGatewayOptions

Configuration options for the .NET API Gateway, controlling core behavior such as request handling, security, routing, and observability.

## API

### `ApplicationName`
Gets or sets the name of the application. Used for logging, metrics, and identification in distributed tracing.

### `Version`
Gets or sets the version of the application. Typically used to differentiate deployments or API versions in logs and telemetry.

### `MaxRequestBodySize`
Gets or sets the maximum allowed size (in bytes) for incoming request bodies. Requests exceeding this limit will be rejected with HTTP 413 (Payload Too Large).

### `DefaultTimeoutSeconds`
Gets or sets the default timeout (in seconds) for upstream requests. If an upstream does not respond within this duration, the gateway will return HTTP 504 (Gateway Timeout).

### `MaxConcurrentRequests`
Gets or sets the maximum number of concurrent requests the gateway will process. Requests beyond this limit will be queued or rejected depending on gateway implementation.

### `EnableCors`
Gets or sets a value indicating whether Cross-Origin Resource Sharing (CORS) is enabled. When enabled, the gateway will inject CORS headers into responses.

### `EnableCompression`
Gets or sets a value indicating whether response compression (e.g., gzip, deflate) is enabled. Compression reduces payload size but increases CPU usage.

### `EnableLogging`
Gets or sets a value indicating whether request and response logging is enabled. When enabled, gateway logs request metadata and outcomes.

### `LogLevel`
Gets or sets the minimum logging level (e.g., "Debug", "Info", "Warning", "Error") for gateway operations. Only applies when `EnableLogging` is `true`.

### `EnableMetrics`
Gets or sets a value indicating whether request metrics (e.g., latency, throughput) are collected and exposed. Metrics are typically exposed via an endpoint or external system.

### `EnableHealthCheck`
Gets or sets a value indicating whether the health check endpoint is enabled. When enabled, the gateway exposes a health status endpoint at `HealthCheckPath`.

### `HealthCheckPath`
Gets or sets the HTTP path (e.g., "/health") for the health check endpoint. Only applies when `EnableHealthCheck` is `true`.

### `JwtValidationOptions`
Gets or sets the JWT validation configuration. Contains settings for issuer validation, audience validation, and secret key validation.

### `Routes`
Gets or sets the collection of gateway routes. Each route defines how incoming requests are mapped to upstream services.

### `Validate()`
Validates the current configuration. Returns a list of validation results indicating any configuration errors (e.g., invalid timeouts, missing required fields).

### `Enabled`
Gets or sets a value indicating whether the gateway is enabled. When `false`, the gateway will reject all requests with HTTP 503 (Service Unavailable).

### `Issuer`
Gets or sets the expected JWT issuer. Used during JWT validation to ensure tokens originate from a trusted source.

### `Audience`
Gets or sets the expected JWT audience. Used during JWT validation to ensure tokens are intended for this gateway.

### `SecretKey`
Gets or sets the secret key used to validate JWT signatures. Must match the key used by the token issuer.

## Usage

### Basic Configuration
