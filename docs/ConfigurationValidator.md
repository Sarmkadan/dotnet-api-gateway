# ConfigurationValidator

A utility class for validating configuration objects used in the API Gateway, including gateway settings, routes, targets, and policy configurations. It collects validation errors during checks and provides methods to retrieve error summaries or detailed validation results.

## API

### `public ConfigurationValidator()`

Initializes a new instance of the `ConfigurationValidator` class with an empty error list.

### `public ValidationResult ValidateGatewayConfig(object config)`

Validates the provided gateway configuration object.

- **Parameters**
  - `config` – The gateway configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `config` is `null`.

### `public ValidationResult ValidateRoute(object routeConfig)`

Validates the provided route configuration object.

- **Parameters**
  - `routeConfig` – The route configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the route configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `routeConfig` is `null`.

### `public ValidationResult ValidateRouteTarget(object targetConfig)`

Validates the provided route target configuration object.

- **Parameters**
  - `targetConfig` – The route target configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the target configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `targetConfig` is `null`.

### `public ValidationResult ValidateRateLimitPolicy(object policyConfig)`

Validates the provided rate limit policy configuration object.

- **Parameters**
  - `policyConfig` – The rate limit policy configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the policy configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `policyConfig` is `null`.

### `public ValidationResult ValidateCircuitBreakerPolicy(object policyConfig)`

Validates the provided circuit breaker policy configuration object.

- **Parameters**
  - `policyConfig` – The circuit breaker policy configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the policy configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `policyConfig` is `null`.

### `public ValidationResult ValidateCachePolicy(object policyConfig)`

Validates the provided cache policy configuration object.

- **Parameters**
  - `policyConfig` – The cache policy configuration object to validate.
- **Return value**
  - A `ValidationResult` indicating whether the policy configuration is valid and containing any validation messages.
- **Throws**
  - `ArgumentNullException` if `policyConfig` is `null`.

### `public List<string> Errors { get; }`

Gets the list of validation error messages collected during validation operations.

### `public void AddError(string error)`

Adds a validation error message to the internal errors list.

- **Parameters**
  - `error` – The error message to add.
- **Throws**
  - `ArgumentNullException` if `error` is `null`.

### `public string GetErrorSummary()`

Generates a summary of all collected validation errors.

- **Return value**
  - A string containing a formatted summary of all errors, or an empty string if no errors are present.

## Usage

### Example 1: Validating a gateway configuration
