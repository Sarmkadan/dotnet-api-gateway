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
    public static ConditionalAggregationTarget Clone(this ConditionalAggregationTarget target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        return new ConditionalAggregationTarget
        {
            Id = target.Id,
            UpstreamUrl = target.UpstreamUrl,
            JsonPathCondition = target.JsonPathCondition,
            Method = target.Method,
            Headers = target.Headers is null ? null : new Dictionary<string, string>(target.Headers),
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
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        // If no condition is specified, always use the target
        if (string.IsNullOrEmpty(target.JsonPathCondition))
            return true;

        // If payload is null or empty, and target is not optional, don't use it
        if (string.IsNullOrEmpty(jsonPayload))
            return target.Optional;

        try
        {
            // Simple evaluation: check if the condition path exists in the JSON
            // This is a basic implementation that checks for existence of the path
            var jsonDocument = JsonDocument.Parse(jsonPayload);
            var root = jsonDocument.RootElement;

            // Try to navigate the JSON path (simplified approach)
            // For a real implementation, you'd use a proper JSONPath library
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
                    // Path doesn't exist
                    return false;
                }
            }

            // If we reached a value, it exists (even if it's null, empty string, etc.)
            return true;
        }
        catch
        {
            // If JSON parsing fails, don't use the target unless it's optional
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
    public static ConditionalAggregationTarget WithHeader(this ConditionalAggregationTarget target, string name, string value)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Header name cannot be null or whitespace", nameof(name));

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
    public static ConditionalAggregationTarget WithJsonBody(this ConditionalAggregationTarget target, object bodyObject, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (bodyObject is null)
            throw new ArgumentNullException(nameof(bodyObject));

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
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateWithDetails(this ConditionalAggregationTarget target, bool includeHeadersCheck = true)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(target.UpstreamUrl))
            errors.Add("UpstreamUrl cannot be empty for ConditionalAggregationTarget");

        if (target.TimeoutSeconds < 1 || target.TimeoutSeconds > 300)
            errors.Add("TimeoutSeconds must be between 1 and 300 seconds");

        if (includeHeadersCheck && target.Headers is not null)
        {
            foreach (var header in target.Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                    errors.Add("Header name cannot be null or whitespace");
                if (string.IsNullOrWhiteSpace(header.Value))
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
    public static ConditionalAggregationTarget WithMethod(this ConditionalAggregationTarget target, HttpMethod method)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

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
    public static ConditionalAggregationTarget WithTimeout(this ConditionalAggregationTarget target, int timeoutSeconds)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

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