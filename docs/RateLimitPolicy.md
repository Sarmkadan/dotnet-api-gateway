# RateLimitPolicy

A configuration class that defines rate-limiting rules for API endpoints in the dotnet-api-gateway. It specifies limits, strategies, and storage options to control how requests are throttled, including per-minute, per-hour, and burst-based constraints.

## API

### `public string Id`
A unique identifier for the rate-limit policy. Used to reference the policy when applying it to routes or endpoints.

### `public int RequestsPerMinute`
The maximum number of requests allowed per minute. Must be a positive integer. Defaults to `0` (no limit).

### `public int RequestsPerHour`
The maximum number of requests allowed per hour. Must be a positive integer. Defaults to `0` (no limit).

### `public RateLimitStrategy Strategy`
The strategy used to enforce rate limits. Possible values include `FixedWindow`, `SlidingWindow`, or `TokenBucket`. Determines how the limits are applied over time.

### `public string KeyGenerator`
A string identifier for the key generator used to compute the rate-limiting key (e.g., by client IP, user ID, or custom logic). Must not be null or empty.

### `public bool BypassForAuthenticatedUsers`
If `true`, requests with valid authentication are exempt from rate limiting. Defaults to `false`.

### `public int BurstSize`
The maximum number of requests allowed in a short burst. Must be a positive integer. Used in conjunction with `TokenBucket` strategy to allow temporary spikes.

### `public bool Enabled`
Determines whether the policy is active. If `false`, rate limiting is not enforced. Defaults to `true`.

### `public RateLimitStorageType StorageType`
Specifies where rate-limit counters are stored. Options include `Memory`, `Redis`, or `Distributed`. Defaults to `Memory`.

### `public string? RedisConnectionString`
The connection string for Redis when `StorageType` is set to `Redis`. Must be provided if Redis storage is used.

### `public void Validate()`
Validates the policy configuration. Throws an exception if required fields are missing or invalid (e.g., negative limits, invalid strategy, or missing Redis connection string when required).

### `public int GetLimitForWindow()`
Returns the effective limit for the current window based on the configured `RequestsPerMinute` and `RequestsPerHour`. Returns `0` if no limits are set.

### `public bool IsEnabled()`
Returns `true` if the policy is enabled (`Enabled` is `true`) and all required configurations are valid. Otherwise, returns `false`.
