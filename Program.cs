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
        stats = "/gateway/stats",
        circuitBreakers = "/gateway/circuit-breakers",
        rateLimits = "/api/GatewayManagement/rate-limits/{key}"
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

// Request metrics dashboard endpoint
app.MapGet("/gateway/stats", (DotNetApiGateway.Services.MetricsService metricsService) =>
{
    var metrics = metricsService.GetMetrics();
    return Results.Ok(new
    {
        timestamp = DateTime.UtcNow,
        uptime = metrics.Uptime.ToString(@"d\.hh\:mm\:ss"),
        totalRequests = metrics.TotalRequests,
        successfulRequests = metrics.SuccessfulRequests,
        failedRequests = metrics.FailedRequests,
        successRate = Math.Round(metrics.SuccessRate, 2),
        averageResponseTimeMs = Math.Round(metrics.AverageResponseTimeMs, 2),
        requestsPerSecond = Math.Round(metrics.GetRequestsPerSecond(), 2),
        statusCodeDistribution = metrics.StatusCodeDistribution,
        routes = metrics.RouteMetrics.Select(r => new
        {
            routeId = r.RouteId,
            requestCount = r.RequestCount,
            averageResponseTimeMs = Math.Round(r.GetAverageResponseTime(), 2),
            minResponseTimeMs = r.MinResponseTimeMs,
            maxResponseTimeMs = r.MaxResponseTimeMs,
            lastRequestAt = r.LastRequestAt
        })
    });
}).WithName("GetStats");

// Default routing and forwarding endpoint
app.MapFallback(async (
    HttpContext context,
    DotNetApiGateway.Services.RoutingService routingService,
    DotNetApiGateway.Services.RequestAggregationService aggregationService,
    DotNetApiGateway.Integration.ExternalApiClient externalApiClient,
    DotNetApiGateway.Services.IResponseTransformer responseTransformer,
    ILogger<Program> logger) =>
{
    // Check if route resolution failed in RoutingMiddleware
    if (context.Items.TryGetValue("RouteNotFoundException", out var notFoundEx) && notFoundEx is RouteNotFoundException rnf)
    {
        context.Response.StatusCode = 404;
        return Results.Json(new { error = rnf.Message, errorCode = rnf.ErrorCode });
    }
    if (context.Items.TryGetValue("RouteResolutionError", out var resolutionEx) && resolutionEx is Exception resEx)
    {
        logger.LogError(resEx, "Error during route resolution in MapFallback.");
        context.Response.StatusCode = 500;
        return Results.Json(new { error = $"Error during route resolution: {resEx.Message}" });
    }

    // Retrieve the resolved route from HttpContext.Items
    if (!context.Items.TryGetValue("GatewayRoute", out var routeObj) || routeObj is not GatewayRoute route)
    {
        context.Response.StatusCode = 404;
        return Results.Json(new { error = "Route not found", path = context.Request.Path.Value, method = context.Request.Method });
    }

    // Handle Aggregation Policy
    if (route.AggregationPolicy?.Enabled == true)
    {
        string? requestBody = null;
        if (context.Request.ContentLength > 0 && context.Request.HasFormContentType == false) // Check for body, but not form data
        {
            using var reader = new StreamReader(context.Request.Body);
            requestBody = await reader.ReadToEndAsync();
        }

        try
        {
            var aggregatedResponse = await aggregationService.AggregateAsync(route.AggregationPolicy, requestBody);
            return Results.Json(aggregatedResponse); // Return the aggregated response
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during request aggregation for route {RouteId}", route.Id);
            context.Response.StatusCode = 500;
            return Results.Json(new { error = $"Aggregation failed: {ex.Message}" });
        }
    }

    // Existing forwarding logic (now actually performs the forward)
    try
    {
        var target = routingService.SelectTarget(route, context.Connection.RemoteIpAddress?.ToString());
        var forwardUrl = routingService.BuildForwardUrl(target, context.Request.Path.Value ?? "/");

        // Forward the request using ExternalApiClient
        // Need to read the request body for forwarding
        string? requestBody = null;
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // Rewind for potential downstream use
            }
        }
        
        using var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), forwardUrl)
        {
            Content = requestBody != null ? new StringContent(requestBody, System.Text.Encoding.UTF8, context.Request.ContentType ?? "application/json") : null
        };

        // Copy headers from incoming request to outgoing request
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue; // Host header is managed by HttpClient
            if (requestMessage.Headers.Contains(header.Key) == false)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
        
        // Timeout handling for individual target
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(target.TimeoutSeconds ?? route.TimeoutSeconds));

        var responseMessage = await externalApiClient.SendRequestAsync(requestMessage, cts.Token);

        // Apply response transformations (security headers, custom headers, etc.)
        responseMessage = await responseTransformer.TransformAsync(responseMessage, route);

        // Copy status code and headers from upstream response to downstream response
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in responseMessage.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await responseMessage.Content.CopyToAsync(context.Response.Body);
        return Results.Empty; // Indicate that the response has been handled
    }
    catch (DotNetApiGateway.Exceptions.RouteNotFoundException ex)
    {
        logger.LogWarning(ex, "Route not found during forwarding for route {RouteId}", route.Id);
        context.Response.StatusCode = 404;
        return Results.Json(new { error = ex.Message, errorCode = ex.ErrorCode });
    }
    catch (DotNetApiGateway.Exceptions.GatewayException ex)
    {
        logger.LogError(ex, "Gateway exception during forwarding for route {RouteId}", route.Id);
        context.Response.StatusCode = ex.StatusCode;
        return Results.Json(new { error = ex.Message, errorCode = ex.ErrorCode });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during request forwarding for route {RouteId}", route.Id);
        context.Response.StatusCode = 500;
        return Results.Json(new { error = $"Request forwarding failed: {ex.Message}" });
    }
});

app.Run();
