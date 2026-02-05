#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Extension methods for registering request coalescing services in the DI container.
/// </summary>
public static class RequestCoalescingExtensions
{
    /// <summary>
    /// Registers <see cref="RequestCoalescingService"/> as a singleton and validates
    /// the supplied <paramref name="policy"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="policy">
    /// An optional <see cref="RequestCoalescingPolicy"/> to validate and register.
    /// When <see langword="null"/> a default policy is used.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policy"/> contains invalid configuration values.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.Services.AddRequestCoalescing(new RequestCoalescingPolicy
    /// {
    ///     TimeoutMs           = 3000,
    ///     MaxQueuedRequests   = 500,
    ///     CoalescibleMethods  = ["GET"],
    ///     IncludeQueryString  = true,
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRequestCoalescing(
        this IServiceCollection services,
        RequestCoalescingPolicy? policy = null)
    {
        var resolved = policy ?? new RequestCoalescingPolicy();
        resolved.Validate();

        services.AddSingleton(resolved);
        services.AddSingleton<RequestCoalescingService>();

        return services;
    }
}
