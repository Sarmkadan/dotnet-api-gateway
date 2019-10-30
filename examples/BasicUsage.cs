using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DotNetApiGateway.Configuration;

// Minimal setup for DotNetApiGateway
var builder = WebApplication.CreateBuilder(args);

// 1. Configure the gateway with basic settings
var gatewayConfig = new DotnetApiGatewayOptions
{
    ApplicationName = "BasicGateway",
    Version = "1.0.0",
    EnableLogging = true
};

// 2. Register gateway services
builder.Services.AddGatewayServices(gatewayConfig);

var app = builder.Build();

// 3. Register gateway middleware
// RoutingMiddleware resolves the target route based on request path
app.UseRoutingMiddleware();

// 4. Map a catch-all route for the gateway
app.MapFallback(async (HttpContext context) =>
{
    // The routing is handled by the Middleware and the final forward logic
    // This example is illustrative of the entry point
    await context.Response.WriteAsync("Gateway is running. Requests are forwarded based on configuration.");
});

app.Run();
