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
    /// <param name="rule">The transformation rule to configure.</param>
    /// <param name="key">The header name to add.</param>
    /// <param name="value">The header value to set.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/>, <paramref name="key"/>, or <paramref name="value"/> is null.</exception>
    public static TransformationRule AddRequestHeader(
        this TransformationRule rule,
        string key,
        string value,
        string? description = null,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.AddHeader;
        rule.Key = key;
        rule.Value = value;
        rule.Description = description ?? $"Add header '{key}' with value '{value}' to outgoing request";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for setting a header on responses.
    /// </summary>
    /// <param name="rule">The transformation rule to configure.</param>
    /// <param name="key">The header name to set.</param>
    /// <param name="value">The header value to set.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/>, <paramref name="key"/>, or <paramref name="value"/> is null.</exception>
    public static TransformationRule SetResponseHeader(
        this TransformationRule rule,
        string key,
        string value,
        string? description = null,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        rule.Phase = TransformationPhase.Response;
        rule.Operation = TransformationOperation.SetHeader;
        rule.Key = key;
        rule.Value = value;
        rule.Description = description ?? $"Set response header '{key}' to '{value}'";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for removing a query parameter from requests.
    /// </summary>
    /// <param name="rule">The transformation rule to configure.</param>
    /// <param name="paramName">The query parameter name to remove.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> or <paramref name="paramName"/> is null.</exception>
    public static TransformationRule RemoveRequestQueryParam(
        this TransformationRule rule,
        string paramName,
        string? description = null,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(paramName);

        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.RemoveQueryParam;
        rule.Key = paramName;
        rule.Description = description ?? $"Remove query parameter '{paramName}' from outgoing request";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Creates a new transformation rule for rewriting path prefixes.
    /// </summary>
    /// <param name="rule">The transformation rule to configure.</param>
    /// <param name="oldPrefix">The path prefix to match and replace.</param>
    /// <param name="newPrefix">The replacement path prefix.</param>
    /// <param name="description">Optional description of the rule.</param>
    /// <param name="order">Evaluation order (lower runs first).</param>
    /// <returns>A configured <see cref="TransformationRule"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/>, <paramref name="oldPrefix"/>, or <paramref name="newPrefix"/> is null.</exception>
    public static TransformationRule RewritePathPrefix(
        this TransformationRule rule,
        string oldPrefix,
        string newPrefix,
        string? description = null,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(oldPrefix);
        ArgumentNullException.ThrowIfNull(newPrefix);

        rule.Phase = TransformationPhase.Request;
        rule.Operation = TransformationOperation.RewritePathPrefix;
        rule.Key = oldPrefix;
        rule.Value = newPrefix;
        rule.Description = description ?? $"Rewrite path prefix from '{oldPrefix}' to '{newPrefix}'";
        rule.Order = order;
        rule.IsEnabled = true;
        return rule;
    }

    /// <summary>
    /// Checks if this rule operates on headers (either AddHeader or SetHeader).
    /// </summary>
    /// <param name="rule">The transformation rule to check.</param>
    /// <returns>True if the rule is a header operation; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
    public static bool IsHeaderOperation(this TransformationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.Operation is TransformationOperation.AddHeader or TransformationOperation.SetHeader or TransformationOperation.RemoveHeader;
    }

    /// <summary>
    /// Checks if this rule operates on query parameters (either AddQueryParam, SetQueryParam, or RemoveQueryParam).
    /// </summary>
    /// <param name="rule">The transformation rule to check.</param>
    /// <returns>True if the rule is a query parameter operation; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
    public static bool IsQueryParamOperation(this TransformationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.Operation is TransformationOperation.AddQueryParam or TransformationOperation.SetQueryParam or TransformationOperation.RemoveQueryParam;
    }

    /// <summary>
    /// Creates a deep clone of this transformation rule.
    /// </summary>
    /// <param name="rule">The transformation rule to clone.</param>
    /// <returns>A new <see cref="TransformationRule"/> instance with identical property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
    public static TransformationRule Clone(this TransformationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

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
    /// <param name="rule">The transformation rule to check.</param>
    /// <returns>True if the rule can be safely applied; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
    public static bool CanApply(this TransformationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.IsEnabled && !string.IsNullOrWhiteSpace(rule.Key);
    }
}