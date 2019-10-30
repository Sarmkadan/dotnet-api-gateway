using Microsoft.Extensions.DependencyInjection;
using DotNetApiGateway.Configuration;

namespace DotNetApiGateway.Examples;

public static class IntegrationExample
{
    // Shows how to wire the gateway services into an existing ASP.NET Core DI container
    public static void ConfigureGatewayServices(IServiceCollection services)
    {
        // 1. Define configuration
        var gatewayConfig = new DotnetApiGatewayOptions
        {
            ApplicationName = "IntegrationGateway",
            Version = "1.0.0"
        };

        // 2. Register gateway services via the extension method
        // This makes services like RoutingService, CircuitBreakerService, etc. 
        // available for injection in your controllers or middleware.
        services.AddGatewayServices(gatewayConfig);
        
        // 3. You can also register custom implementations if needed to override defaults
        // services.AddSingleton<IRateLimitStore, MyCustomRateLimitStore>();
    }
}
