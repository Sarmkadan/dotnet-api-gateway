# RequestContextTests

Unit test class for `RequestContext` that validates behavior of request context management, including request identification, client identification, authentication token handling, timing, and data storage.

## API

### `Constructor_InitializesWithDefaults`
Validates that a new `RequestContext` instance is initialized with default values for all properties (e.g., empty headers, query parameters, and custom data).

### `RequestId_GeneratesUniqueId`
Ensures that each `RequestContext` instance receives a unique `RequestId` upon construction.

### `GetClientIdentifier_WithClientIdentity_ReturnsIdentityId`
When a client identity is present in the request, returns the identity ID as the client identifier.

### `GetClientIdentifier_NoClientIdentity_ReturnsClientIp`
When no client identity is present, returns the client IP address as the client identifier.

### `GetClientIdentifier_ClientIdentityIdNull_ReturnsClientIp`
When the client identity ID is explicitly null, returns the client IP address as the client identifier.

### `HasAuthToken_WithToken_ReturnsTrue`
Returns `true` when the request contains a non-empty, non-whitespace authentication token.

### `HasAuthToken_WithoutToken_ReturnsFalse`
Returns `false` when the request contains no authentication token.

### `HasAuthToken_WithEmptyToken_ReturnsFalse`
Returns `false` when the authentication token is an empty string.

### `HasAuthToken_WithWhitespaceToken_ReturnsFalse`
Returns `false` when the authentication token consists only of whitespace.

### `ExtractBearerToken_WithBearerPrefix_RemovesPrefix`
When the token starts with `"Bearer "` (case-sensitive), returns the substring after the prefix.

### `ExtractBearerToken_WithLowercaseBearerPrefix_RemovesPrefix`
When the token starts with `"bearer "` (lowercase), returns the substring after the prefix.

### `ExtractBearerToken_WithoutBearerPrefix_ReturnsFull`
When the token does not start with a bearer prefix, returns the full token string.

### `ExtractBearerToken_NoAuthToken_ReturnsEmpty`
When no authentication token is present, returns an empty string.

### `ExtractBearerToken_EmptyAuthToken_ReturnsEmpty`
When the authentication token is an empty string, returns an empty string.

### `ElapsedTime_CalculatesTimeDifference`
Returns the duration between `ReceivedAt` and the current time as a `TimeSpan`.

### `ElapsedTime_JustCreated_ReturnsSmallDuration`
Returns a small positive duration when called immediately after construction.

### `ElapsedTime_FutureReceivedAt_ReturnsNegativeDuration`
Returns a negative duration when `ReceivedAt` is set to a future time.

### `Headers_CanBeModified`
Validates that the `Headers` dictionary can be modified after construction without throwing exceptions.

### `QueryParameters_CanBeModified`
Validates that the `QueryParameters` dictionary can be modified after construction without throwing exceptions.

### `CustomData_CanStoreArbitraryData`
Validates that the `CustomData` dictionary can store arbitrary key-value pairs without throwing exceptions.

## Usage
