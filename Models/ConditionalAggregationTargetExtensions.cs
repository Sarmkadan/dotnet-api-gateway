#nullable enable

using System.Text.Json;
using DotNetApiGateway.Models;
using HttpMethod = DotNetApiGateway.Constants.HttpMethod;

namespace DotNetApiGateway.Models;

/// <summary>
/// Extension methods for <see cref="ConditionalAggregationTarget"/> providing useful utilities for
/// request building, validation, and conditional logic.
/// </summary>
public static class ConditionalAggregationTargetExtensions
{
    /// <summary>
    /// Creates a deep copy of the <see cref="ConditionalAggregationTarget"/> to avoid modifying the original.
    /// </summary>
    /// <param name="target">The target to clone</param>
    /// <returns>A new instance with copied values</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null</exception>
    public static ConditionalAggregationTarget Clone(this ConditionalAggregationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return new ConditionalAggregationTarget
        {
            Id = target.Id,
            UpstreamUrl = target.UpstreamUrl,
            JsonPathCondition = target.JsonPathCondition,
            Method = target.Method,
            Headers = target.Headers?.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
            Body = target.Body,
            TimeoutSeconds = target.TimeoutSeconds,
            Optional = target.Optional
        };
    }

    /// <summary>
    /// Determines whether this target should be used based on the provided JSON payload.
    /// Returns true if no JsonPathCondition is set, or if the condition evaluates to true.
    /// </summary>
    /// <param name="target">The target to check</param>
    /// <param name="jsonPayload">The JSON payload to evaluate against</param>
    /// <returns>True if the target should be used; false otherwise</returns>
    public static bool ShouldUse(this ConditionalAggregationTarget target, string? jsonPayload)
    {
        ArgumentNullException.ThrowIfNull(target);

        // If no condition is specified, always use the target
        if (string.IsNullOrEmpty(target.JsonPathCondition))
            return true;

        // If payload is null or empty, and target is not optional, don't use it
        if (string.IsNullOrEmpty(jsonPayload))
            return target.Optional;

        try
        {
            var jsonDocument = JsonDocument.Parse(jsonPayload);
            var root = jsonDocument.RootElement;

            var pathSegments = target.JsonPathCondition.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var current = root;
            foreach (var segment in pathSegments)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var property))
                {
                    current = property;
                }
                else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index) && current.TryGetArrayElement(index, out var arrayElement))
                {
                    current = arrayElement;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return target.Optional;
        }
    }

    /// <summary>
    /// Adds or updates a header in the target's headers collection.
    /// Creates the headers dictionary if it doesn't exist.
    /// </summary>
    /// <param name="target">The target to modify</param>
    /// <param name="name">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>The modified target (for fluent chaining)</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace</exception>
    public static ConditionalAggregationTarget WithHeader(this ConditionalAggregationTarget target, string name, string value)
        => target.WithHeaderCore(name, value);

    private static ConditionalAggregationTarget WithHeaderCore(this ConditionalAggregationTarget target, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        target.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        target.Headers[name] = value;
        return target;
    }

    /// <summary>
    /// Sets the request body from an object that will be serialized to JSON.
    /// </summary>
    /// <param name="target">The target to modify</param>
    /// <param name="bodyObject">The object to serialize as JSON body</param>
    /// <param name="jsonSerializerOptions">Optional serializer options</param>
    /// <returns>The modified target (for fluent chaining)</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> or <paramref name="bodyObject"/> is null</exception>
    public static ConditionalAggregationTarget WithJsonBody(this ConditionalAggregationTarget target, object bodyObject, JsonSerializerOptions? jsonSerializerOptions = null)
        => target.WithJsonBodyCore(bodyObject, jsonSerializerOptions);

    private static ConditionalAggregationTarget WithJsonBodyCore(this ConditionalAggregationTarget target, object bodyObject, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(bodyObject);

        jsonSerializerOptions ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        target.Body = JsonSerializer.Serialize(bodyObject, jsonSerializerOptions);
        return target;
    }

    /// <summary>
    /// Validates the target and throws descriptive exceptions for common issues.
    /// </summary>
    /// <param name="target">The target to validate</param>
    /// <param name="includeHeadersCheck">Whether to validate headers if present</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateWithDetails(this ConditionalAggregationTarget target, bool includeHeadersCheck = true)
    {
        ArgumentNullException.ThrowIfNull(target);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(target.UpstreamUrl))
            errors.Add("UpstreamUrl cannot be empty for ConditionalAggregationTarget");

        if (target.TimeoutSeconds is < 1 or > 300)
            errors.Add("TimeoutSeconds must be between 1 and 300 seconds");

        if (includeHeadersCheck && target.Headers is { Count: > 0 })
        {
            foreach (var header in target.Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                    errors.Add("Header name cannot be null or whitespace");
                else if (string.IsNullOrWhiteSpace(header.Value))
                    errors.Add($"Header '{header.Key}' value cannot be null or whitespace");
            }
        }

        if (errors.Count > 0)
            throw new ArgumentException(string.Join(Environment.NewLine, errors));

        // Also validate using the built-in method
        target.Validate();
    }

    /// <summary>
    /// Creates a new target with the same configuration but a different HTTP method.
    /// </summary>
    /// <param name="target">The original target</param>
    /// <param name="method">The new HTTP method</param>
    /// <returns>A new target instance with the updated method</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null</exception>
    public static ConditionalAggregationTarget WithMethod(this ConditionalAggregationTarget target, HttpMethod method)
        => target.WithMethodCore(method);

    private static ConditionalAggregationTarget WithMethodCore(this ConditionalAggregationTarget target, HttpMethod method)
    {
        ArgumentNullException.ThrowIfNull(target);

        var clone = target.Clone();
        clone.Method = method;
        return clone;
    }

    /// <summary>
    /// Creates a new target with the same configuration but a different timeout.
    /// </summary>
    /// <param name="target">The original target</param>
    /// <param name="timeoutSeconds">The new timeout in seconds</param>
    /// <returns>A new target instance with the updated timeout</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null</exception>
    public static ConditionalAggregationTarget WithTimeout(this ConditionalAggregationTarget target, int timeoutSeconds)
        => target.WithTimeoutCore(timeoutSeconds);

    private static ConditionalAggregationTarget WithTimeoutCore(this ConditionalAggregationTarget target, int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(target);

        var clone = target.Clone();
        clone.TimeoutSeconds = timeoutSeconds;
        return clone;
    }

    private static bool TryGetArrayElement(this JsonElement element, int index, out JsonElement result)
    {
        result = default;
        if (element.ValueKind != JsonValueKind.Array || index < 0 || index >= element.GetArrayLength())
            return false;

        result = element[index];
        return true;
    }
}