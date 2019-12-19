# InMemoryRateLimitStore

A lightweight, in-memory implementation of `IRateLimitStore` designed for rate-limiting in high-throughput scenarios where persistence is not required. It maintains rate-limit counters and policies entirely in process, making it suitable for single-instance gateways or testing environments.

## API

### `InMemoryRateLimitStore`

Initializes a new instance of the in-memory rate-limit store. All rate-limit data is stored in a concurrent dictionary and is not persisted across application restarts.

### `Task<bool> IsRequestAllowedAsync(string key, RateLimitPolicy policy)`

Determines whether a request identified by `key` is allowed under the given `policy`.

- **Parameters**
  - `key`: The unique identifier for the rate-limited entity (e.g., client IP, API key).
  - `policy`: The rate-limiting policy defining limits (e.g., number of requests, time window).

- **Return Value**
  Returns `true` if the request is allowed; otherwise, `false`.

- **Exceptions**
  Throws `ArgumentNullException` if `key` or `policy` is `null`.

### `Task<RateLimitEntry> GetEntryAsync(string key)`

Retrieves the current rate-limit entry for the specified `key`.

- **Parameters**
  - `key`: The unique identifier for the rate-limited entity.

- **Return Value**
  Returns a `RateLimitEntry` object representing the current state of the rate limit for the key, or `null` if no entry exists.

- **Exceptions**
  Throws `ArgumentNullException` if `key` is `null`.

### `Task ResetKeyAsync(string key)`

Removes all rate-limit data associated with the specified `key`.

- **Parameters**
  - `key`: The unique identifier for the rate-limited entity.

- **Exceptions**
  Throws `ArgumentNullException` if `key` is `null`.

### `Task ResetAllAsync()`

Removes all rate-limit data across all keys.

## Usage
