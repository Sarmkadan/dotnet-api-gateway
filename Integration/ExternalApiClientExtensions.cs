using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetApiGateway.Integration
{
    /// <summary>
    /// Extension methods that make working with <see cref="ExternalApiClient"/> more convenient.
    /// </summary>
    public static class ExternalApiClientExtensions
    {
        /// <summary>
        /// Sends a GET request and returns the deserialized JSON payload.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="client">The <see cref="ExternalApiClient"/> instance.</param>
        /// <param name="endpoint">The relative endpoint to call.</param>
        /// <returns>The deserialized response, or <c>null</c> if the request failed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="endpoint"/> is empty or whitespace.</exception>
        public static async Task<T?> GetJsonAsync<T>(this ExternalApiClient client, string endpoint)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

            return await client.GetAsync<T>(endpoint);
        }

        /// <summary>
        /// Sends a request using the specified HTTP method. This overload
        /// places the optional <paramref name="content"/> parameter before <paramref name="contentType"/>
        /// for a more natural call signature.
        /// </summary>
        /// <param name="client">The <see cref="ExternalApiClient"/> instance.</param>
        /// <param name="endpoint">The relative endpoint to call.</param>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="content">The request body as a string (optional).</param>
        /// <param name="contentType">The MIME type of the request body (optional).</param>
        /// <returns>The raw <see cref="HttpResponseMessage"/> from the server.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="endpoint"/> is empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> is <c>null</c>.</exception>
        public static async Task<HttpResponseMessage> SendAsyncWithMethod(
            this ExternalApiClient client,
            string endpoint,
            System.Net.Http.HttpMethod method,
            string? content = null,
            string? contentType = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
            ArgumentNullException.ThrowIfNull(method);

            return await client.SendAsync(endpoint, method, contentType, content);
        }

        /// <summary>
        /// Sends an <see cref="HttpRequestMessage"/> and throws if the response indicates failure.
        /// </summary>
        /// <param name="client">The <see cref="ExternalApiClient"/> instance.</param>
        /// <param name="request">The prepared request message.</param>
        /// <returns>The successful <see cref="HttpResponseMessage"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="HttpRequestException">Thrown when the response status code does not indicate success.</exception>
        public static async Task<HttpResponseMessage> SendRequestAndEnsureSuccessAsync(
            this ExternalApiClient client,
            HttpRequestMessage request)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(request);

            var response = await client.SendRequestAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}