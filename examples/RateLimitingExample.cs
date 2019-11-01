#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: Rate Limiting Configuration and Testing
/// Demonstrates how to configure and test rate limiting policies.
/// </summary>
public sealed class RateLimitingExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - Rate Limiting Example ===\n");

        // Step 1: Rate limiting configuration
        Console.WriteLine("Step 1: Rate Limiting Configuration");
        Console.WriteLine(new string('-', 60));

        var config = @"
{
  ""GatewayConfiguration"": {
    ""Routes"": [
      {
        ""name"": ""public-api"",
        ""pattern"": ""^/api/public(/.*)?$"",
        ""rateLimitPolicy"": {
          ""enabled"": true,
          ""requestsPerMinute"": 60,
          ""requestsPerSecond"": 2,
          ""burst"": true,
          ""burstSize"": 5
        }
      },
      {
        ""name"": ""premium-api"",
        ""pattern"": ""^/api/premium(/.*)?$"",
        ""rateLimitPolicy"": {
          ""enabled"": true,
          ""requestsPerMinute"": 1000,
          ""requestsPerSecond"": 20
        }
      },
      {
        ""name"": ""unlimited-api"",
        ""pattern"": ""^/api/unlimited(/.*)?$"",
        ""rateLimitPolicy"": {
          ""enabled"": false
        }
      }
    ]
  }
}";

        Console.WriteLine(config);
        Console.WriteLine();

        // Step 2: Token bucket algorithm explanation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 2: Token Bucket Algorithm Explanation");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Token Bucket Algorithm:
  1. Bucket capacity: 60 tokens (requestsPerMinute)
  2. Refill rate: 1 token per second
  3. Each request consumes 1 token
  4. If no tokens available → 429 Too Many Requests

Timeline Example:
  T=0s:   Bucket: 60 tokens | Request → 59 tokens remain
  T=0.5s: Bucket: 59 tokens | Request → 58 tokens remain
  T=1s:   Bucket: 59 tokens | Refill → 60 tokens | Request → 59 remain
  T=60s:  Bucket: Full capacity (refill complete)
");

        // Step 3: Simulate rate limiting
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 3: Simulating Rate Limiting (60 req/min, 2 req/sec)");
        Console.WriteLine(new string('-', 60));

        var rateLimiter = new SimpleTokenBucketRateLimiter(
            requestsPerSecond: 2,
            requestsPerMinute: 60);

        Console.WriteLine("\nSimulating 8 requests in quick succession:");
        Console.WriteLine();

        for (int i = 1; i <= 8; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var allowed = rateLimiter.IsAllowed("client-1");
            stopwatch.Stop();

            var status = allowed ? "✓ ALLOWED" : "✗ BLOCKED (429)";
            Console.WriteLine($"  Request {i}: {status}  |  Tokens remaining: {rateLimiter.GetTokensRemaining("client-1"):F1}");
        }

        Console.WriteLine("\nWaiting 2 seconds for tokens to refill...");
        await Task.Delay(2000);

        Console.WriteLine("\nAfter 2 seconds:");
        Console.WriteLine($"  Tokens available: {rateLimiter.GetTokensRemaining("client-1"):F1}");

        for (int i = 1; i <= 3; i++)
        {
            var allowed = rateLimiter.IsAllowed("client-1");
            var status = allowed ? "✓ ALLOWED" : "✗ BLOCKED (429)";
            Console.WriteLine($"  Request {8 + i}: {status}");
        }

        Console.WriteLine();

        // Step 4: Multiple client isolation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 4: Multi-Client Rate Limiting (Isolation)");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine("\nEach client has independent rate limits:");
        Console.WriteLine();

        var clients = new[] { "client-1", "client-2", "client-3" };

        for (int i = 1; i <= 5; i++)
        {
            Console.WriteLine($"Request batch {i}:");
            foreach (var clientId in clients)
            {
                var allowed = rateLimiter.IsAllowed(clientId);
                var remaining = rateLimiter.GetTokensRemaining(clientId);
                var status = allowed ? "✓" : "✗";
                Console.WriteLine($"  {status} {clientId:15} → Remaining: {remaining:F1}");
            }
            Console.WriteLine();
        }

        // Step 5: Burst handling
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 5: Burst Handling (Emergency Traffic Spike)");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Burst Configuration:
  - Normal rate: 2 requests/second
  - Burst size: 5 additional requests
  - Use case: Handle temporary traffic spikes
  - Cost: Uses quota for burst
