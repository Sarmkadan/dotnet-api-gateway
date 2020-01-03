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
        /// <param name="ex">The exception to check.</param>
        /// <returns><see langword="true"/> if the exception represents a client-side error; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <see langword="null"/>.</exception>
        public static bool IsClientError(this GatewayException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return ex.StatusCode is >= 400 and < 500;
        }

        /// <summary>
        /// Determines whether the exception represents a server‑side error (HTTP 5xx).
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns><see langword="true"/> if the exception represents a server-side error; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <see langword="null"/>.</exception>
        public static bool IsServerError(this GatewayException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return ex.StatusCode is >= 500 and < 600;
        }

        /// <summary>
        /// Serialises the exception details (message, error code and status code) to a JSON string.
        /// </summary>
        /// <param name="ex">The exception to serialize.</param>
        /// <returns>A JSON string containing the exception details.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <see langword="null"/>.</exception>
        public static string ToJson(this GatewayException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            var payload = new
            {
                Message = ex.Message,
                ErrorCode = ex.ErrorCode,
                StatusCode = ex.StatusCode
            };

            return JsonSerializer.Serialize(payload);
        }

        /// <summary>
        /// Logs the exception using the supplied <see cref="ILogger"/>.
        /// The log entry includes the message, error code and status code.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="logger">The logger instance to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
        public static void Log(this GatewayException ex, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(ex);
            ArgumentNullException.ThrowIfNull(logger);

            logger.LogError(
                ex,
                "GatewayException occurred. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                ex.ErrorCode,
                ex.StatusCode);
        }
    }
}
