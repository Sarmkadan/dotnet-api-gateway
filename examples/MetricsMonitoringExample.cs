#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: Metrics and Monitoring
/// Demonstrates how to collect, analyze, and act on gateway metrics.
/// </summary>
public sealed class MetricsMonitoringExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - Metrics & Monitoring Example ===\n");

        // Step 1: Understanding metrics
        Console.WriteLine("Step 1: Key Metrics to Monitor");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Gateway Metrics Categories:

1. Request Metrics
   - Total requests
   - Requests per second (throughput)
   - Success/error rates
   - HTTP status code distribution

2. Latency Metrics
   - Average latency
   - Median (p50) latency
   - 95th percentile (p95) latency
   - 99th percentile (p99) latency
   - Min/max latency

3. Per-Route Metrics
   - Requests per route
   - Error rate per route
   - Latency per route
   - Top routes by traffic

4. Cache Metrics
   - Cache hit rate
   - Cache misses
   - Cached responses count
   - Cache memory usage

5. Rate Limit Metrics
   - Violations count
   - Violations per client
   - Top violating clients

6. Circuit Breaker Metrics
   - Opens count
   - Closes count
   - Current open circuits
   - Recovery success rate

7. Health Metrics
   - Healthy services
   - Unhealthy services
   - Service availability %
   - Health check response time
");

        // Step 2: Accessing metrics
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 2: Accessing Gateway Metrics");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Metrics Endpoints:

1. Overall Gateway Metrics
   GET /api/metrics

   Returns:
   {
     ""totalRequests"": 10000,
     ""totalErrors"": 50,
     ""errorRate"": 0.005,
     ""averageLatencyMs"": 125,
     ""p95LatencyMs"": 500,
     ""p99LatencyMs"": 1200,
     ""requestsPerSecond"": 2.78
   }

2. Per-Route Metrics
   GET /api/metrics/routes/{routeName}

   Returns latency percentiles, status codes, error types

3. Health Status
   GET /health
   GET /health/{serviceName}

   Returns service health and response times

4. Rate Limit Status
   GET /api/gateway/ratelimit/{clientId}

   Returns current rate limit usage

5. Circuit Breaker Status
   GET /api/gateway/circuitbreaker/{routeName}

   Returns state, failure count, recovery progress
");

        // Step 3: Metrics analysis
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 3: Analyzing Metrics for Issues");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Metric Analysis Guide:

High Error Rate (>5%)?
  ✓ Check backend service health
  ✓ Review circuit breaker status
  ✓ Check error logs for details
  ✓ Look for pattern (all endpoints or specific?)

High Latency (p95 > 500ms)?
  ✓ Check backend response times
  ✓ Review gateway logs for processing overhead
  ✓ Check network connectivity
  ✓ Enable detailed logging

High Rate Limit Violations?
  ✓ Legitimate surge or attack?
  ✓ Consider increasing limits if legitimate
  ✓ Implement rate limit by IP/key if attack
  ✓ Alert on sudden increase

Circuit Breaker Frequently Opening?
  ✓ Backend service unstable
  ✓ Lower failure threshold if too aggressive
  ✓ Increase timeout for slow recovery
  ✓ Check backend logs for root cause

Cache Hit Rate < 80%?
  ✓ Increase TTL for stable endpoints
  ✓ Check if caching is configured
  ✓ Review request patterns
  ✓ Consider pre-warming cache
");

        // Step 4: Simulation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 4: Simulating Real-World Metrics");
        Console.WriteLine(new string('-', 60));

        var simulator = new MetricsSimulator();

        // Normal operation
        Console.WriteLine("\nScenario 1: Normal Operation");
        Console.WriteLine(new string('-', 40));

        for (int i = 0; i < 100; i++)
        {
            simulator.RecordRequest("user-service", 105, 200);
            simulator.RecordRequest("order-service", 240, 200);
            simulator.RecordRequest("payment-service", 85, 200);
        }

        Console.WriteLine("After 100 requests to each service:");
        Console.WriteLine();
        simulator.DisplayMetrics();

        // Degradation
        Console.WriteLine("\nScenario 2: Service Degradation");
        Console.WriteLine(new string('-', 40));

        for (int i = 0; i < 100; i++)
        {
            simulator.RecordRequest("order-service", 1200, 500); // Slow responses
            simulator.RecordRequest("payment-service", 85, 200);
        }

        Console.WriteLine("After order-service becomes slow:");
        Console.WriteLine();
        simulator.DisplayMetrics();

        // Step 5: Alerting
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 5: Alerting Strategy");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Recommended Alerts:

Critical (Page on-call):
  ✗ Any service health = unhealthy
  ✗ Error rate > 10%
  ✗ Circuit breaker stuck OPEN > 5 minutes
  ✗ Gateway unresponsive (health check timeout)

Major (Ticket, notify team):
  ⚠ Error rate > 5%
  ⚠ P95 latency > 1000ms
  ⚠ Circuit breaker opens/closes repeatedly
  ⚠ Cache hit rate < 50%

Minor (Dashboard alert):
  ℹ Error rate > 1%
  ℹ P95 latency > 500ms
  ℹ Rate limit violations > 100/hour
  ℹ Cache hit rate < 80%

Alerting Tools Integration:
  - Prometheus + AlertManager
  - Grafana + Alerting rules
  - Datadog monitors
  - PagerDuty escalation
  - Slack notifications
