#nullable enable
using System;
using Microsoft.AspNetCore.Http;

namespace DotNetApiGateway.Extensions
{
    public static class HttpContextGatewayExtensions
    {
        /// <summary>
        /// Retrieves the client IP address, honoring the X-Forwarded-For header if present.
        /// </summary>
        public static string GetClientIp(this HttpContext context)
        {
            const string xForwardedForHeader = "X-Forwarded-For";

            if (context.Request.Headers.TryGetValue(xForwardedForHeader, out var xffValues))
            {
                var xff = xffValues.ToString();
                if (!string.IsNullOrWhiteSpace(xff))
                {
                    // X-Forwarded-For may contain a comma-separated list of IPs; take the first one.
                    var firstIp = xff.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(firstIp))
                    {
                        return firstIp;
                    }
                }
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the correlation ID from the request headers or generates a new one if missing.
        /// The correlation ID is stored in the X-Correlation-ID header.
        /// </summary>
        public static string GetCorrelationId(this HttpContext context)
        {
            const string correlationHeader = "X-Correlation-ID";

            if (context.Request.Headers.TryGetValue(correlationHeader, out var values) &&
                !string.IsNullOrWhiteSpace(values))
            {
                return values.ToString();
            }

            var correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[correlationHeader] = correlationId;
            return correlationId;
        }

        /// <summary>
        /// Determines whether the current request is a WebSocket request.
        /// </summary>
        public static bool IsWebSocketRequest(this HttpContext context)
        {
            return context.WebSockets.IsWebSocketRequest;
        }
    }
}
