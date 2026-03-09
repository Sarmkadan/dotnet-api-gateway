#nullable enable

namespace DotNetApiGateway.Services;

/// <summary>
/// Transforms an upstream HTTP response before it is forwarded to the client.
/// Implementations can inject headers, rewrite body content, or modify status codes.
/// </summary>
public interface IResponseTransformer
{
    /// <summary>
    /// Applies transformations to the upstream response according to the route's configuration.
    /// </summary>
    /// <param name="response">The raw response received from the upstream service.</param>
    /// <param name="route">The matched gateway route whose policies drive the transformation.</param>
    /// <returns>The (possibly modified) response message.</returns>
    Task<HttpResponseMessage> TransformAsync(HttpResponseMessage response, GatewayRoute route);
}
