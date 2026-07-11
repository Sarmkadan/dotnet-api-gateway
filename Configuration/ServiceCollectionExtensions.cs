#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using DotNetApiGateway.Middleware;

/// <summary>
/// Extension methods for configuring gateway services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all gateway services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown if services or configuration is null</exception>
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register configuration
        services.AddOptions<DotnetApiGatewayOptions>()
            .Bind(configuration.GetSection(DotnetApiGatewayOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register repositories
        services.AddSingleton<GatewayRouteRepository>();
        services.AddSingleton<CircuitBreakerRepository>();

        // Register rate limit stores and factory
        services.AddSingleton<InMemoryRateLimitStore>();
        services.AddSingleton<IRateLimitStoreFactory, RateLimitStoreFactory>();

        // Register services
        services.AddSingleton<RoutingService>();
        services.AddSingleton<RateLimitingService>();
        services.AddSingleton<JwtValidationService>();
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<MetricsService>();
        services.AddSingleton<IResponseTransformer, ResponseTransformationService>();
        services.AddSingleton<RequestTransformationService>();
        services.AddSingleton<ApiVersioningService>();
        services.AddScoped<RequestAggregationService>();

        // Register HTTP client
        services.AddHttpClient<RequestAggregationService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<DotnetApiGatewayOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
            });

        return services;
    }

    /// <summary>
    /// Adds and configures all gateway middleware components to the HTTP request pipeline.
    /// This includes error handling, request validation, routing, and gateway processing.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The configured application builder</returns>
    /// <exception cref="ArgumentNullException">Thrown if app is null</exception>
    public static IApplicationBuilder UseAllGatewayMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app
            .UseMiddleware<ErrorHandlingMiddleware>()
            .UseMiddleware<RequestValidationMiddleware>()
            .UseMiddleware<RequestLoggingMiddleware>()
            .UseMiddleware<RoutingMiddleware>()
            .UseMiddleware<RateLimitingMiddleware>()
            .UseMiddleware<PerformanceMonitoringMiddleware>()
            .UseMiddleware<GatewayMiddleware>();
    }
}
