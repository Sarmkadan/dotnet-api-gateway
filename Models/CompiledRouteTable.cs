#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;

namespace DotNetApiGateway.Models;

/// <summary>
/// Immutable compiled route table for fast route matching.
/// Routes are pre-compiled into a dictionary for O(1) lookup by path pattern.
/// </summary>
public sealed class CompiledRouteTable : IEquatable<CompiledRouteTable>
{
    private readonly ConcurrentDictionary<string, GatewayRoute> _routesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GatewayRoute> _routesById = new(StringComparer.Ordinal);
    private readonly int _version;

    /// <summary>
    /// Initializes a new empty compiled route table.
    /// </summary>
    public CompiledRouteTable(int version = 0)
    {
        _version = version;
    }

    /// <summary>
    /// Initializes a new compiled route table from the specified routes.
    /// </summary>
    /// <param name="routes">The routes to compile.</param>
    /// <param name="version">The version number of this compilation.</param>
    public CompiledRouteTable(IEnumerable<GatewayRoute> routes, int version = 0)
    {
        _version = version;
        CompileRoutes(routes);
    }

    /// <summary>
    /// Gets the version number of this compiled route table.
    /// </summary>
    public int Version => _version;

    /// <summary>
    /// Gets the number of routes in this table.
    /// </summary>
    public int Count => _routesByPath.Count;

    /// <summary>
    /// Finds a route by path and HTTP method.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The matching gateway route, or null if not found.</returns>
    public GatewayRoute? FindRoute(string path, string method)
    {
        // Try exact path match first
        if (_routesByPath.TryGetValue(path, out var exactRoute))
        {
            if (exactRoute.IsActive && exactRoute.SupportsMethod(method))
            {
                return exactRoute;
            }
        }

        // Try to find a route with wildcard pattern matching
        foreach (var routeEntry in _routesByPath)
        {
            var route = routeEntry.Value;
            if (route.IsActive && route.MatchesPath(path) && route.SupportsMethod(method))
            {
                return route;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a route by its ID.
    /// </summary>
    /// <param name="routeId">The route ID.</param>
    /// <returns>The route, or null if not found.</returns>
    public GatewayRoute? GetRouteById(string routeId)
    {
        _routesById.TryGetValue(routeId, out var route);
        return route;
    }

    /// <summary>
    /// Gets all active routes.
    /// </summary>
    /// <returns>An enumerable of active routes.</returns>
    public IEnumerable<GatewayRoute> GetAllActiveRoutes()
    {
        return _routesByPath.Values.Where(r => r.IsActive);
    }

    /// <summary>
    /// Compiles the specified routes into the route table.
    /// </summary>
    /// <param name="routes">The routes to compile.</param>
    private void CompileRoutes(IEnumerable<GatewayRoute> routes)
    {
        foreach (var route in routes)
        {
            if (route.IsActive)
            {
                _routesByPath[route.PathPattern] = route;
                _routesById[route.Id] = route;
            }
        }
    }

    /// <summary>
    /// Creates a new compiled route table with updated routes.
    /// </summary>
    /// <param name="newRoutes">The new routes to compile.</param>
    /// <param name="newVersion">The new version number.</param>
    /// <returns>A new compiled route table instance.</returns>
    public CompiledRouteTable WithUpdatedRoutes(IEnumerable<GatewayRoute> newRoutes, int newVersion)
    {
        var newTable = new CompiledRouteTable(newVersion);
        newTable.CompileRoutes(newRoutes);
        return newTable;
    }

    /// <summary>
    /// Determines whether this instance equals another compiled route table.
    /// </summary>
    /// <param name="other">The other instance.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public bool Equals(CompiledRouteTable? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_version != other._version) return false;
        if (_routesByPath.Count != other._routesByPath.Count) return false;

        // Compare route IDs since routes are immutable
        var thisIds = _routesById.Keys.OrderBy(id => id).ToList();
        var otherIds = other._routesById.Keys.OrderBy(id => id).ToList();

        return thisIds.SequenceEqual(otherIds);
    }

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as CompiledRouteTable);

    /// <summary>
    /// Gets the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = _version.GetHashCode();
            foreach (var routeId in _routesById.Keys.OrderBy(id => id))
            {
                hash = (hash * 397) ^ routeId.GetHashCode();
            }
            return hash;
        }
    }
}