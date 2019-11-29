# RateLimitingServiceTests

Unit tests for `RateLimitingService` that validate rate limiting behavior across different strategies, policies, and edge cases. The test suite verifies request allowance logic, rate limit tracking, and store interactions under various scenarios including sliding window and token bucket strategies.

## API

### `IsAllowedAsync_DisabledPolicy_ReturnsTrue`
Verifies that when a rate limiting policy is disabled, any request is allowed regardless of prior activity. Useful for testing policy configuration scenarios.

### `IsAllowedAsync_ValidRequest_ReturnsTrue`
Ensures that a request under the allowed limit is permitted when the rate limiting policy is active. Tests the baseline happy path for rate limiting.

### `IsAllowedAsync_RateLimitExceeded_ReturnsFalse`
Confirms that a request exceeding the configured rate limit is denied. Validates enforcement of rate limiting thresholds.

### `GetRateLimitInfoAsync_SlidingWindowStrategy_CalculatesRemaining`
Checks that the sliding window strategy correctly computes remaining requests within the current window. Validates time-windowed rate limiting logic.

### `GetRateLimitInfoAsync_TokenBucketStrategy_UsesTokensRemaining`
Validates that the token bucket strategy returns the correct number of available tokens. Ensures token consumption and replenishment are tracked accurately.

### `ResetKeyLimitsAsync_CallsResetOnAllStores`
Ensures that calling `ResetKeyLimitsAsync` invokes the reset operation on all configured rate limit stores for a given key. Tests store-level cleanup behavior.

### `ResetAllLimitsAsync_CallsResetOnAllStores`
Confirms that `ResetAllLimitsAsync` triggers a full reset across all rate limit stores. Validates system-wide cleanup functionality.

### `IsAllowedAsync_MultipleClients_TracksIndividually`
Verifies that rate limiting is applied per client identifier, ensuring isolation between different clients. Tests multi-tenant isolation.

### `Dispose_DisposesFactory`
Ensures that disposing the test fixture properly disposes the underlying rate limit factory and its resources. Validates cleanup of disposable components.

### `GetRateLimitInfoAsync_ZeroRequestCount_HasFullRemaining`
Checks that when no requests have been made, the remaining request count equals the maximum allowed. Validates initial state reporting.

### `GetRateLimitInfoAsync_MaxedOutRequests_ZeroRemaining`
Confirms that after exhausting the rate limit, the remaining request count is zero. Tests accurate state reporting under full load.

## Usage
