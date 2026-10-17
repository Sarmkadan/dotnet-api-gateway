#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using DotNetApiGateway.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// Validates the routing section (<c>DotnetApiGateway:Routes</c>) at startup.
/// Every problem found is collected, so a single failure lists all of them.
/// </summary>
public sealed class GatewayRoutesOptionsValidator : IValidateOptions<DotnetApiGatewayOptions>
{
    /// <summary>
    /// Checks the route definitions and returns every problem found.
    /// </summary>
    /// <param name="name">The options instance name (unused; rules apply to all instances)</param>
    /// <param name="options">The gateway options to validate</param>
    /// <returns>Success when all rules pass; otherwise a failure containing one message per problem</returns>
    /// <exception cref="ArgumentNullException">Thrown if options is null</exception>
    public ValidateOptionsResult Validate(string? name, DotnetApiGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var routes = options.Routes;
        var failures = new List<string>();

        if (routes.Count == 0)
            failures.Add("DotnetApiGateway:Routes must define at least one route.");

        AddDuplicateNameFailures(routes, failures);
        AddDuplicatePathFailures(routes, failures);

        for (var i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            if (route is null)
            {
                failures.Add($"Routes[{i}] is null.");
                continue;
            }

            AddRouteFailures(i, route, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddDuplicateNameFailures(List<GatewayRoute> routes, List<string> failures)
    {
        var duplicates = routes
            .Where(r => r is not null)
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
            failures.Add($"Route name '{group.Key}' is used by {group.Count()} routes; route names must be unique.");
    }

    private static void AddDuplicatePathFailures(List<GatewayRoute> routes, List<string> failures)
    {
        // Routing ignores case and empty path segments, so '/api' and '/API/' are the same prefix
        var duplicates = routes
            .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.PathPattern))
            .GroupBy(r => NormalizePath(r.PathPattern), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            var paths = string.Join(", ", group.Select(r => $"'{r.PathPattern}'"));
            failures.Add($"Path prefixes {paths} are duplicates; each route needs a unique path prefix.");
        }
    }

    private static void AddRouteFailures(int index, GatewayRoute route, List<string> failures)
    {
        var label = string.IsNullOrWhiteSpace(route.Name)
            ? $"Routes[{index}]"
            : $"Route '{route.Name}'";

        if (string.IsNullOrWhiteSpace(route.PathPattern))
            failures.Add($"{label}: path prefix must not be empty.");
        else if (!route.PathPattern.StartsWith('/'))
            failures.Add($"{label}: path prefix '{route.PathPattern}' must start with '/'.");

        if (route.TimeoutSeconds <= 0)
            failures.Add($"{label}: TimeoutSeconds must be greater than 0 (was {route.TimeoutSeconds}).");

        var targets = route.Targets;
        if (targets.Length == 0)
            failures.Add($"{label}: must define at least one upstream in Targets.");

        for (var j = 0; j < targets.Length; j++)
        {
            var target = targets[j];
            if (target is null)
            {
                failures.Add($"{label}: upstream[{j}] is null.");
                continue;
            }

            var upstream = $"{label}: upstream[{j}] '{target.Name}'";

            if (!IsAbsoluteHttpUri(target.BaseUrl))
                failures.Add($"{upstream} BaseUrl '{target.BaseUrl}' must be an absolute http or https URI.");

            if (target.TimeoutSeconds is <= 0)
                failures.Add($"{upstream} TimeoutSeconds must be greater than 0 when set (was {target.TimeoutSeconds}).");
        }
    }

    private static bool IsAbsoluteHttpUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string NormalizePath(string path) => path.Trim().TrimEnd('/');
}
