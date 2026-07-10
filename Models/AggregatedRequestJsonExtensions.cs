using System;
using System.Text.Json;

namespace DotNetApiGateway.Models
{
    /// <summary>
    /// JSON serialization helpers for <see cref="AggregatedRequest"/>.
    /// </summary>
    public static class AggregatedRequestJsonExtensions
    {
        // Cached options with camel‑case naming policy.
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Preserve the default behavior for other settings; callers can request indentation.
        };

        /// <summary>
        /// Serializes the <see cref="AggregatedRequest"/> to a JSON string.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">If <c>true</c>, the output will be formatted with indentation.</param>
        /// <returns>A JSON representation of the instance.</returns>
        public static string ToJson(this AggregatedRequest value, bool indented = false)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            // Clone the cached options so we can set WriteIndented without affecting other calls.
            var options = new JsonSerializerOptions(_options)
            {
                WriteIndented = indented
            };

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into an <see cref="AggregatedRequest"/>.
        /// </summary>
        /// <param name="json">The JSON payload.</param>
        /// <returns>The deserialized <see cref="AggregatedRequest"/> instance, or <c>null</c> if the JSON is empty.</returns>
        public static AggregatedRequest? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<AggregatedRequest>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into an <see cref="AggregatedRequest"/>.
        /// </summary>
        /// <param name="json">The JSON payload.</param>
        /// <param name="value">When this method returns, contains the deserialized value if the operation succeeded; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        public static bool TryFromJson(string json, out AggregatedRequest? value)
        {
            try
            {
                value = FromJson(json);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