");

        // Step 6: Dashboards
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 6: Creating Dashboards");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Grafana Dashboard Example:

Panel 1: Throughput
  - Title: Requests per Second
  - Query: rate(gateway_requests_total[1m])
  - Alert: threshold=10 requests/sec

Panel 2: Error Rate
  - Title: Error Rate %
  - Query: rate(gateway_errors_total[1m]) / rate(gateway_requests_total[1m])
  - Alert: threshold=5%

Panel 3: Latency Percentiles
  - Title: Response Time Percentiles
  - Query: gateway_request_duration_p50, p95, p99
  - Alert: p95 > 500ms

Panel 4: Service Health
  - Title: Service Status
  - Query: health_status{service=~"".*""}
  - Color: green=healthy, red=unhealthy

Panel 5: Circuit Breaker State
  - Title: Circuit Breaker Status
  - Query: circuit_breaker_state
  - Colors: green=CLOSED, red=OPEN, yellow=HALF_OPEN

Panel 6: Cache Performance
  - Title: Cache Hit Rate
  - Query: cache_hits / (cache_hits + cache_misses)
  - Target: > 80%
");

        // Step 7: Best practices
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 7: Monitoring Best Practices");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
✓ Do's:
  - Monitor at least 4 golden signals (latency, traffic, errors, saturation)
  - Set up automated alerts
  - Review metrics daily
  - Track trends over time
  - Set realistic thresholds
  - Include team in alert decisions
  - Keep dashboards updated
  - Document alert runbooks

✗ Don'ts:
  - Monitor only success metrics
  - Set alerts for every minor issue (alert fatigue)
  - Ignore alerts (lose credibility)
  - Hardcode thresholds (should be configurable)
  - Alert on metrics you can't act on
  - Create too many dashboards (use 1-3 main ones)
  - Forget to test alerts
  - Ignore historical trends
");

        // Step 8: Tools
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 8: Popular Monitoring Tools");
        Console.WriteLine(new string('-', 60));

        var tools = new[]
        {
            ("Prometheus", "Metrics collection + alerting", "Open source"),
            ("Grafana", "Metrics visualization & dashboards", "Open source"),
            ("Datadog", "Full monitoring & APM", "SaaS, paid"),
            ("New Relic", "Application performance monitoring", "SaaS, paid"),
            ("Azure Monitor", "Azure cloud monitoring", "Azure integrated"),
            ("CloudWatch", "AWS monitoring service", "AWS integrated"),
            ("ELK Stack", "Logs, metrics, traces", "Open source"),
            ("Jaeger", "Distributed tracing", "Open source")
        };

        Console.WriteLine();
        foreach (var (tool, purpose, model) in tools)
        {
            Console.WriteLine($"  {tool:20} → {purpose:40} ({model})");
        }

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private class MetricsSimulator
    {
        private Dictionary<string, List<int>> routeLatencies = new();
        private Dictionary<string, List<int>> routeStatusCodes = new();

        public void RecordRequest(string route, int latencyMs, int statusCode)
        {
            if (!routeLatencies.ContainsKey(route))
            {
                routeLatencies[route] = new List<int>();
                routeStatusCodes[route] = new List<int>();
            }

            routeLatencies[route].Add(latencyMs);
            routeStatusCodes[route].Add(statusCode);
        }

        public void DisplayMetrics()
        {
            foreach (var route in routeLatencies.Keys.OrderBy(x => x))
            {
                var latencies = routeLatencies[route];
                var statusCodes = routeStatusCodes[route];

                var avg = latencies.Average();
                var p50 = latencies.OrderBy(x => x).ElementAt(latencies.Count / 2);
                var p95 = latencies.OrderBy(x => x).ElementAt((int)(latencies.Count * 0.95));
                var p99 = latencies.OrderBy(x => x).ElementAt((int)(latencies.Count * 0.99));

                var errorCount = statusCodes.Count(x => x >= 400);
                var errorRate = (errorCount * 100.0) / statusCodes.Count;

                Console.WriteLine($"Route: {route}");
                Console.WriteLine($"  Requests: {latencies.Count}");
                Console.WriteLine($"  Avg Latency: {avg:F0}ms");
                Console.WriteLine($"  P50: {p50}ms | P95: {p95}ms | P99: {p99}ms");
                Console.WriteLine($"  Error Rate: {errorRate:F1}%");
                Console.WriteLine();
            }
        }
    }
}

/// <summary>
/// To run this example:
///
/// 1. Access gateway metrics:
///    curl http://localhost:5000/api/metrics | jq
///
/// 2. Monitor in real-time:
///    watch -n 1 'curl -s http://localhost:5000/api/metrics | jq .errorRate'
///
/// 3. Set up Grafana dashboard:
///    - Data source: Prometheus at http://gateway:9090
///    - Create panels for each metric type
///    - Set up alert thresholds
///
/// 4. Create Prometheus scrape config:
///    scrape_configs:
///      - job_name: 'api-gateway'
///        static_configs:
///          - targets: ['localhost:5000']
///
/// Key Metrics to Watch:
/// - Error rate should stay < 1%
/// - P95 latency should be < 500ms
/// - Cache hit rate should be > 80%
/// - Circuit breaker should rarely open
/// </summary>