");

        var burstLimiter = new SimpleTokenBucketRateLimiter(
            requestsPerSecond: 2,
            requestsPerMinute: 60,
            burstSize: 5);

        Console.WriteLine("\nSimulating burst scenario (10 requests in 1 second):");
        Console.WriteLine();

        int successCount = 0;
        for (int i = 1; i <= 10; i++)
        {
            var allowed = burstLimiter.IsAllowed("burst-client");
            if (allowed) successCount++;

            var status = allowed ? "✓ ALLOWED" : "✗ BLOCKED";
            Console.WriteLine($"  Request {i:2}: {status}  |  Tokens: {burstLimiter.GetTokensRemaining("burst-client"):F1}");
        }

        Console.WriteLine($"\nResult: {successCount}/10 requests allowed (burst + normal rate)");
        Console.WriteLine();

        // Step 6: HTTP response headers
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 6: Rate Limit Response Headers");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Gateway returns rate limit information in response headers:

Success Response (200):
  X-RateLimit-Limit: 60
  X-RateLimit-Remaining: 45
  X-RateLimit-Reset: 1715160660
  X-RateLimit-Retry-After: 0

Rate Limited Response (429):
  X-RateLimit-Limit: 60
  X-RateLimit-Remaining: 0
  X-RateLimit-Reset: 1715160660
  X-RateLimit-Retry-After: 15
");

        // Step 7: Best practices
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 7: Rate Limiting Best Practices");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
✓ Do's:
  - Set limits based on your backend capacity
  - Use tiered limits (free/standard/premium)
  - Return clear 429 responses with Retry-After
  - Log rate limit violations for monitoring
  - Implement gradual backoff in clients
  - Use burst allowance for spike handling

✗ Don'ts:
  - Don't set limits too low (user frustration)
  - Don't set limits too high (backend overload)
  - Don't silently drop requests
  - Don't forget to return proper headers
  - Don't rate limit health check endpoints
  - Don't rate limit internal services
");

        // Step 8: Testing
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 8: Testing Rate Limits");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Test rate limiting with Apache Bench (ab):

  # Make 100 requests with 10 concurrent connections
  ab -n 100 -c 10 http://localhost:5000/api/public/data

Test with curl loop:

  # Make 10 rapid requests
  for i in {{1..10}}; do
    curl -i http://localhost:5000/api/public/data | grep X-RateLimit
  done

Monitor rate limit status:

  # Check current client limits
  curl http://localhost:5000/api/gateway/ratelimit/client-1

  # Reset limits
  curl -X POST http://localhost:5000/api/gateway/ratelimit/client-1/reset
");

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private class SimpleTokenBucketRateLimiter
    {
        private readonly double tokensPerSecond;
        private readonly double maxTokens;
        private readonly double burstSize;
        private readonly Dictionary<string, (double tokens, DateTime lastRefill)> clientBuckets;

        public SimpleTokenBucketRateLimiter(
            double requestsPerSecond,
            double requestsPerMinute,
            double burstSize = 0)
        {
            tokensPerSecond = requestsPerSecond;
            maxTokens = Math.Max(requestsPerSecond, requestsPerMinute / 60.0) + burstSize;
            this.burstSize = burstSize;
            clientBuckets = new Dictionary<string, (double, DateTime)>();
        }

        public bool IsAllowed(string clientId)
        {
            lock (clientBuckets)
            {
                if (!clientBuckets.ContainsKey(clientId))
                {
                    clientBuckets[clientId] = (maxTokens, DateTime.UtcNow);
                }

                var (tokens, lastRefill) = clientBuckets[clientId];
                var now = DateTime.UtcNow;
                var elapsed = (now - lastRefill).TotalSeconds;
                var refilled = tokens + (elapsed * tokensPerSecond);
                var capped = Math.Min(refilled, maxTokens);

                if (capped >= 1)
                {
                    clientBuckets[clientId] = (capped - 1, now);
                    return true;
                }

                clientBuckets[clientId] = (capped, now);
                return false;
            }
        }

        public double GetTokensRemaining(string clientId)
        {
            lock (clientBuckets)
            {
                if (!clientBuckets.ContainsKey(clientId))
                    return maxTokens;

                var (tokens, lastRefill) = clientBuckets[clientId];
                var now = DateTime.UtcNow;
                var elapsed = (now - lastRefill).TotalSeconds;
                return Math.Min(tokens + (elapsed * tokensPerSecond), maxTokens);
            }
        }
    }
}

/// <summary>
/// To run this example:
///
/// 1. Ensure gateway is running with rate limiting configured
/// 2. Test with curl:
///
///    # Normal requests
///    curl http://localhost:5000/api/public/data
///
///    # Check rate limit headers
///    curl -i http://localhost:5000/api/public/data | grep X-RateLimit
///
///    # Rapid requests to trigger 429
///    for i in {1..100}; do curl http://localhost:5000/api/public/data; done
///
/// 3. Monitor rate limit status:
///
///    curl http://localhost:5000/api/gateway/ratelimit/my-client
///
/// Configuration tips:
/// - Set requestsPerMinute based on backend capacity
/// - Use burst for known spike scenarios
/// - Different limits for different API tiers
/// - Monitor violations in metrics
/// </summary>
