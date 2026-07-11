# RouteTargetExtensions

Extension methods for route targets that provide common routing and traffic management capabilities.

## API

### `GetEffectiveBaseUrl`

Gets the effective base URL for the route target, combining any configured base path with the host's base URL.

- **Returns**
  The effective base URL as a string, or `null` if no base URL is configured.

### `GetTimeoutMilliseconds`

Gets the timeout value in milliseconds for requests to this route target.

- **Returns**
  The timeout in milliseconds as an `int?`, or `null` if no timeout is configured.

### `ShouldReceiveTraffic`

Determines whether the route target should receive traffic based on configured conditions.

- **Returns**
  `true` if the route target should receive traffic; otherwise, `false`.

### `GetNormalizedWeight`

Gets the normalized weight of the route target, used for load balancing purposes.

- **Returns**
  The normalized weight as a `double`, representing the relative traffic share this target should receive.

## Usage
