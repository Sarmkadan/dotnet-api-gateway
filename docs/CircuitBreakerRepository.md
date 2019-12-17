# CircuitBreakerRepository

Provides asynchronous persistence operations for circuit breaker statuses, enabling state management of circuit breaker patterns in the API gateway. Supports CRUD operations, querying by state, and bulk reset functionality.

## API

### `GetByIdAsync`
Retrieves a circuit breaker status by its unique identifier.

- **Parameters**:
  - `id` (string): The unique identifier of the circuit breaker.
- **Returns**: `Task<CircuitBreakerStatus?>`: The circuit breaker status if found; otherwise, `null`.
- **Exceptions**: Throws if the underlying storage fails.

### `GetAllAsync`
Retrieves all circuit breaker statuses stored in the repository.

- **Returns**: `Task<IEnumerable<CircuitBreakerStatus>>`: An enumerable of all circuit breaker statuses.
- **Exceptions**: Throws if the underlying storage fails.

### `AddAsync`
Adds a new circuit breaker status to the repository.

- **Parameters**:
  - `status` (CircuitBreakerStatus): The circuit breaker status to add.
- **Returns**: `Task<CircuitBreakerStatus>`: The added circuit breaker status, including any generated identifiers.
- **Exceptions**: Throws if the status already exists or if storage fails.

### `UpdateAsync`
Updates an existing circuit breaker status in the repository.

- **Parameters**:
  - `status` (CircuitBreakerStatus): The updated circuit breaker status.
- **Returns**: `Task<CircuitBreakerStatus>`: The updated circuit breaker status.
- **Exceptions**: Throws if the status does not exist or if storage fails.

### `DeleteAsync`
Removes a circuit breaker status from the repository by its identifier.

- **Parameters**:
  - `id` (string): The unique identifier of the circuit breaker to remove.
- **Returns**: `Task<bool>`: `true` if the status was found and removed; otherwise, `false`.
- **Exceptions**: Throws if storage fails.

### `ExistsAsync`
Checks whether a circuit breaker status with the given identifier exists.

- **Parameters**:
  - `id` (string): The unique identifier to check.
- **Returns**: `Task<bool>`: `true` if the status exists; otherwise, `false`.
- **Exceptions**: Throws if storage fails.

### `GetByServiceNameAsync`
Retrieves a circuit breaker status by the associated service name.

- **Parameters**:
  - `serviceName` (string): The name of the service to look up.
- **Returns**: `Task<CircuitBreakerStatus?>`: The circuit breaker status if found; otherwise, `null`.
- **Exceptions**: Throws if storage fails.

### `GetByStateAsync`
Retrieves all circuit breaker statuses matching the specified state.

- **Parameters**:
  - `state` (CircuitBreakerState): The state to filter by (e.g., `Open`, `Closed`, `HalfOpen`).
- **Returns**: `Task<IEnumerable<CircuitBreakerStatus>>`: An enumerable of matching circuit breaker statuses.
- **Exceptions**: Throws if storage fails.

### `GetOpenCircuitsAsync`
Retrieves all circuit breaker statuses currently in the `Open` state.

- **Returns**: `Task<IEnumerable<CircuitBreakerStatus>>`: An enumerable of open circuit breaker statuses.
- **Exceptions**: Throws if storage fails.

### `ResetAllAsync`
Resets all circuit breaker statuses to their default or initial state (e.g., `Closed`).

- **Returns**: `Task`: A task representing the asynchronous operation.
- **Exceptions**: Throws if storage fails.

### `ClearAll`
Removes all circuit breaker statuses from the repository.

- **Returns**: `void`: No return value.
- **Exceptions**: Throws if storage fails.

## Usage

### Example 1: Managing a Circuit Breaker
