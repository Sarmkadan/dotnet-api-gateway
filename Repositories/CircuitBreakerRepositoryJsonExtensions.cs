using System.Text.Json;

namespace DotNetApiGateway.Repositories
{
    /// <summary>
    /// JSON serialization helpers for <see cref="CircuitBreakerRepository"/>.
    /// </summary>
    public static class CircuitBreakerRepositoryJsonExtensions
    {
        // Cached serializer options with camel‑case naming.
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serialises the repository to a JSON string.
        /// </summary>
        /// <param name="value">The repository instance to serialise.</param>
        /// <param name="indented">If <c>true</c>, the output will be indented.</param>
        /// <returns>A JSON representation of the repository.</returns>
        public static string ToJson(this CircuitBreakerRepository value, bool indented = false)
        {
            var options = indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserialises a JSON string into a <see cref="CircuitBreakerRepository"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialised repository, or <c>null</c> if the JSON is <c>null</c> or empty.</returns>
        public static CircuitBreakerRepository? FromJson(string json)
        {
            return JsonSerializer.Deserialize<CircuitBreakerRepository>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialise a JSON string into a <see cref="CircuitBreakerRepository"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="value">When the method returns, contains the deserialised repository if successful; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        public static bool TryFromJson(string json, out CircuitBreakerRepository? value)
        {
            try
            {
                value = JsonSerializer.Deserialize<CircuitBreakerRepository>(json, _options);
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
