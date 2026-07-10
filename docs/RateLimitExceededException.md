# RateLimitExceededException

The `RateLimitExceededException` is thrown when a client exceeds the allowed request rate limit in the `dotnet-api-gateway` project. This exception provides details about the rate limit policy that was violated, including the client identifier, the remaining cooldown period, and the configured requests-per-minute limit.

## API

### `public string ClientId`
- **Purpose**: Identifies the client that triggered the rate limit violation.
- **Value**: A non-null string representing the client identifier (e.g., API key, IP address, or user ID). Never empty or whitespace-only in valid scenarios.

### `public long RemainingSeconds`
- **Purpose**: Indicates the number of seconds remaining until the client may resume making requests without violating the rate limit.
- **Value**: A non-negative integer representing the cooldown period. Zero means the client may retry immediately, though subsequent requests may still be rate-limited if the window has not reset.

### `public int LimitPerMinute`
- **Purpose**: Specifies the maximum number of requests allowed per minute for the client under the violated rate limit policy.
- **Value**: A positive integer representing the configured threshold. Values ≤ 0 are invalid and indicate a misconfigured policy.

### `public RateLimitExceededException`
- **Purpose**: Constructs an instance of the exception with the provided rate limit violation details.
- **Parameters**: None (inherits from `System.Exception`; internal logic populates `ClientId`, `RemainingSeconds`, and `LimitPerMinute`).
- **Throws**: None. The constructor does not validate input; invalid states (e.g., negative `RemainingSeconds`) should be prevented by the caller.

## Usage

### Example 1: Handling Rate Limit Exceptions
