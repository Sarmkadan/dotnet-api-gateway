# .NET API Gateway Architecture Overview

This document provides a high-level overview of the .NET API Gateway's architecture, illustrating how requests flow through various components and middleware layers.

## Core Components

The API Gateway is built around several key components that work together to process incoming requests and route them to appropriate upstream services.

-   **Routing Engine:** The core of the gateway, responsible for matching incoming requests to configured `GatewayRoute` definitions.
-   **Middleware Pipeline:** A series of middleware components that process requests in a specific order, implementing cross-cutting concerns like authentication, rate limiting, and error handling.
-   **Upstream Service Integration:** Handles communication with backend services, including features like circuit breakers, retries, and request aggregation.

## Request Lifecycle

A typical request to the API Gateway follows this flow:

1.  **Request Reception:** An incoming HTTP request arrives at the gateway.
2.  **Request Logging:** (Optional) The `RequestLoggingMiddleware` records details about the incoming request.
3.  **Error Handling:** The `ErrorHandlingMiddleware` is positioned early in the pipeline to catch and handle exceptions gracefully.
4.  **Request Validation:** The `RequestValidationMiddleware` can enforce schema validation or other structural checks on the incoming request.
5.  **Authentication/Authorization:** The `JwtValidationMiddleware` (or other authentication mechanisms) verifies the client's identity and permissions based on configured `AuthenticationPolicy`.
6.  **Rate Limiting:** The `RateLimitingMiddleware` enforces request limits based on `RateLimitPolicy` to protect backend services from overload.
7.  **Routing:** The `RoutingService` matches the incoming request to a `GatewayRoute`.
8.  **Request Transformation:** (Optional) Controllers like `RequestTransformationController` can modify headers, body, or query parameters before forwarding.
9.  **Circuit Breaker:** The `CircuitBreakerService` protects against cascading failures by monitoring upstream service health. If a service is unhealthy, requests are short-circuited.
10. **Request Coalescing/Aggregation:** For specific routes, `RequestCoalescingService` or `RequestAggregationService` might combine multiple identical requests or fan-out to multiple upstream services, respectively.
11. **Upstream Call:** The `HttpClientFactory` and `ExternalApiClient` are used to make the actual call to the target upstream service, potentially with `RetryPolicy`.
12. **Response Processing:** The response from the upstream service is received.
13. **Response Transformation:** (Optional) Responses can be modified before being sent back to the client.
14. **Metrics and Performance Monitoring:** `MetricsService` and `PerformanceMonitoringMiddleware` collect data throughout the request lifecycle.
15. **Response Sent:** The final response is sent back to the client.

## Key Services and Repositories

-   **`RoutingService`**: Manages and resolves `GatewayRoute` definitions.
-   **`CircuitBreakerService`**: Implements the circuit breaker pattern, tracking service health and managing states (Closed, Open, Half-Open).
-   **`RateLimitingService`**: Enforces rate limits using various strategies.
-   **`JwtValidationService`**: Handles JSON Web Token validation.
-   **`CacheService`**: Provides caching capabilities for responses.
-   **`GatewayRouteRepository`**: Manages persistence of `GatewayRoute` configurations.
-   **`CircuitBreakerRepository`**: Stores and retrieves `CircuitBreakerStatus` information.
-   **`RateLimitRepository`**: Manages persistence for rate limit counters.

## Extensibility

The gateway's modular design allows for extensibility through:

-   **Custom Middleware:** Adding new `IMiddleware` implementations to the pipeline.
-   **Pluggable Policies:** Defining new `AuthenticationPolicy`, `RateLimitPolicy`, or `CircuitBreakerPolicy` types.
-   **Custom Formatters:** Implementing new `IResponseFormatter` to support different response formats.

This architecture ensures a flexible, resilient, and high-performance API gateway solution.
