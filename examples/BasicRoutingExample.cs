// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: Basic Routing Configuration
/// Demonstrates how to configure simple request routing to backend services.
/// </summary>
public class BasicRoutingExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - Basic Routing Example ===\n");

        // Create configuration
        var routes = new List<GatewayRoute>
        {
            new()
            {
                Name = "user-service",
                Pattern = "^/api/users(/.*)?$",
                Method = "ANY",
                Description = "Route for user service API",
                Targets = new List<RouteTarget>
                {
                    new()
                    {
                        Url = "https://jsonplaceholder.typicode.com/users",
                        Weight = 100,
                        HealthCheckUrl = "https://jsonplaceholder.typicode.com"
                    }
                }
            },
            new()
            {
                Name = "post-service",
                Pattern = "^/api/posts(/.*)?$",
                Method = "GET",
                Description = "Route for posts API (GET only)",
                Targets = new List<RouteTarget>
                {
                    new()
                    {
                        Url = "https://jsonplaceholder.typicode.com/posts",
                        Weight = 100
                    }
                }
            },
            new()
            {
                Name = "comment-service",
                Pattern = "^/api/comments(/.*)?$",
                Method = "ANY",
                Description = "Route for comments API",
                Targets = new List<RouteTarget>
                {
                    new()
                    {
                        Url = "https://jsonplaceholder.typicode.com/comments",
                        Weight = 100
                    }
                }
            }
        };

        // Display routes
        Console.WriteLine("Configured Routes:");
        Console.WriteLine(new string('-', 60));

        foreach (var route in routes)
        {
            Console.WriteLine($"Route: {route.Name}");
            Console.WriteLine($"  Pattern: {route.Pattern}");
            Console.WriteLine($"  Methods: {route.Method}");
            Console.WriteLine($"  Description: {route.Description}");
            Console.WriteLine($"  Targets:");

            foreach (var target in route.Targets)
            {
                Console.WriteLine($"    - URL: {target.Url}");
                Console.WriteLine($"      Weight: {target.Weight}");
            }
            Console.WriteLine();
        }

        // Simulate route matching
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Route Matching Examples:");
        Console.WriteLine(new string('-', 60));

        var testPaths = new[]
        {
            "/api/users",
            "/api/users/1",
            "/api/users/123/profile",
            "/api/posts",
            "/api/posts/1/comments",
            "/api/comments",
            "/api/unknown"
        };

        foreach (var path in testPaths)
        {
            var matchedRoute = routes.FirstOrDefault(r =>
                System.Text.RegularExpressions.Regex.IsMatch(path, r.Pattern) &&
                (r.Method == "ANY" || r.Method == "GET"));

            Console.WriteLine($"Request: GET {path}");
            if (matchedRoute != null)
            {
                Console.WriteLine($"  ✓ Matched route: {matchedRoute.Name}");
                Console.WriteLine($"  ✓ Target URL: {matchedRoute.Targets[0].Url}");
                Console.WriteLine($"  ✓ Full URL would be: {matchedRoute.Targets[0].Url}{path.Substring(matchedRoute.Name.Length)}");
            }
            else
            {
                Console.WriteLine($"  ✗ No matching route (404)");
            }
            Console.WriteLine();
        }

        // Demonstrate load balancing with multiple targets
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Load Balancing Example (Round Robin):");
        Console.WriteLine(new string('-', 60));

        var loadBalancedRoute = new GatewayRoute
        {
            Name = "api-cluster",
            Pattern = "^/api/cluster(/.*)?$",
            Method = "ANY",
            Targets = new List<RouteTarget>
            {
                new() { Url = "http://api-1:3000", Weight = 50 },
                new() { Url = "http://api-2:3000", Weight = 30 },
                new() { Url = "http://api-3:3000", Weight = 20 }
            }
        };

        Console.WriteLine($"Route: {loadBalancedRoute.Name}");
        Console.WriteLine("Targets with weights:");
        foreach (var target in loadBalancedRoute.Targets)
        {
            Console.WriteLine($"  - {target.Url} (weight: {target.Weight}%)");
        }

        Console.WriteLine("\nSimulating 10 requests distribution:");
        var requestDistribution = SimulateLoadBalancing(loadBalancedRoute.Targets, 10);
        foreach (var (url, count) in requestDistribution)
        {
            var percentage = (count * 100) / 10;
            Console.WriteLine($"  {url}: {count} requests ({percentage}%)");
        }

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private static Dictionary<string, int> SimulateLoadBalancing(
        List<RouteTarget> targets, int requestCount)
    {
        // Simple weighted round-robin simulation
        var distribution = targets.ToDictionary(t => t.Url, t => 0);
        var totalWeight = targets.Sum(t => t.Weight);

        for (int i = 0; i < requestCount; i++)
        {
            var randomValue = (i * 100) % totalWeight;
            var accumulated = 0;

            foreach (var target in targets)
            {
                accumulated += target.Weight;
                if (randomValue < accumulated)
                {
                    distribution[target.Url]++;
                    break;
                }
            }
        }

        return distribution;
    }
}

/// <summary>
/// To run this example:
///
/// 1. Ensure the gateway is running on http://localhost:5000
/// 2. Test the routes:
///    curl http://localhost:5000/api/users
///    curl http://localhost:5000/api/posts/1
///    curl http://localhost:5000/api/comments
///
/// Note: This example uses JSONPlaceholder public API for testing.
/// </summary>
