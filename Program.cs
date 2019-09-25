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
        circuitBreakers = "/gateway/circuit-breakers"
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

// Rate limit info endpoint
app.MapPost("/gateway/rate-limit-info", async (
    string clientId,
    string routeId,
    DotNetApiGateway.Services.RateLimitingService rateLimitService,
    DotNetApiGateway.Repositories.GatewayRouteRepository routeRepository) =>
{
    var route = await routeRepository.GetByIdAsync(routeId);
    if (route?.RateLimitPolicy == null)
        return Results.NotFound("Route or rate limit policy not found");

    var info = await rateLimitService.GetRateLimitInfoAsync(clientId, routeId, route.RateLimitPolicy);
    return Results.Ok(info);
}).WithName("GetRateLimitInfo");

// Default routing endpoint
app.MapFallback(async (HttpContext context, DotNetApiGateway.Services.RoutingService routingService) =>
{
    var path = context.Request.Path.Value ?? "/";
    var method = context.Request.Method;

    try
    {
        var route = await routingService.FindRouteAsync(path, method);

        if (route == null)
            return Results.NotFound(new { error = "Route not found", path, method });

        var target = routingService.SelectTarget(route, context.Connection.RemoteIpAddress?.ToString());
        var forwardUrl = routingService.BuildForwardUrl(target, path);

        return Results.Json(new
        {
            message = "Request would be forwarded",
            route = route.Name,
            target = target.Name,
            forwardUrl,
            timestamp = DateTime.UtcNow
        });
    }
    catch (DotNetApiGateway.Exceptions.RouteNotFoundException ex)
    {
        context.Response.StatusCode = 404;
        return Results.Json(new { error = ex.Message, errorCode = ex.ErrorCode });
    }
    catch (DotNetApiGateway.Exceptions.GatewayException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        return Results.Json(new { error = ex.Message, errorCode = ex.ErrorCode });
    }
});

app.Run();
