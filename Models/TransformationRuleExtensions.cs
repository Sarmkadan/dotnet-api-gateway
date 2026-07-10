#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// Provides extension methods for <see cref="TransformationRule"/> to simplify common operations.
/// </summary>
public static class TransformationRuleExtensions
{
    /// <summary>
    /// Creates a new transformation rule for adding a header to outgoing requests.
    /// </summary>
    /// <param name="key">The header name to add.</param>
    /// <param name="value">The header value to set.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    public static TransformationRule AddRequestHeader(
        this TransformationRule rule,
        string key,
        string value,
        string? description = null,
        int order = 0)
    {
        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.AddHeader;
        rule.Key = key ?? throw new ArgumentNullException(nameof(key));
        rule.Value = value ?? throw new ArgumentNullException(nameof(value));
        rule.Description = description ?? $"Add header '{key}' with value '{value}' to outgoing request";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for setting a header on responses.
    /// </summary>
    /// <param name="key">The header name to set.</param>
    /// <param name="value">The header value to set.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    public static TransformationRule SetResponseHeader(
        this TransformationRule rule,
        string key,
        string value,
        string? description = null,
        int order = 0)
    {
        rule.Phase = TransformationPhase.Response;
        rule.Operation = TransformationOperation.SetHeader;
        rule.Key = key ?? throw new ArgumentNullException(nameof(key));
        rule.Value = value ?? throw new ArgumentNullException(nameof(value));
        rule.Description = description ?? $"Set response header '{key}' to '{value}'";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for removing a query parameter from requests.
    /// </summary>
    /// <param name="paramName">The query parameter name to remove.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    public static TransformationRule RemoveRequestQueryParam(
        this TransformationRule rule,
        string paramName,
        string? description = null,
        int order = 0)
    {
        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.RemoveQueryParam;
        rule.Key = paramName ?? throw new ArgumentNullException(nameof(paramName));
        rule.Description = description ?? $"Remove query parameter '{paramName}' from outgoing request";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for rewriting path prefixes.
    /// </summary>
    /// <param name="oldPrefix">The path prefix to match and replace.</param>
    /// <param name="newPrefix">The replacement path prefix.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    public static TransformationRule RewritePathPrefix(
        this TransformationRule rule,
        string oldPrefix,
        string newPrefix,
        string? description = null,
        int order = 0)
    {
        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.RewritePathPrefix;
        rule.Key = oldPrefix ?? throw new ArgumentNullException(nameof(oldPrefix));
        rule.Value = newPrefix ?? throw new ArgumentNullException(nameof(newPrefix));
        rule.Description = description ?? $"Rewrite path prefix from '{oldPrefix}' to '{newPrefix}'";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Checks if this rule operates on headers (either AddHeader or SetHeader).
    /// </summary>
    /// <returns>True if the rule is a header operation; otherwise false.</returns>
    public static bool IsHeaderOperation(this TransformationRule rule)
    {
        return rule.Operation is TransformationOperation.AddHeader or TransformationOperation.SetHeader or TransformationOperation.RemoveHeader;
    }

    /// <summary>
    /// Checks if this rule operates on query parameters (either AddQueryParam, SetQueryParam, or RemoveQueryParam).
    /// </summary>
    /// <returns>True if the rule is a query parameter operation; otherwise false.</returns>
    public static bool IsQueryParamOperation(this TransformationRule rule)
    {
        return rule.Operation is TransformationOperation.AddQueryParam or TransformationOperation.SetQueryParam or TransformationOperation.RemoveQueryParam;
    }

    /// <summary>
    /// Creates a deep clone of this transformation rule.
    /// </summary>
    /// <returns>A new <see cref="TransformationRule"/> instance with identical property values.</returns>
    public static TransformationRule Clone(this TransformationRule rule)
    {
        return new TransformationRule
        {
            Id = rule.Id,
            Description = rule.Description,
            Phase = rule.Phase,
            Operation = rule.Operation,
            Key = rule.Key,
            Value = rule.Value,
            Order = rule.Order,
            IsEnabled = rule.IsEnabled
        };
    }

    /// <summary>
    /// Determines whether this rule should be applied based on its enabled state and validation.
    /// </summary>
    /// <returns>True if the rule can be safely applied; otherwise false.</returns>
    public static bool CanApply(this TransformationRule rule)
    {
        return rule.IsEnabled && !string.IsNullOrWhiteSpace(rule.Key);
    }
}