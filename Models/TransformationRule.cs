#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Models;

/// <summary>
/// The phase of the request pipeline in which a transformation rule is applied.
/// </summary>
public enum TransformationPhase
{
    /// <summary>Applied to the outgoing request before it reaches the backend.</summary>
    Request,

    /// <summary>Applied to the upstream response before it reaches the client.</summary>
    Response
}

/// <summary>
/// The type of transformation operation to perform.
/// </summary>
public enum TransformationOperation
{
    /// <summary>Add a header if it does not already exist.</summary>
    AddHeader,

    /// <summary>Set a header, replacing any existing value.</summary>
    SetHeader,

    /// <summary>Remove a header from the message.</summary>
    RemoveHeader,

    /// <summary>Only allow specific headers to be forwarded upstream; removes all others.</summary>
    AllowlistHeaders,

    /// <summary>Add a query parameter if it does not already exist.</summary>
    AddQueryParam,

    /// <summary>Set a query parameter, replacing any existing value.</summary>
    SetQueryParam,

    /// <summary>Remove a query parameter from the URL.</summary>
    RemoveQueryParam,

    /// <summary>Replace the matched prefix in the request path.</summary>
    RewritePathPrefix
}

/// <summary>
/// A single transformation rule applied to a proxied request or response.
/// </summary>
public sealed class TransformationRule
{
    /// <summary>Unique identifier for the rule.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Human-readable description of what this rule does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether this rule is evaluated during the request or response phase.</summary>
    public TransformationPhase Phase { get; set; } = TransformationPhase.Request;

    /// <summary>The transformation to apply.</summary>
    public TransformationOperation Operation { get; set; }

    /// <summary>
    /// The header name, query parameter name, or path prefix to operate on,
    /// depending on <see cref="Operation"/>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value to set or add. For <see cref="TransformationOperation.RewritePathPrefix"/>
    /// this is the replacement prefix. Not required for Remove operations.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>Evaluation order — lower numbers run first.</summary>
    public int Order { get; set; } = 0;

    /// <summary>When false the rule is skipped without error.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Validates that the rule is well-formed.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new ArgumentException($"TransformationRule '{Id}': Key cannot be empty.");

        var requiresValue = Operation is
            TransformationOperation.AddHeader or
            TransformationOperation.SetHeader or
            TransformationOperation.AddQueryParam or
            TransformationOperation.SetQueryParam or
            TransformationOperation.RewritePathPrefix;

        if (requiresValue && string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException($"TransformationRule '{Id}': Value is required for operation {Operation}.");
    }
}
