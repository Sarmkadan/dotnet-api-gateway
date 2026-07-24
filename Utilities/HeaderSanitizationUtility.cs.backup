#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Header sanitization utilities for HTTP request/response forwarding.
// Ensures proper header hygiene by removing hop-by-hop headers, preventing
// sensitive header leakage, and correctly setting forwarding headers.
// =====================================================================

namespace DotNetApiGateway.Utilities;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Provides utilities for sanitizing HTTP headers during request/response forwarding.
/// Removes hop-by-hop headers, prevents sensitive header leakage, and sets
/// standard forwarding headers (X-Forwarded-*, Forwarded).
/// </summary>
public static class HeaderSanitizationUtility
{
    // Hop-by-hop headers that should NOT be forwarded to backend services
    // These headers are defined in RFC 7230 Section 6.1 and RFC 9112 Section 7.6
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authorization",
        "Proxy-Authenticate",
        "Proxy-Connection",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "TE"
    };

    // Headers that should NOT be forwarded to backend services as they contain
    // sensitive authentication/authorization information
    private static readonly HashSet<string> SensitiveAuthHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie"
    };

    // Headers that should be removed from incoming requests before forwarding
    // These are gateway-specific headers that shouldn't be passed to backends
    private static readonly HashSet<string> GatewayInternalHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Forwarded-Host",
        "Forwarded",
        "Via"
    };

    /// <summary>
    /// Sanitizes HTTP headers for forwarding to backend services.
    /// Removes hop-by-hop headers, sensitive auth headers, and gateway-internal headers.
    /// </summary>
    /// <param name="headers">The headers to sanitize.</param>
    /// <param name="clientIp">The client IP address for X-Forwarded-For construction.</param>
    /// <param name="requestScheme">The request scheme (http/https) for X-Forwarded-Proto.</param>
    /// <param name="outgoingRequest">The outgoing HttpRequestMessage being constructed.</param>
    /// <param name="removeHostHeader">Whether to remove the Host header (default: true).</param>
    public static void SanitizeForForwarding(
        IHeaderDictionary headers,
        string clientIp,
        string requestScheme,
        HttpRequestMessage outgoingRequest,
        bool removeHostHeader = true)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(outgoingRequest);

        // Remove hop-by-hop headers that should not be forwarded
        RemoveHopByHopHeaders(headers, outgoingRequest);

        // Remove sensitive authentication headers that should not reach backend
        RemoveSensitiveAuthHeaders(headers, outgoingRequest);

        // Remove gateway-internal headers that shouldn't be passed to backends
        RemoveGatewayInternalHeaders(headers, outgoingRequest);

        // Set standard forwarding headers
        SetForwardingHeaders(headers, clientIp, requestScheme, outgoingRequest, removeHostHeader);
    }

    /// <summary>
    /// Removes hop-by-hop headers from the outgoing request.
    /// </summary>
    private static void RemoveHopByHopHeaders(IHeaderDictionary incomingHeaders, HttpRequestMessage outgoingRequest)
    {
        foreach (var header in incomingHeaders)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                // Remove from incoming headers (if present)
                incomingHeaders.Remove(header.Key);

                // Skip adding to outgoing request
                continue;
            }

            // Try to add to outgoing request if not already present
            // Only add headers that are valid for HttpRequestMessage.Headers
            // Skip content headers like Content-Type, Content-Length, etc.
            try
            {
                // Skip content headers that cannot be added to HttpRequestMessage.Headers
                // Content-Type, Content-Length, etc. should be handled via HttpRequestMessage.Content
                if (!header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    if (!outgoingRequest.Headers.Contains(header.Key))
                    {
                        outgoingRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }
            catch
            {
                // Some headers cannot be added to HttpRequestMessage (content headers, etc.)
                // Silently skip them
            }
        }
    }

    /// <summary>
    /// Removes sensitive authentication headers that should not reach backend services.
    /// </summary>
    private static void RemoveSensitiveAuthHeaders(IHeaderDictionary incomingHeaders, HttpRequestMessage outgoingRequest)
    {
        foreach (var headerName in SensitiveAuthHeaders)
        {
            if (incomingHeaders.TryGetValue(headerName, out var values))
            {
                // Remove from incoming headers
                incomingHeaders.Remove(headerName);

                // Don't add Authorization/Cookie headers to outgoing request
                // These should only be used by the gateway itself
            }
        }
    }

    /// <summary>
    /// Removes gateway-internal forwarding headers that shouldn't be passed to backends.
    /// These will be regenerated by the gateway.
    /// </summary>
    private static void RemoveGatewayInternalHeaders(IHeaderDictionary incomingHeaders, HttpRequestMessage outgoingRequest)
    {
        foreach (var headerName in GatewayInternalHeaders)
        {
            if (incomingHeaders.TryGetValue(headerName, out _))
            {
                // Remove from incoming headers
                incomingHeaders.Remove(headerName);
            }
        }
    }

    /// <summary>
    /// Sets standard forwarding headers (X-Forwarded-For, X-Forwarded-Proto, Forwarded, Host).
    /// </summary>
    private static void SetForwardingHeaders(
        IHeaderDictionary incomingHeaders,
        string clientIp,
        string requestScheme,
        HttpRequestMessage outgoingRequest,
        bool removeHostHeader)
    {
        // Build X-Forwarded-For: append client IP to existing chain (if any)
        // This prevents malicious spoofing by trusting the gateway's position in the chain
        var xForwardedForValues = new List<string>();

        // Check if X-Forwarded-For already exists in incoming headers (from previous proxy)
        if (incomingHeaders.TryGetValue("X-Forwarded-For", out var existingXff))
        {
            var existingValues = existingXff.ToString().Split(',')
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            // Only trust the immediate previous hop (last IP in the chain)
            // This prevents spoofing attacks where clients try to fake their IP
            if (existingValues.Count > 0)
            {
                // The last IP in the chain is the most recent proxy
                // We'll append our client IP after it
                xForwardedForValues.AddRange(existingValues);
            }
        }

        // Append the actual client IP (from connection or trusted proxy)
        xForwardedForValues.Add(clientIp);

        // Set X-Forwarded-For header on outgoing request
        outgoingRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", string.Join(", ", xForwardedForValues));

        // Set X-Forwarded-Proto header based on the request scheme
        outgoingRequest.Headers.TryAddWithoutValidation("X-Forwarded-Proto", requestScheme);

        // Set Forwarded header (RFC 7239) with proper formatting
        // Forwarded: for=<client-ip>;proto=<scheme>;host=<host>
        var forwardedParts = new List<string>();
        forwardedParts.Add($"for={clientIp}");
        forwardedParts.Add($"proto={requestScheme}");

        // Include host if available
        if (incomingHeaders.TryGetValue("Host", out var hostValue) && !string.IsNullOrWhiteSpace(hostValue))
        {
            forwardedParts.Add($"host={hostValue}");
        }

        outgoingRequest.Headers.TryAddWithoutValidation("Forwarded", string.Join("; ", forwardedParts));

        // Handle Host header
        if (removeHostHeader && incomingHeaders.TryGetValue("Host", out var host))
        {
            // Remove Host header from incoming (HttpClient manages this separately)
            incomingHeaders.Remove("Host");

            // Set Host header on outgoing request if needed
            // HttpClient will set this automatically based on BaseAddress, but we can override
            outgoingRequest.Headers.Host = host.ToString();
        }
    }

    /// <summary>
    /// Sanitizes response headers before forwarding to client.
    /// Removes hop-by-hop headers that shouldn't be passed to clients.
    /// </summary>
    /// <param name="responseHeaders">The response headers to sanitize.</param>
    public static void SanitizeResponseHeaders(IHeaderDictionary responseHeaders)
    {
        ArgumentNullException.ThrowIfNull(responseHeaders);

        // Remove hop-by-hop headers from response
        foreach (var headerName in HopByHopHeaders)
        {
            responseHeaders.Remove(headerName);
        }

        // Remove sensitive headers from response
        foreach (var headerName in SensitiveAuthHeaders)
        {
            responseHeaders.Remove(headerName);
        }
    }

    /// <summary>
    /// Gets the list of hop-by-hop header names.
    /// </summary>
    public static IReadOnlyCollection<string> GetHopByHopHeaders() => HopByHopHeaders.ToList();

    /// <summary>
    /// Gets the list of sensitive authentication header names.
    /// </summary>
    public static IReadOnlyCollection<string> GetSensitiveAuthHeaders() => SensitiveAuthHeaders.ToList();

    /// <summary>
    /// Gets the list of gateway-internal header names.
    /// </summary>
    public static IReadOnlyCollection<string> GetGatewayInternalHeaders() => GatewayInternalHeaders.ToList();
}
