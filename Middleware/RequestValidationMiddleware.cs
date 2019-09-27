#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Middleware;

using DotNetApiGateway.Utilities;

/// <summary>
/// Middleware for validating incoming requests before processing.
/// Ensures request size limits, content types, and required headers are valid.
/// </summary>
public sealed class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;
    private readonly long _maxRequestBodySize;
    private readonly string[] _allowedContentTypes = new[] { "application/json", "application/x-www-form-urlencoded", "text/plain" };

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger, long maxBodySize = 10 * 1024 * 1024)
    {
        _next = next;
        _logger = logger;
        _maxRequestBodySize = maxBodySize;
    }

    /// <summary>
    /// Validate request before forwarding to next middleware.
    /// Checks content length, content type, and essential headers.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Validate content length
        if (context.Request.ContentLength.HasValue && context.Request.ContentLength > _maxRequestBodySize)
        {
            _logger.LogWarning("Request body exceeds maximum size: {ContentLength} bytes", context.Request.ContentLength);
            context.Response.StatusCode = 413; // Payload Too Large
            await context.Response.WriteAsJsonAsync(new { error = "Request body exceeds maximum allowed size" });
            return;
        }

        // Validate content type for POST/PUT requests
        if ((context.Request.Method == "POST" || context.Request.Method == "PUT") && !string.IsNullOrEmpty(context.Request.ContentType))
        {
            var contentType = context.Request.ContentType.Split(';')[0].Trim();
            if (!_allowedContentTypes.Contains(contentType))
            {
                _logger.LogWarning("Invalid content type: {ContentType}", contentType);
                context.Response.StatusCode = 415; // Unsupported Media Type
                await context.Response.WriteAsJsonAsync(new { error = "Unsupported content type" });
                return;
            }
        }

        // Validate required headers
        var validationResult = ValidateHeaders(context.Request.Headers);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid headers: {Issues}", string.Join(", ", validationResult.Issues));
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid headers", details = validationResult.Issues });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Validate request headers for required fields and proper format.
    /// </summary>
    private HeaderValidationResult ValidateHeaders(IHeaderDictionary headers)
    {
        var result = new HeaderValidationResult { IsValid = true, Issues = new List<string>() };

        // Check for Host header
        if (!headers.ContainsKey("Host"))
        {
            result.Issues.Add("Missing required 'Host' header");
            result.IsValid = false;
        }

        // Check User-Agent header (informational, not critical)
        if (!headers.ContainsKey("User-Agent"))
        {
            result.Issues.Add("Missing recommended 'User-Agent' header");
        }

        return result;
    }

    private class HeaderValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
    }
}
