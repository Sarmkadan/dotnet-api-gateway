#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using System.Collections.Generic;

/// <summary>
/// Configuration settings that control which webhook callback URLs the gateway
/// is allowed to register and deliver events to. Used to defend against
/// server-side request forgery (SSRF) via attacker-controlled callback URLs.
/// </summary>
public sealed class WebhookSecurityOptions
{
    /// <summary>
    /// The configuration section name this options type binds to.
    /// </summary>
    public const string SectionName = "WebhookSecurity";

    /// <summary>
    /// When <c>true</c>, callback URLs using the plain "http" scheme are accepted.
    /// Defaults to <c>false</c> so only "https" callback URLs are allowed.
    /// </summary>
    public bool AllowHttp { get; set; }

    /// <summary>
    /// CIDR ranges that are always rejected, in addition to the built-in
    /// private, loopback, link-local, and multicast ranges. Entries here take
    /// precedence over <see cref="AllowCidrs"/>.
    /// </summary>
    public List<string> DenyCidrs { get; set; } = new();

    /// <summary>
    /// CIDR ranges that are explicitly permitted even though they would
    /// otherwise be blocked by the built-in private/loopback/link-local checks.
    /// Ignored for any address that also matches <see cref="DenyCidrs"/>.
    /// </summary>
    public List<string> AllowCidrs { get; set; } = new();
}
