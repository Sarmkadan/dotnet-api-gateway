using System;
using System.Text.Json;

namespace DotNetApiGateway.Repositories
{
    /// <summary>
    /// JSON serialization helpers for <see cref="CircuitBreakerRepository"/>.
    /// </summary>
    public static class CircuitBreakerRepositoryJsonExtensions
    {
        // Cached serializer options with camel‑case naming.
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the repository to a JSON string.
        /// </summary>
        /// <param name="value">The repository instance to serialize.</param>
        /// <param name="indented">If <c>true</c>, the output will be indented.</param>
        /// <returns>A JSON representation of the repository.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this CircuitBreakerRepository value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="CircuitBreakerRepository"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized repository, or <c>null</c> if the JSON is <c>null</c>, empty, or whitespace.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
        public static CircuitBreakerRepository? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<CircuitBreakerRepository>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="CircuitBreakerRepository"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="value">When the method returns, contains the deserialized repository if successful; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
        public static bool TryFromJson(string json, out CircuitBreakerRepository? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<CircuitBreakerRepository>(json, _options);
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
