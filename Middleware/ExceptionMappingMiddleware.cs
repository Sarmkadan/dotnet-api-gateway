using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetApiGateway.Exceptions;
using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Middleware;

/// <summary>
/// Centralized middleware that maps known gateway exceptions to appropriate HTTP status codes
/// and returns a consistent JSON error payload.
/// </summary>
public class ExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMappingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthenticationException ex)
        {
            await WriteErrorResponse(context, ex, StatusCodes.Status401Unauthorized);
        }
        catch (RateLimitExceededException ex)
        {
            await WriteErrorResponse(context, ex, StatusCodes.Status429TooManyRequests);
        }
        catch (CircuitBreakerException ex)
        {
            await WriteErrorResponse(context, ex, StatusCodes.Status503ServiceUnavailable);
        }
        catch (RouteNotFoundException ex)
        {
            await WriteErrorResponse(context, ex, StatusCodes.Status404NotFound);
        }
        // Let any other exception bubble up – it will be handled by the generic catch in the endpoint
    }

    private static async Task WriteErrorResponse(HttpContext context, GatewayException ex, int statusCode)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                error = ex.Message,
                errorCode = ex.ErrorCode
            };

            // Use the existing JsonUtility for consistent serialization settings
            var json = JsonUtility.Serialize(payload);
            await context.Response.WriteAsync(json);
        }
    }
}
