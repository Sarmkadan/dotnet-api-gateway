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
    private int _roundRobinIndex = 0;

    public RoutingService(GatewayRouteRepository routeRepository,
        LoadBalancingStrategy loadBalancingStrategy = LoadBalancingStrategy.RoundRobin)
    {
        _routeRepository = routeRepository;
        _loadBalancingStrategy = loadBalancingStrategy;
    }

    public async Task<GatewayRoute?> FindRouteAsync(string path, string method)
    {
        var route = await _routeRepository.FindRouteByPathAsync(path, method);

        if (route is null)
            throw new RouteNotFoundException(path, method);

        return route;
    }

    public RouteTarget SelectTarget(GatewayRoute route, string? clientIp = null)
    {
        var healthyTargets = route.Targets.Where(t => t.IsHealthy).ToList();

        if (healthyTargets.Count == 0)
            throw new GatewayException(
                $"No healthy targets available for route {route.Name}",
                "NO_HEALTHY_TARGETS",
                503);

        return _loadBalancingStrategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectTargetRoundRobin(healthyTargets),
            LoadBalancingStrategy.IpHash => SelectTargetByIpHash(healthyTargets, clientIp),
            LoadBalancingStrategy.LeastConnections => SelectTargetLeastConnections(healthyTargets),
            _ => SelectTargetRoundRobin(healthyTargets)
        };
    }

    private RouteTarget SelectTargetRoundRobin(List<RouteTarget> targets)
    {
        var target = targets[_roundRobinIndex % targets.Count];
        _roundRobinIndex++;
        return target;
    }

    private RouteTarget SelectTargetByIpHash(List<RouteTarget> targets, string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
            return SelectTargetRoundRobin(targets);

        var hash = Math.Abs(clientIp.GetHashCode()) % targets.Count;
        return targets[hash];
    }

    private RouteTarget SelectTargetLeastConnections(List<RouteTarget> targets)
    {
        // Simplified: select by weight (lower weight = less loaded)
        return targets.OrderBy(t => t.Weight).First();
    }

    public string BuildForwardUrl(RouteTarget target, string requestPath)
    {
        return target.GetForwardUrl(requestPath);
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
        return await _routeRepository.AddAsync(route);
    }

    public async Task<GatewayRoute> UpdateRouteAsync(GatewayRoute route)
    {
        route.Validate();
        return await _routeRepository.UpdateAsync(route);
    }

    public async Task<bool> DeleteRouteAsync(string routeId)
    {
        return await _routeRepository.DeleteAsync(routeId);
    }
}
