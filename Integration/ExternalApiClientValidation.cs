#nullable enable

namespace DotNetApiGateway.Integration;

using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="ExternalApiClient"/> instances and their method parameters.
/// </summary>
public static class ExternalApiClientValidation
{
	/// <summary>
	/// Validates that an <see cref="ExternalApiClient"/> instance is properly initialized.
	/// </summary>
	/// <param name="value">The client instance to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
	public static IReadOnlyList<string> Validate(this ExternalApiClient? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		return Array.Empty<string>();
	}

	/// <summary>
	/// Checks whether an <see cref="ExternalApiClient"/> instance is valid.
	/// </summary>
	/// <param name="value">The client instance to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValid(this ExternalApiClient? value) => value is not null;

	/// <summary>
	/// Ensures that an <see cref="ExternalApiClient"/> instance is valid, throwing an exception if not.
	/// </summary>
	/// <param name="value">The client instance to validate.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
	public static void EnsureValid(this ExternalApiClient? value) =>
		ArgumentNullException.ThrowIfNull(value);

	/// <summary>
	/// Validates the endpoint parameter for HTTP requests.
	/// </summary>
	/// <param name="endpoint">The endpoint URL to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="endpoint"/> is null.</exception>
	public static IReadOnlyList<string> ValidateEndpoint(string? endpoint)
	{
		ArgumentException.ThrowIfNullOrEmpty(endpoint);

		var problems = new List<string>();

		if (string.IsNullOrWhiteSpace(endpoint))
		{
			problems.Add("Endpoint cannot be empty or whitespace.");
		}
		else if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
		{
			problems.Add($"Endpoint '{endpoint}' is not a valid absolute URI.");
		}
		else if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			&& !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			problems.Add("Endpoint should use http:// or https:// scheme.");
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Checks whether an endpoint is valid.
	/// </summary>
	/// <param name="endpoint">The endpoint URL to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidEndpoint(string? endpoint)
	{
		return ValidateEndpoint(endpoint).Count == 0;
	}

	/// <summary>
	/// Ensures that an endpoint is valid, throwing an exception if not.
	/// </summary>
	/// <param name="endpoint">The endpoint URL to validate.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="endpoint"/> is not valid.</exception>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="endpoint"/> is null.</exception>
	public static void EnsureValidEndpoint(string? endpoint)
	{
		var problems = ValidateEndpoint(endpoint);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"Endpoint is not valid. Problems: {string.Join(" ", problems)}");
		}
	}

	/// <summary>
	/// Validates request data for POST/PUT operations.
	/// </summary>
	/// <typeparam name="TRequest">The type of request data.</typeparam>
	/// <param name="data">The request data to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	public static IReadOnlyList<string> ValidateRequestData<TRequest>(TRequest? data) where TRequest : class
	{
		var problems = new List<string>();

		if (data is null)
		{
			problems.Add("Request data cannot be null.");
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Checks whether request data is valid.
	/// </summary>
	/// <typeparam name="TRequest">The type of request data.</typeparam>
	/// <param name="data">The request data to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidRequestData<TRequest>(TRequest? data) where TRequest : class
	{
		return ValidateRequestData(data).Count == 0;
	}

	/// <summary>
	/// Ensures that request data is valid, throwing an exception if not.
	/// </summary>
	/// <typeparam name="TRequest">The type of request data.</typeparam>
	/// <param name="data">The request data to validate.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="data"/> is not valid.</exception>
	public static void EnsureValidRequestData<TRequest>(TRequest? data) where TRequest : class
	{
		var problems = ValidateRequestData(data);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"Request data is not valid. Problems: {string.Join(" ", problems)}");
		}
	}

	/// <summary>
	/// Validates HTTP method for SendAsync operations.
	/// </summary>
	/// <param name="method">The HTTP method to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> is null.</exception>
	public static IReadOnlyList<string> ValidateHttpMethod(System.Net.Http.HttpMethod? method)
	{
		ArgumentNullException.ThrowIfNull(method);

		var problems = new List<string>();

		if (method == System.Net.Http.HttpMethod.Get
			|| method == System.Net.Http.HttpMethod.Post
			|| method == System.Net.Http.HttpMethod.Put
			|| method == System.Net.Http.HttpMethod.Delete
			|| method == System.Net.Http.HttpMethod.Patch
			|| method == System.Net.Http.HttpMethod.Head
			|| method == System.Net.Http.HttpMethod.Options)
		{
			return problems.AsReadOnly();
		}

		problems.Add($"HTTP method '{method}' is not a standard method.");
		return problems.AsReadOnly();
	}

	/// <summary>
	/// Checks whether an HTTP method is valid.
	/// </summary>
	/// <param name="method">The HTTP method to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidHttpMethod(System.Net.Http.HttpMethod? method)
	{
		return ValidateHttpMethod(method).Count == 0;
	}

	/// <summary>
	/// Ensures that an HTTP method is valid, throwing an exception if not.
	/// </summary>
	/// <param name="method">The HTTP method to validate.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="method"/> is not valid.</exception>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> is null.</exception>
	public static void EnsureValidHttpMethod(System.Net.Http.HttpMethod? method)
	{
		var problems = ValidateHttpMethod(method);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"HTTP method is not valid. Problems: {string.Join(" ", problems)}");
		}
	}

	/// <summary>
	/// Validates request content for SendAsync operations.
	/// </summary>
	/// <param name="content">The content to validate.</param>
	/// <param name="contentType">The content type to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	public static IReadOnlyList<string> ValidateRequestContent(string? content, string? contentType = null)
	{
		var problems = new List<string>();

		if (string.IsNullOrEmpty(content))
		{
			if (string.IsNullOrEmpty(contentType))
			{
				problems.Add("Content cannot be null or empty when contentType is not specified.");
			}
			// Empty content with a content type is valid (e.g., DELETE requests)
		}
		else
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				problems.Add("Content cannot be whitespace.");
			}

			if (contentType is not null && string.IsNullOrWhiteSpace(contentType))
			{
				problems.Add("Content type cannot be whitespace.");
			}
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Checks whether request content is valid.
	/// </summary>
	/// <param name="content">The content to check.</param>
	/// <param name="contentType">The content type to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidRequestContent(string? content, string? contentType = null)
	{
		return ValidateRequestContent(content, contentType).Count == 0;
	}

	/// <summary>
	/// Ensures that request content is valid, throwing an exception if not.
	/// </summary>
	/// <param name="content">The content to validate.</param>
	/// <param name="contentType">The content type to validate.</param>
	/// <exception cref="ArgumentException">Thrown if content or contentType is not valid.</exception>
	public static void EnsureValidRequestContent(string? content, string? contentType = null)
	{
		var problems = ValidateRequestContent(content, contentType);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"Request content is not valid. Problems: {string.Join(" ", problems)}");
		}
	}

	/// <summary>
	/// Validates request headers for SendAsync operations.
	/// </summary>
	/// <param name="headers">The headers to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	public static IReadOnlyList<string> ValidateRequestHeaders(Dictionary<string, string>? headers)
	{
		var problems = new List<string>();

		if (headers is null)
		{
			return problems.AsReadOnly();
		}

		if (headers.Count == 0)
		{
			problems.Add("Headers dictionary cannot be empty.");
		}

		foreach (var header in headers)
		{
			if (string.IsNullOrWhiteSpace(header.Key))
			{
				problems.Add("Header key cannot be null or whitespace.");
				continue;
			}

			if (string.IsNullOrWhiteSpace(header.Value))
			{
				problems.Add($"Header value for '{header.Key}' cannot be null or whitespace.");
			}
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Checks whether request headers are valid.
	/// </summary>
	/// <param name="headers">The headers to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidRequestHeaders(Dictionary<string, string>? headers)
	{
		return ValidateRequestHeaders(headers).Count == 0;
	}

	/// <summary>
	/// Ensures that request headers are valid, throwing an exception if not.
	/// </summary>
	/// <param name="headers">The headers to validate.</param>
	/// <exception cref="ArgumentException">Thrown if headers are not valid.</exception>
	public static void EnsureValidRequestHeaders(Dictionary<string, string>? headers)
	{
		var problems = ValidateRequestHeaders(headers);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"Request headers are not valid. Problems: {string.Join(" ", problems)}");
		}
	}

	/// <summary>
	/// Validates cancellation token usage.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to validate.</param>
	/// <returns>A list of human-readable validation problems; empty if valid.</returns>
	public static IReadOnlyList<string> ValidateCancellationToken(CancellationToken cancellationToken)
	{
		// CancellationToken is a struct, so it can't be null
		// We can check if it's in a canceled state, but that's not typically a validation concern
		// This method exists for API consistency

		return Array.Empty<string>();
	}

	/// <summary>
	/// Checks whether a cancellation token is valid.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to check.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public static bool IsValidCancellationToken(CancellationToken cancellationToken)
	{
		return ValidateCancellationToken(cancellationToken).Count == 0;
	}

	/// <summary>
	/// Ensures that a cancellation token is valid, throwing an exception if not.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to validate.</param>
	/// <exception cref="ArgumentException">Thrown if cancellationToken is not valid.</exception>
	public static void EnsureValidCancellationToken(CancellationToken cancellationToken)
	{
		var problems = ValidateCancellationToken(cancellationToken);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"Cancellation token is not valid. Problems: {string.Join(" ", problems)}");
		}
	}
}

file static class ExternalApiClientExtensions
{
	public static System.Net.Http.HttpClient? GetHttpClient(this ExternalApiClient client)
	{
		// Use reflection to access the private field
		var field = typeof(ExternalApiClient).GetField("_httpClient",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return field?.GetValue(client) as System.Net.Http.HttpClient;
	}

	public static DotNetApiGateway.Integration.RetryPolicy? GetRetryPolicy(this ExternalApiClient client)
	{
		// Use reflection to access the private field
		var field = typeof(ExternalApiClient).GetField("_retryPolicy",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return field?.GetValue(client) as DotNetApiGateway.Integration.RetryPolicy;
	}
}