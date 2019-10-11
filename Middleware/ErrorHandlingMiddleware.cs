#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Middleware;

using DotNetApiGateway.Exceptions;
using System.Net;

/// <summary>
/// Global error handling middleware that catches all exceptions and returns standardized error responses.
/// Ensures consistent error format across the gateway and logs all unhandled exceptions.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke middleware to catch and handle exceptions uniformly.
    /// Converts exceptions to HTTP responses with standardized format.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request pipeline");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Convert exception to HTTP response with appropriate status code and error format.
    /// Maps gateway exceptions to correct HTTP status codes.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        // Handle gateway-specific exceptions
        if (ex is RateLimitExceededException rateLimitEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            response = new ErrorResponse
            {
                ErrorCode = rateLimitEx.ErrorCode,
                Message = rateLimitEx.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (ex is CircuitBreakerException cbEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            response = new ErrorResponse
            {
                ErrorCode = cbEx.ErrorCode,
                Message = cbEx.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (ex is AuthenticationException authEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response = new ErrorResponse
            {
                ErrorCode = authEx.ErrorCode,
                Message = authEx.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (ex is RouteNotFoundException notFoundEx)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            response = new ErrorResponse
            {
                ErrorCode = notFoundEx.ErrorCode,
                Message = notFoundEx.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (ex is GatewayException gwEx)
        {
            context.Response.StatusCode = gwEx.StatusCode;
            response = new ErrorResponse
            {
                ErrorCode = gwEx.ErrorCode,
                Message = gwEx.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else
        {
            // Generic unhandled exception
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response = new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An internal server error occurred",
                Timestamp = DateTime.UtcNow
            };
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Standardized error response format used across the gateway.
/// </summary>
public sealed class ErrorResponse
{
    public string ErrorCode { get; set; } = "UNKNOWN_ERROR";
    public string Message { get; set; } = "An error occurred";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Details { get; set; }
}
