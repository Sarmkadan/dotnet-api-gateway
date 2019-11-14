using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Exceptions
{
    /// <summary>
    /// Extension methods that add useful functionality to <see cref="GatewayException"/>.
    /// </summary>
    public static class GatewayExceptionExtensions
    {
        /// <summary>
        /// Determines whether the exception represents a client‑side error (HTTP 4xx).
        /// </summary>
        public static bool IsClientError(this GatewayException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            return ex.StatusCode >= 400 && ex.StatusCode < 500;
        }

        /// <summary>
        /// Determines whether the exception represents a server‑side error (HTTP 5xx).
        /// </summary>
        public static bool IsServerError(this GatewayException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            return ex.StatusCode >= 500 && ex.StatusCode < 600;
        }

        /// <summary>
        /// Serialises the exception details (message, error code and status code) to a JSON string.
        /// </summary>
        public static string ToJson(this GatewayException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            var payload = new
            {
                ex.Message,
                ex.ErrorCode,
                ex.StatusCode
            };

            return JsonSerializer.Serialize(payload);
        }

        /// <summary>
        /// Logs the exception using the supplied <see cref="ILogger"/>.
        /// The log entry includes the message, error code and status code.
        /// </summary>
        public static void Log(this GatewayException ex, ILogger logger)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            logger.LogError(
                ex,
                "GatewayException occurred. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                ex.ErrorCode,
                ex.StatusCode);
        }
    }
}
