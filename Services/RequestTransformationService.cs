#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

using DotNetApiGateway.Models;

/// <summary>
/// Applies <see cref="TransformationRule"/> collections to outgoing HTTP requests
/// and incoming HTTP responses according to the phase each rule targets.
/// </summary>
public sealed class RequestTransformationService
{
    private readonly ILogger<RequestTransformationService> _logger;

    public RequestTransformationService(ILogger<RequestTransformationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies all request-phase transformation rules to the given <see cref="HttpRequestMessage"/>.
    /// The original message is mutated and returned; its URI may be replaced if path or
    /// query-parameter rules change the URL.
    /// </summary>
    /// <param name="request">The outbound request to transform.</param>
    /// <param name="rules">All rules defined on the route (response-phase rules are skipped).</param>
    /// <returns>The transformed request message.</returns>
    public HttpRequestMessage ApplyRequestRules(HttpRequestMessage request, IEnumerable<TransformationRule> rules)
    {
        var orderedRules = rules
            .Where(r => r.IsEnabled && r.Phase == TransformationPhase.Request)
            .OrderBy(r => r.Order);

        foreach (var rule in orderedRules)
        {
            try
            {
                ApplyRuleToRequest(request, rule);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Transformation rule {RuleId} ({Operation}) failed on request", rule.Id, rule.Operation);
            }
        }

        return request;
    }

    /// <summary>
    /// Applies all response-phase transformation rules to the given <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="response">The upstream response to transform.</param>
    /// <param name="rules">All rules defined on the route (request-phase rules are skipped).</param>
    /// <returns>The transformed response message.</returns>
    public HttpResponseMessage ApplyResponseRules(HttpResponseMessage response, IEnumerable<TransformationRule> rules)
    {
        var orderedRules = rules
            .Where(r => r.IsEnabled && r.Phase == TransformationPhase.Response)
            .OrderBy(r => r.Order);

        foreach (var rule in orderedRules)
        {
            try
            {
                ApplyRuleToResponse(response, rule);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Transformation rule {RuleId} ({Operation}) failed on response", rule.Id, rule.Operation);
            }
        }

        return response;
    }

    // -------------------------------------------------------------------------
    // Private helpers — request
    // -------------------------------------------------------------------------

    private void ApplyRuleToRequest(HttpRequestMessage request, TransformationRule rule)
    {
        switch (rule.Operation)
        {
            case TransformationOperation.AddHeader:
                if (!request.Headers.Contains(rule.Key))
                    request.Headers.TryAddWithoutValidation(rule.Key, rule.Value);
                break;

            case TransformationOperation.SetHeader:
                request.Headers.Remove(rule.Key);
                request.Headers.TryAddWithoutValidation(rule.Key, rule.Value);
                break;

            case TransformationOperation.RemoveHeader:
                request.Headers.Remove(rule.Key);
                break;

            case TransformationOperation.AddQueryParam:
                request.RequestUri = AddQueryParam(request.RequestUri!, rule.Key, rule.Value!, replace: false);
                break;

            case TransformationOperation.SetQueryParam:
                request.RequestUri = AddQueryParam(request.RequestUri!, rule.Key, rule.Value!, replace: true);
                break;

            case TransformationOperation.RemoveQueryParam:
                request.RequestUri = RemoveQueryParam(request.RequestUri!, rule.Key);
                break;

            case TransformationOperation.RewritePathPrefix:
                request.RequestUri = RewritePrefix(request.RequestUri!, rule.Key, rule.Value!);
                break;

            default:
                _logger.LogWarning("Unsupported request transformation operation: {Op}", rule.Operation);
                break;
        }

        _logger.LogDebug("Applied request rule {RuleId}: {Op} on key '{Key}'", rule.Id, rule.Operation, rule.Key);
    }

    private void ApplyRuleToResponse(HttpResponseMessage response, TransformationRule rule)
    {
        switch (rule.Operation)
        {
            case TransformationOperation.AddHeader:
                if (!response.Headers.Contains(rule.Key))
                    response.Headers.TryAddWithoutValidation(rule.Key, rule.Value);
                break;

            case TransformationOperation.SetHeader:
                response.Headers.Remove(rule.Key);
                response.Headers.TryAddWithoutValidation(rule.Key, rule.Value);
                break;

            case TransformationOperation.RemoveHeader:
                response.Headers.Remove(rule.Key);
                break;

            default:
                _logger.LogWarning("Operation {Op} is not supported in the response phase; rule {RuleId} skipped",
                    rule.Operation, rule.Id);
                break;
        }

        _logger.LogDebug("Applied response rule {RuleId}: {Op} on key '{Key}'", rule.Id, rule.Operation, rule.Key);
    }

    // -------------------------------------------------------------------------
    // URL mutation helpers
    // -------------------------------------------------------------------------

    private static Uri AddQueryParam(Uri uri, string key, string value, bool replace)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (replace || query[key] == null)
            query[key] = value;

        var builder = new UriBuilder(uri) { Query = query.ToString() };
        return builder.Uri;
    }

    private static Uri RemoveQueryParam(Uri uri, string key)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query.Remove(key);
        var builder = new UriBuilder(uri) { Query = query.ToString() };
        return builder.Uri;
    }

    private static Uri RewritePrefix(Uri uri, string oldPrefix, string newPrefix)
    {
        var path = uri.AbsolutePath;
        if (!path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
            return uri;

        var newPath = newPrefix + path[oldPrefix.Length..];
        var builder = new UriBuilder(uri) { Path = newPath };
        return builder.Uri;
    }
}
