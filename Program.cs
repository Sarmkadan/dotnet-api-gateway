#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// Configure services
var gatewayConfig = new DotNetApiGateway.Configuration.GatewayConfiguration
{
    MaxRequestBodySize = 10 * 1024 * 1024,
    DefaultTimeoutSeconds = 30,
    MaxConcurrentRequests = 100,
    EnableLogging = true,
    EnableMetrics = true
};

builder.Services.AddGatewayServices(gatewayConfig);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure middleware
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Core Gateway Middleware - Order is important!
app.UseRoutingMiddleware();      // 1. Resolve GatewayRoute and store in HttpContext.Items
app.UseRateLimitingMiddleware(); // 2. Apply rate limiting based on resolved route

// Health check endpoint
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}).WithName("Health");

// Gateway info endpoint
app.MapGet("/gateway/info", () => new
{
    name = gatewayConfig.ApplicationName,
    version = gatewayConfig.Version,
    endpoints = new
    {
        health = "/health",
        routes = "/gateway/routes",
        circuitBreakers = "/gateway/circuit-breakers",
        rateLimits = "/api/GatewayManagement/rate-limits/{key}" // Updated info for new endpoint
    }
}).WithName("GatewayInfo");

// Routes endpoint
app.MapGet("/gateway/routes", async (DotNetApiGateway.Services.RoutingService routingService) =>
{
    var routes = await routingService.GetAllActiveRoutesAsync();
    return Results.Ok(routes);
}).WithName("GetRoutes");

// Circuit breaker status endpoint
app.MapGet("/gateway/circuit-breakers", async (DotNetApiGateway.Services.CircuitBreakerService cbService) =>
{
    var statuses = await cbService.GetAllStatusesAsync();
    return Results.Ok(statuses);
}).WithName("GetCircuitBreakers");

// Default routing and forwarding endpoint
app.MapFallback(async (HttpContext context, DotNetApiGateway.Services.RoutingService routingService) =>
{
    // Check if route resolution failed in RoutingMiddleware
    if (context.Items.TryGetValue("RouteNotFoundException", out var notFoundEx) && notFoundEx is RouteNotFoundException rnf)
    {
        context.Response.StatusCode = 404;
        return Results.Json(new { error = rnf.Message, errorCode = rnf.ErrorCode });
    }
    if (context.Items.TryGetValue("RouteResolutionError", out var resolutionEx) && resolutionEx is Exception resEx)
    {
        context.Response.StatusCode = 500;
        return Results.Json(new { error = $"Error during route resolution: {resEx.Message}" });
    }

    // Retrieve the resolved route from HttpContext.Items
    if (!context.Items.TryGetValue("GatewayRoute", out var routeObj) || routeObj is not GatewayRoute route)
    {
        // This should not happen if RoutingMiddleware ran correctly and found no explicit error.
        // It implies no route was found, so return 404.
        context.Response.StatusCode = 404;
        return Results.Json(new { error = "Route not found", path = context.Request.Path.Value, method = context.Request.Method });
    }
    
    // Proceed with forwarding the request using the resolved route
    // (This part would involve calling a service to forward the request)
    var target = routingService.SelectTarget(route, context.Connection.RemoteIpAddress?.ToString());
    var forwardUrl = routingService.BuildForwardUrl(target, context.Request.Path.Value ?? "/");

    return Results.Json(new
    {
        message = "Request would be forwarded",
        route = route.Id, // Changed from route.Name to route.Id for consistency
        target = target.Name,
        forwardUrl,
        timestamp = DateTime.UtcNow
    });
});

app.Run();
