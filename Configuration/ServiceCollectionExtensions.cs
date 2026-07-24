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
using DotNetApiGateway.Integration;

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

        services.AddOptions<WebhookSecurityOptions>()
            .Bind(configuration.GetSection(WebhookSecurityOptions.SectionName));

        // Some consumers (e.g. AdminDashboardController) take the options object directly
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DotnetApiGatewayOptions>>().Value);

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
services.AddSingleton<GatewayManagementService>();
        services.AddSingleton<IResponseTransformer, ResponseTransformationService>();
        services.AddSingleton<RequestTransformationService>();
        services.AddSingleton<ApiVersioningService>();
        services.AddScoped<RequestAggregationService>();

        // Register integration layer
        services.AddSingleton<WebhookCallbackUrlValidator>();
        services.AddSingleton<WebhookRegistry>();

        // Register HTTP clients
        services.AddHttpClient<RequestAggregationService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<DotnetApiGatewayOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
            });

        // ExternalApiClient does the actual forwarding in the fallback endpoint;
        // per-request timeouts are handled there via CancellationTokenSource, so the
        // HttpClient itself gets a generous ceiling instead of the default 100s only.
        services.AddHttpClient<ExternalApiClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<DotnetApiGatewayOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(Math.Max(options.DefaultTimeoutSeconds * 2, 60));
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
