#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for routing requests to appropriate backend targets
/// </summary>
public sealed class RoutingService
{
    private readonly GatewayRouteRepository _routeRepository;
    private readonly LoadBalancingStrategy _loadBalancingStrategy;
    private readonly ILogger<RoutingService> _logger;
    private int _roundRobinIndex = -1; // first Interlocked.Increment yields 0

    public RoutingService(GatewayRouteRepository routeRepository,
        LoadBalancingStrategy loadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
        ILogger<RoutingService>? logger = null)
    {
        _routeRepository = routeRepository;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RoutingService>.Instance;
        _loadBalancingStrategy = loadBalancingStrategy;
    }

    /// <summary>
    /// Finds a route matching the specified path and HTTP method.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The matching gateway route; never null.</returns>
    /// <exception cref="RouteNotFoundException">Thrown if no route is found.</exception>
    public async Task<GatewayRoute?> FindRouteAsync(string path, string method)
    {
        var route = await _routeRepository.FindRouteByPathAsync(path, method);

        if (route is null)
        {
            _logger.LogWarning("Route not found for {Method} {Path}", method, path);
            throw new RouteNotFoundException(path, method);
        }

        _logger.LogDebug("Route found: {RouteId} for {Method} {Path}", route.Id, method, path);
        return route;
    }

    /// <summary>
    /// Selects a target for the given route based on the configured load balancing strategy.
    /// </summary>
    /// <param name="route">The gateway route.</param>
    /// <param name="clientIp">The client IP address, used for IP hash strategy.</param>
    /// <returns>The selected route target.</returns>
    /// <exception cref="GatewayException">Thrown if no healthy targets are available.</exception>
    public RouteTarget SelectTarget(GatewayRoute route, string? clientIp = null)
    {
        var healthyTargets = route.Targets.Where(t => t.IsHealthy).ToList();

        if (healthyTargets.Count == 0)
        {
            _logger.LogError("No healthy targets available for route {RouteName} ({RouteId})", route.Name, route.Id);
            throw new GatewayException(
                $"No healthy targets available for route {route.Name}",
                "NO_HEALTHY_TARGETS",
                503);
        }

        var target = _loadBalancingStrategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectTargetRoundRobin(healthyTargets),
            LoadBalancingStrategy.IpHash => SelectTargetByIpHash(healthyTargets, clientIp),
            LoadBalancingStrategy.LeastConnections => SelectTargetLeastConnections(healthyTargets),
            _ => SelectTargetRoundRobin(healthyTargets)
        };

        _logger.LogDebug("Selected target {TargetUrl} for route {RouteId} using {Strategy}", target.BaseUrl, route.Id, _loadBalancingStrategy);
        return target;
    }

    /// <summary>
    /// Builds the URL to forward the request to the target.
    /// </summary>
    /// <param name="target">The target to forward to.</param>
    /// <param name="requestPath">The original request path.</param>
    /// <returns>The full URL for the backend target.</returns>
    public string BuildForwardUrl(RouteTarget target, string requestPath)
    {
        return target.GetForwardUrl(requestPath);
    }

    private RouteTarget SelectTargetRoundRobin(List<RouteTarget> targets)
    {
        // Interlocked keeps concurrent callers from losing increments; the unsigned
        // modulo keeps the index valid after the counter wraps past int.MaxValue.
        var ticket = unchecked((uint)Interlocked.Increment(ref _roundRobinIndex));
        return targets[(int)(ticket % (uint)targets.Count)];
    }

    private RouteTarget SelectTargetByIpHash(List<RouteTarget> targets, string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
            return SelectTargetRoundRobin(targets);

        // string.GetHashCode is randomized per process, which would break client
        // affinity across restarts; FNV-1a gives a stable, deterministic hash.
        var hash = ComputeStableHash(clientIp);
        return targets[(int)(hash % (uint)targets.Count)];
    }

    private static uint ComputeStableHash(string value)
    {
        const uint fnvOffsetBasis = 2166136261;
        const uint fnvPrime = 16777619;

        var hash = fnvOffsetBasis;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= fnvPrime;
        }

        return hash;
    }

    private static RouteTarget SelectTargetLeastConnections(List<RouteTarget> targets)
    {
        // Connection counts are not tracked per target, so approximate least-loaded
        // by weight (lower weight = less loaded).
        return targets.OrderBy(t => t.Weight).First();
    }

    public Dictionary<string, string> ApplyHeaderTransforms(RouteTarget target, Dictionary<string, string> originalHeaders)
    {
        var headers = new Dictionary<string, string>(originalHeaders);

        foreach (var transform in target.TransformHeaders)
        {
            headers[transform.Key] = transform.Value;
        }

        return headers;
    }

    public async Task<IEnumerable<GatewayRoute>> GetAllActiveRoutesAsync()
    {
        return await _routeRepository.GetActiveRoutesAsync();
    }

    public async Task<GatewayRoute> CreateRouteAsync(GatewayRoute route)
    {
        route.Validate();
        var created = await _routeRepository.AddAsync(route);
        _logger.LogInformation("Route created: {RouteId} ({RouteName})", created.Id, created.Name);
        return created;
    }

    public async Task<GatewayRoute> UpdateRouteAsync(GatewayRoute route)
    {
        route.Validate();
        var updated = await _routeRepository.UpdateAsync(route);
        _logger.LogInformation("Route updated: {RouteId} ({RouteName})", updated.Id, updated.Name);
        return updated;
    }

    public async Task<bool> DeleteRouteAsync(string routeId)
    {
        var deleted = await _routeRepository.DeleteAsync(routeId);
        if (deleted)
        {
            _logger.LogInformation("Route deleted: {RouteId}", routeId);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent route: {RouteId}", routeId);
        }
        return deleted;
    }
}
