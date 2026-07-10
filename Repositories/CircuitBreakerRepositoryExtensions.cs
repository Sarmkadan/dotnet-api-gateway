using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetApiGateway.Repositories
{
    public static class CircuitBreakerRepositoryExtensions
    {
        /// <summary>
        /// Gets the first circuit breaker status by service name, or null if not found.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="serviceName">Name of the service to find.</param>
        /// <returns>The circuit breaker status or null if not found.</returns>
        public static async Task<CircuitBreakerStatus?> GetByServiceNameOrDefaultAsync(
            this CircuitBreakerRepository repository,
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("Service name cannot be null or whitespace.", nameof(serviceName));
            }

            return await repository.GetByServiceNameAsync(serviceName);
        }

        /// <summary>
        /// Gets all circuit breakers that are in a specific state.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="state">The state to filter by.</param>
        /// <returns>Collection of circuit breakers in the specified state.</returns>
        public static async Task<IEnumerable<CircuitBreakerStatus>> GetByStateAsync(
            this CircuitBreakerRepository repository,
            CircuitBreakerState state)
        {
            return await repository.GetByStateAsync(state);
        }

        /// <summary>
        /// Gets all circuit breakers that are currently in the OPEN state.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <returns>Collection of circuit breakers in OPEN state.</returns>
        public static async Task<IEnumerable<CircuitBreakerStatus>> GetOpenCircuitsAsync(
            this CircuitBreakerRepository repository)
        {
            return await repository.GetOpenCircuitsAsync();
        }

        /// <summary>
        /// Checks if a circuit breaker with the given ID exists.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="id">The circuit breaker ID.</param>
        /// <returns>True if exists, false otherwise.</returns>
        public static async Task<bool> ExistsByIdAsync(
            this CircuitBreakerRepository repository,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID cannot be null or whitespace.", nameof(id));
            }

            return await repository.ExistsAsync(id);
        }

        /// <summary>
        /// Updates multiple circuit breakers in a single batch operation.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="circuitBreakers">Collection of circuit breakers to update.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static async Task UpdateBatchAsync(
            this CircuitBreakerRepository repository,
            IEnumerable<CircuitBreakerStatus> circuitBreakers)
        {
            if (circuitBreakers == null)
            {
                throw new ArgumentNullException(nameof(circuitBreakers));
            }

            foreach (var circuitBreaker in circuitBreakers)
            {
                await repository.UpdateAsync(circuitBreaker);
            }
        }

        /// <summary>
        /// Gets all circuit breakers and converts them to a dictionary keyed by service name.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <returns>Dictionary mapping service names to their circuit breaker status.</returns>
        public static async Task<Dictionary<string, CircuitBreakerStatus>> ToDictionaryByServiceNameAsync(
            this CircuitBreakerRepository repository)
        {
            var all = await repository.GetAllAsync();
            return all.ToDictionary(cb => cb.ServiceName, cb => cb);
        }

        /// <summary>
        /// Resets all circuit breakers to their initial state (CLOSED).
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static async Task ResetAllToClosedAsync(this CircuitBreakerRepository repository)
        {
            await repository.ResetAllAsync();

            // After reset, ensure all are in CLOSED state
            var all = await repository.GetAllAsync();
            foreach (var cb in all)
            {
                if (cb.State != CircuitBreakerState.Closed)
                {
                    cb.State = CircuitBreakerState.Closed;
                    await repository.UpdateAsync(cb);
                }
            }
        }

        /// <summary>
        /// Gets circuit breakers filtered by multiple states.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="states">States to include.</param>
        /// <returns>Collection of circuit breakers matching any of the specified states.</returns>
        public static async Task<IEnumerable<CircuitBreakerStatus>> GetByStatesAsync(
            this CircuitBreakerRepository repository,
            params CircuitBreakerState[] states)
        {
            if (states == null || states.Length == 0)
            {
                return await repository.GetAllAsync();
            }

            var all = await repository.GetAllAsync();
            return all.Where(cb => states.Contains(cb.State));
        }

        /// <summary>
        /// Safely adds or updates a circuit breaker based on whether it already exists.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="circuitBreaker">The circuit breaker to upsert.</param>
        /// <returns>The added or updated circuit breaker status.</returns>
        public static async Task<CircuitBreakerStatus> UpsertAsync(
            this CircuitBreakerRepository repository,
            CircuitBreakerStatus circuitBreaker)
        {
            if (circuitBreaker == null)
            {
                throw new ArgumentNullException(nameof(circuitBreaker));
            }

            if (string.IsNullOrWhiteSpace(circuitBreaker.Id))
            {
                throw new ArgumentException("Circuit breaker ID cannot be null or whitespace.", nameof(circuitBreaker));
            }

            var exists = await repository.ExistsAsync(circuitBreaker.Id);
            return exists
                ? await repository.UpdateAsync(circuitBreaker)
                : await repository.AddAsync(circuitBreaker);
        }
    }
}