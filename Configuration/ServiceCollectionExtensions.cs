#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Extension methods for configuring gateway services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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

    public static IApplicationBuilder UseGatewayMiddleware(this IApplicationBuilder app)
    {
        return app;
    }
}
