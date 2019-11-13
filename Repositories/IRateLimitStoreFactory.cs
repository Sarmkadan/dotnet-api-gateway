#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Factory for resolving the correct IRateLimitStore implementation based on policy.
/// </summary>
public interface IRateLimitStoreFactory : IDisposable
{
    /// <summary>
    /// Gets an IRateLimitStore instance for the given rate limit policy.
    /// </summary>
    /// <param name="policy">The rate limit policy.</param>
    /// <returns>An IRateLimitStore instance.</returns>
    IRateLimitStore GetStore(RateLimitPolicy policy);

    /// <summary>
    /// Gets all registered IRateLimitStore instances.
    /// </summary>
    /// <returns>A collection of all IRateLimitStore instances.</returns>
    IEnumerable<IRateLimitStore> GetAllStores();
}
