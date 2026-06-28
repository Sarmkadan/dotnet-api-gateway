using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DotNetApiGateway.Configuration;
using DotNetApiGateway.Models;

// Advanced setup with configuration, custom options, and error handling
var builder = WebApplication.CreateBuilder(args);

// 1. Advanced configuration
var gatewayConfig = new GatewayConfiguration
{
    ApplicationName = "AdvancedGateway",
    Version = "2.0.0",
    MaxRequestBodySize = 50 * 1024 * 1024, // 50MB
    DefaultTimeoutSeconds = 60,
    EnableMetrics = true,
    EnableLogging = true
};

// 2. Register gateway services with custom options
builder.Services.AddGatewayServices(gatewayConfig);

var app = builder.Build();

// 3. Register middleware in specific order
app.UseRoutingMiddleware();      // Resolve route
app.UseRateLimitingMiddleware(); // Apply rate limits
app.UsePerformanceMonitoringMiddleware(); // Monitor performance

// 4. Custom Error Handling
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Custom error handling logic
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { 
            error = "An internal gateway error occurred.",
            message = ex.Message 
        });
    }
});

app.MapFallback(async (HttpContext context) =>
{
    // The actual forwarding logic would be implemented here, 
    // often using the services registered by the gateway.
    await context.Response.WriteAsync("Advanced gateway request processing.");
});

app.Run();
