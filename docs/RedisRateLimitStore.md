# RedisRateLimitStore

A distributed rate-limiting store that uses Redis as the backing storage to track and enforce request quotas across multiple application instances. It is designed for scenarios where horizontal scaling requires a centralized, thread-safe rate-limiting mechanism.

## API

### `RedisRateLimitStore`

Initializes a new instance of the rate-limiting store using the provided Redis connection configuration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `configuration` | `string` | The Redis connection string used to establish the connection. |
| `database` | `int` | The Redis database index to use for rate-limiting data. |

No exceptions are thrown during construction; connection issues are deferred until first use.

---

### `Dispose`

Releases all managed and unmanaged resources associated with the store. After disposal, any further calls to methods will result in an `ObjectDisposedException`.

No parameters or return value.

---

### `IsRequestAllowedAsync`

Checks whether a new request is allowed based on the current rate-limiting state for the specified key.

| Parameter | Type | Description |
|-----------|------|-------------|
| `key` | `string` | The unique identifier for the rate-limited resource or endpoint. |
| `limit` | `int` | The maximum number of allowed requests within the time window. |
| `window` | `TimeSpan` | The duration of the rate-limiting window. |

| Return Value | Type | Description |
|-------------|------|-------------|
| `Task<bool>` | `true` if the request is allowed; otherwise, `false`. |

Throws `ArgumentOutOfRangeException` if `limit` is less than 1 or `window` is zero or negative. Throws `ObjectDisposedException` if the store has been disposed.

---

### `GetEntryAsync`

Retrieves the current rate-limiting entry for the specified key without modifying state.

| Parameter | Type | Description |
|-----------|------|-------------|
| `key` | `string` | The unique identifier for the rate-limited resource or endpoint. |

| Return Value | Type | Description |
|-------------|------|-------------|
| `Task<RateLimitEntry>` | A `RateLimitEntry` object representing the current state of the rate limit. |

Throws `ArgumentNullException` if `key` is `null`. Throws `ObjectDisposedException` if the store has been disposed.

---
### `ResetKeyAsync`

Resets the rate-limiting state for the specified key, effectively clearing its counters and timestamp.

| Parameter | Type | Description |
|-----------|------|-------------|
| `key` | `string` | The unique identifier for the rate-limited resource or endpoint. |

| Return Value | Type | Description |
|-------------|------|-------------|
| `Task` | Clears the state for the key. |

Throws `ArgumentNullException` if `key` is `null`. Throws `ObjectDisposedException` if the store has been disposed.

---
### `ResetAllAsync`

Resets the rate-limiting state for all keys across the entire Redis database used by this store.

| Return Value | Type | Description |
|-------------|------|-------------|
| `Task` | Clears all rate-limiting entries. |

Throws `ObjectDisposedException` if the store has been disposed.

## Usage

### Basic Rate-Limit Check
