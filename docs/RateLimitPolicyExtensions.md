# RateLimitPolicyExtensions

Extension methods for configuring rate limiting policies in the dotnet-api-gateway project. These methods provide fluent interfaces to define rate limit strategies, storage mechanisms, and request quotas per time window.

## API

### `WithRequestsPerMinute`

Configures the rate limit policy to allow a specified number of requests per minute.

- **Parameters**
  - `count` (int): The maximum number of requests allowed per minute.
- **Return Value**
  - `RateLimitPolicy`: The configured policy instance for method chaining.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `count` is less than or equal to zero.

### `WithRequestsPerHour`

Configures the rate limit policy to allow a specified number of requests per hour.

- **Parameters**
  - `count` (int): The maximum number of requests allowed per hour.
- **Return Value**
  - `RateLimitPolicy`: The configured policy instance for method chaining.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `count` is less than or equal to zero.

### `WithStrategy`

Sets the rate limiting strategy for the policy.

- **Parameters**
  - `strategy` (RateLimitStrategy): The strategy to apply (e.g., sliding window, fixed window).
- **Return Value**
  - `RateLimitPolicy`: The configured policy instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `strategy` is null.

### `WithStorage`

Configures the storage mechanism for tracking rate limit counters.

- **Parameters**
  - `storage` (RateLimitStorage): The storage implementation to use (e.g., in-memory, distributed).
- **Return Value**
  - `RateLimitPolicy`: The configured policy instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `storage` is null.

## Usage

### Example 1: Fixed Window Rate Limiting with In-Memory Storage
