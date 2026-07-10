# ApiVersioningPolicy

Configuration policy for API versioning behavior in the gateway. Controls how API versions are detected, validated, and processed for incoming requests.

## API

### `Enabled`
Gets or sets a value indicating whether API versioning is enabled for the gateway.

- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `false`, versioning checks and processing are bypassed entirely.

### `DefaultVersion`
Gets or sets the default API version to use when no version is specified in the request.

- **Type**: `string?`
- **Default**: `null`
- **Remarks**: If `null` and no version is detected, versioning may fail depending on `RequireVersion`.

### `RequireVersion`
Gets or sets a value indicating whether a version must be explicitly provided in the request.

- **Type**: `bool`
- **Default**: `false`
- **Remarks**: When `true`, requests without a detectable version will result in a client error.

### `Strategies`
Gets the list of versioning strategies used to detect the API version from a request.

- **Type**: `List<VersioningStrategy>`
- **Default**: Empty list
- **Remarks**: Strategies are evaluated in order. The first matching strategy determines the version.

### `HeaderName`
Gets or sets the name of the HTTP header used to pass the API version.

- **Type**: `string`
- **Default**: `"X-Api-Version"`
- **Remarks**: Only relevant when a header-based strategy is included in `Strategies`.

### `QueryParameterName`
Gets or sets the name of the query parameter used to pass the API version.

- **Type**: `string`
- **Default**: `"api-version"`
- **Remarks**: Only relevant when a query-based strategy is included in `Strategies`.

### `SupportedVersions`
Gets the list of API versions explicitly supported by the gateway.

- **Type**: `List<string>`
- **Default**: Empty list
- **Remarks**: Used to validate detected versions against allowed values.

### `StripVersionFromPath`
Gets or sets a value indicating whether the version segment should be removed from the request path.

- **Type**: `bool`
- **Default**: `false`
- **Remarks**: Useful when routing to downstream services that do not expect version prefixes.

## Usage

### Example 1: Basic Configuration
