#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

/// <summary>
/// Extension methods for configuring gateway services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        GatewayConfiguration? configuration = null)
    {
        var config = configuration ?? new GatewayConfiguration();
        config.Validate();

        // Register configuration
        services.AddSingleton(config);

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
        services.AddScoped<RequestAggregationService>();

        // Register HTTP client
        services.AddHttpClient<RequestAggregationService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(config.DefaultTimeoutSeconds);
            });

        return services;
    }

    public static IApplicationBuilder UseGatewayMiddleware(this IApplicationBuilder app)
    {
        return app;
    }
}
