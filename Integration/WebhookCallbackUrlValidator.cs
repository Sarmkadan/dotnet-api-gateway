#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Integration;

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotNetApiGateway.Configuration;

/// <summary>
/// Result of validating a webhook callback URL against SSRF protection rules.
/// </summary>
/// <param name="IsAllowed">Whether the callback URL is safe to register or deliver to.</param>
/// <param name="Error">A human-readable rejection reason, populated when <paramref name="IsAllowed"/> is <c>false</c>.</param>
public readonly record struct WebhookUrlValidationResult(bool IsAllowed, string? Error)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A result with <see cref="IsAllowed"/> set to <c>true</c>.</returns>
    public static WebhookUrlValidationResult Allow() => new(true, null);

    /// <summary>
    /// Creates a failed validation result with the given reason.
    /// </summary>
    /// <param name="reason">Human-readable rejection reason.</param>
    /// <returns>A result with <see cref="IsAllowed"/> set to <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="reason"/> is null or empty.</exception>
    public static WebhookUrlValidationResult Deny(string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);
        return new(false, reason);
    }
}

/// <summary>
/// Validates webhook callback URLs to prevent server-side request forgery (SSRF).
/// Requires HTTPS by default, resolves DNS to reject requests aimed at private,
/// loopback, link-local, or otherwise disallowed IP ranges, and re-validates the
/// resolved address immediately before delivery to defeat DNS rebinding attacks.
/// </summary>
public sealed class WebhookCallbackUrlValidator
{
    private static readonly string[] BuiltInDeniedCidrs =
    [
        "0.0.0.0/8",        // "this" network
        "10.0.0.0/8",       // RFC1918 private
        "100.64.0.0/10",    // carrier-grade NAT
        "127.0.0.0/8",      // loopback
        "169.254.0.0/16",   // link-local (cloud metadata services live here)
        "172.16.0.0/12",    // RFC1918 private
        "192.0.0.0/24",     // IETF protocol assignments
        "192.0.2.0/24",     // documentation (TEST-NET-1)
        "192.168.0.0/16",   // RFC1918 private
        "198.18.0.0/15",    // benchmarking
        "198.51.100.0/24",  // documentation (TEST-NET-2)
        "203.0.113.0/24",   // documentation (TEST-NET-3)
        "224.0.0.0/4",      // multicast
        "240.0.0.0/4",      // reserved
        "::1/128",          // loopback
        "::/128",           // unspecified
        "64:ff9b::/96",     // NAT64 well-known prefix (can carry mapped IPv4 targets)
        "fc00::/7",         // unique local
        "fe80::/10",        // link-local
        "ff00::/8",         // multicast
    ];

    private readonly WebhookSecurityOptions _options;
    private readonly List<IPNetwork> _denyNetworks;
    private readonly List<IPNetwork> _allowNetworks;
    private readonly ILogger<WebhookCallbackUrlValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookCallbackUrlValidator"/> class.
    /// </summary>
    /// <param name="options">The configured webhook security options.</param>
    /// <param name="logger">Logger used to record rejected callback URLs.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="logger"/> is null.</exception>
    public WebhookCallbackUrlValidator(IOptions<WebhookSecurityOptions> options, ILogger<WebhookCallbackUrlValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _denyNetworks = ParseCidrs(BuiltInDeniedCidrs).Concat(ParseCidrs(_options.DenyCidrs)).ToList();
        _allowNetworks = ParseCidrs(_options.AllowCidrs).ToList();
    }

    /// <summary>
    /// Validates that a callback URL uses an allowed scheme and does not resolve to a
    /// disallowed (private/loopback/link-local/denied) IP address. Intended to be run both
    /// at registration time and again immediately before each delivery attempt, since DNS
    /// records can change between the two (DNS rebinding).
    /// </summary>
    /// <param name="callbackUrl">The candidate webhook callback URL.</param>
    /// <param name="cancellationToken">Token used to cancel the DNS lookup.</param>
    /// <returns>A <see cref="WebhookUrlValidationResult"/> describing whether the URL is safe to use.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="callbackUrl"/> is null or empty.</exception>
    public async Task<WebhookUrlValidationResult> ValidateAsync(string callbackUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(callbackUrl);

        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var uri))
            return WebhookUrlValidationResult.Deny("Callback URL is not a valid absolute URI.");

        var allowedScheme = _options.AllowHttp
            ? uri.Scheme is "https" or "http"
            : uri.Scheme is "https";

        if (!allowedScheme)
        {
            return WebhookUrlValidationResult.Deny(_options.AllowHttp
                ? "Callback URL must use the http or https scheme."
                : "Callback URL must use the https scheme.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
                ? [IPAddress.Parse(uri.DnsSafeHost)]
                : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "DNS resolution failed for webhook callback host {Host}", uri.DnsSafeHost);
            return WebhookUrlValidationResult.Deny($"Could not resolve callback host '{uri.DnsSafeHost}'.");
        }

        if (addresses.Length == 0)
            return WebhookUrlValidationResult.Deny($"Could not resolve callback host '{uri.DnsSafeHost}'.");

        foreach (var address in addresses)
        {
            if (IsBlockedAddress(address, out var reason))
            {
                _logger.LogWarning(
                    "Rejected webhook callback URL {CallbackUrl}: host {Host} resolved to blocked address {Address} ({Reason})",
                    callbackUrl, uri.DnsSafeHost, address, reason);
                return WebhookUrlValidationResult.Deny($"Callback host resolves to a disallowed address ({reason}).");
            }
        }

        return WebhookUrlValidationResult.Allow();
    }

    /// <summary>
    /// Determines whether the given address falls inside a denied range and is not
    /// covered by an explicit allow-list override.
    /// </summary>
    /// <param name="address">The resolved IP address to check.</param>
    /// <param name="reason">Describes why the address was blocked, when this method returns <c>true</c>.</param>
    /// <returns><c>true</c> if the address must be rejected; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null.</exception>
    private bool IsBlockedAddress(IPAddress address, out string reason)
    {
        ArgumentNullException.ThrowIfNull(address);

        var normalized = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        foreach (var network in _denyNetworks)
        {
            if (network.Contains(normalized))
            {
                reason = $"matches denied range {network}";
                return true;
            }
        }

        if (_allowNetworks.Any(n => n.Contains(normalized)))
        {
            reason = string.Empty;
            return false;
        }

        if (IPAddress.IsLoopback(normalized) || normalized.IsIPv6LinkLocal || normalized.IsIPv6SiteLocal || normalized.IsIPv6Multicast)
        {
            reason = "loopback/link-local/multicast address";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses a collection of CIDR strings into <see cref="IPNetwork"/> instances, skipping
    /// (and logging) any entries that fail to parse instead of throwing.
    /// </summary>
    /// <param name="cidrs">The CIDR notation strings to parse.</param>
    /// <returns>The successfully parsed networks.</returns>
    private IEnumerable<IPNetwork> ParseCidrs(IEnumerable<string> cidrs)
    {
        foreach (var cidr in cidrs)
        {
            if (IPNetwork.TryParse(cidr, out var network))
            {
                yield return network;
            }
            else
            {
                _logger.LogWarning("Ignoring invalid webhook security CIDR entry: {Cidr}", cidr);
            }
        }
    }
}
