#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: Circuit Breaker Pattern
/// Demonstrates how circuit breaker protects against cascading failures.
/// </summary>
public sealed class CircuitBreakerExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - Circuit Breaker Example ===\n");

        // Step 1: Problem without circuit breaker
        Console.WriteLine("Step 1: Problem Without Circuit Breaker");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Scenario: Backend service is down

Without Circuit Breaker:
  Request 1 → Backend  → Timeout (30s) → Wait complete → Fail
  Request 2 → Backend  → Timeout (30s) → Wait complete → Fail
  Request 3 → Backend  → Timeout (30s) → Wait complete → Fail
  ...
  Result: All requests timeout, user experience is terrible

With Circuit Breaker:
  Request 1-5 → Failures accumulate (5 = threshold)
  Request 6 → Circuit OPENS → Instant failure (no timeout) → Fail fast
  Request 7 → Circuit OPEN → Instant failure → Fail fast
  Request 8 → Circuit still OPEN → Instant failure → Fail fast
  (After timeout)
  Request 9 → Circuit HALF_OPEN → Test with 1 request
  (If success)
  Request 10+ → Circuit CLOSED → Normal operation → Success
");

        // Step 2: Configuration
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 2: Circuit Breaker Configuration");
        Console.WriteLine(new string('-', 60));

        var config = @"
{
  ""GatewayConfiguration"": {
    ""Routes"": [
      {
        ""name"": ""critical-service"",
        ""pattern"": ""^/api/critical(/.*)?$"",
        ""targets"": [{ ""url"": ""http://backend:3000"" }],
        ""circuitBreakerPolicy"": {
          ""enabled"": true,
          ""failureThreshold"": 5,
          ""successThreshold"": 2,
          ""timeoutSeconds"": 60,
          ""halfOpenMaxCalls"": 1
        }
      },
      {
        ""name"": ""resilient-service"",
        ""pattern"": ""^/api/resilient(/.*)?$"",
        ""targets"": [{ ""url"": ""http://backend:3000"" }],
        ""circuitBreakerPolicy"": {
          ""enabled"": true,
          ""failureThreshold"": 10,
          ""successThreshold"": 5,
          ""timeoutSeconds"": 120
        }
      },
      {
        ""name"": ""unprotected-service"",
        ""pattern"": ""^/api/unprotected(/.*)?$"",
        ""circuitBreakerPolicy"": {
          ""enabled"": false
        }
      }
    ]
  }
}";

        Console.WriteLine(config);
        Console.WriteLine();

        // Step 3: State transitions
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 3: Circuit Breaker State Transitions");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
State Machine:

    ┌─────────────────────────────────────────────────────┐
    │                                                     │
    │    ┌──────────┐                                     │
    │    │  CLOSED  │◄──────────────────────────────────┐│
    │    └────┬─────┘                                   ││
    │         │ failureCount >= failureThreshold         ││
    │         ▼                                           ││
    │    ┌──────────┐                                     ││
    │    │  OPEN    ├──────────────────┐                 ││
    │    └────┬─────┘                  │                 ││
    │         │ timeout elapsed        │                 ││
    │         ▼                        ▼                 ││
    │    ┌──────────┐    ┌────────────────────┐          ││
    │    │HALF_OPEN├───►successCount >=       ├──────────┘│
    │    └──────────┘    │successThreshold    │           │
    │                    └────────────────────┘           │
    └─────────────────────────────────────────────────────┘

Configuration Parameters:
  - failureThreshold: Number of failures to trigger OPEN (default: 5)
  - successThreshold: Consecutive successes to restore (default: 2)
  - timeoutSeconds: Duration before trying recovery (default: 60)
  - halfOpenMaxCalls: Max requests in HALF_OPEN state (default: 1)
");

        // Step 4: Simulation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 4: Simulating Circuit Breaker Behavior");
        Console.WriteLine(new string('-', 60));

        var breaker = new SimpleCircuitBreaker(
            failureThreshold: 3,
            successThreshold: 2,
            timeoutSeconds: 5);

        Console.WriteLine("\nInitial state: CLOSED");
        Console.WriteLine();

        // Simulate failures
        for (int i = 1; i <= 4; i++)
        {
            Console.WriteLine($"Request {i}: Backend returns error");
            breaker.RecordFailure();
            Console.WriteLine($"  State: {breaker.State} | Failures: {breaker.FailureCount}");

            if (breaker.State == CircuitState.Open)
            {
                Console.WriteLine("  ⚠️  CIRCUIT OPENED - Fail-fast mode enabled");
                break;
            }
        }

        Console.WriteLine();

        // Try to use while open
        Console.WriteLine("Request 5: Circuit is OPEN");
        if (!breaker.CanExecute())
        {
            Console.WriteLine("  ✗ REJECTED immediately (Circuit is OPEN) - Fail fast!");
            Console.WriteLine($"  State: {breaker.State}");
        }

        Console.WriteLine();
        Console.WriteLine("Waiting 6 seconds for timeout...");
        Task.Delay(6000).Wait();

        Console.WriteLine();
        Console.WriteLine("Request 6: After timeout, circuit transitions to HALF_OPEN");
        Console.WriteLine($"  State: {breaker.State}");

        if (breaker.CanExecute())
        {
            Console.WriteLine("  ✓ Request allowed (testing recovery)");
            Console.WriteLine("  Backend returns success!");
            breaker.RecordSuccess();
            Console.WriteLine($"  Successes: {breaker.SuccessCount} / {2}");
        }

        Console.WriteLine();
        Console.WriteLine("Request 7: Second success");
        if (breaker.CanExecute())
        {
            Console.WriteLine("  ✓ Request allowed");
            Console.WriteLine("  Backend returns success!");
            breaker.RecordSuccess();
            Console.WriteLine($"  Successes: {breaker.SuccessCount} / {2}");
            Console.WriteLine();
            Console.WriteLine("  ✓✓ CIRCUIT CLOSED - Service recovered!");
            Console.WriteLine($"  State: {breaker.State}");
        }

        Console.WriteLine();

        // Step 5: Integration with gateway
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 5: Monitoring Circuit Breaker via Gateway API");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Check circuit breaker status:

  curl http://localhost:5000/api/gateway/circuitbreaker/critical-service

Response:
{
  ""routeName"": ""critical-service"",
  ""status"": ""OPEN"",
  ""failureCount"": 5,
  ""successCount"": 0,
  ""failureThreshold"": 5,
  ""successThreshold"": 2,
  ""timeoutSeconds"": 60,
  ""lastFailureTime"": ""2026-05-04T10:00:30Z"",
  ""lastSuccessTime"": null,
  ""openedAt"": ""2026-05-04T10:00:35Z"",
  ""willResetAt"": ""2026-05-04T10:01:35Z""
}

Reset manually:

  curl -X POST http://localhost:5000/api/gateway/circuitbreaker/critical-service/reset
");

        // Step 6: Best practices
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 6: Circuit Breaker Best Practices");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
✓ Configuration Strategy:
  - Set failureThreshold = expected error rate × request rate
  - Use lower timeout for fast recovery
  - Adjust successThreshold based on backend stability
  - Different settings per service criticality

✓ Fallback Strategy:
  - Return cached response when circuit opens
  - Implement degraded functionality
  - Use circuit breaker + retry + timeout together
  - Monitor fallback activation

✓ Monitoring:
  - Alert on circuit state changes
  - Track open circuit duration
  - Monitor recovery success rate
  - Log circuit breaker events

✗ Common Mistakes:
  - Setting failureThreshold too low (too aggressive)
  - Setting timeoutSeconds too high (slow recovery)
  - Not monitoring circuit state changes
  - Cascading failures (not protecting all calls)
  - No fallback strategy
");

        // Step 7: Scenarios
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 7: Real-World Scenarios");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine(@"
Scenario 1: Database Outage
  Without CB: 1000 requests timeout (30s each) = 500+ minute wait
  With CB:    Circuit opens, fail-fast, DB admin can fix issue

Scenario 2: Service Restart
  Without CB: Requests timeout during restart window
  With CB:    Circuit opens, waits, recovers gracefully

Scenario 3: Cascading Failures
  Without CB: Service A fails → Service B hammered → Service B fails → ...
  With CB:    Service A protected → B protected → limits propagation

Scenario 4: Traffic Spike
  Without CB: Spike → Backend overload → Slower responses → More timeouts
  With CB:    Spike → Circuit opens → Fail-fast → Load reduced → Recovery
");

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    private class SimpleCircuitBreaker
    {
        private readonly int failureThreshold;
        private readonly int successThreshold;
        private readonly int timeoutSeconds;
        private CircuitState state = CircuitState.Closed;
        private int failureCount = 0;
        private int successCount = 0;
        private DateTime? openedAt;

        public CircuitState State => state;
        public int FailureCount => failureCount;
        public int SuccessCount => successCount;

        public SimpleCircuitBreaker(
            int failureThreshold = 5,
            int successThreshold = 2,
            int timeoutSeconds = 60)
        {
            this.failureThreshold = failureThreshold;
            this.successThreshold = successThreshold;
            this.timeoutSeconds = timeoutSeconds;
        }

        public bool CanExecute()
        {
            if (state == CircuitState.Closed)
                return true;

            if (state == CircuitState.Open)
            {
                if (DateTime.UtcNow.Subtract(openedAt.Value).TotalSeconds >= timeoutSeconds)
                {
                    state = CircuitState.HalfOpen;
                    successCount = 0;
                    return true;
                }
                return false;
            }

            if (state == CircuitState.HalfOpen)
                return true;

            return false;
        }

        public void RecordFailure()
        {
            if (state == CircuitState.HalfOpen)
            {
                state = CircuitState.Open;
                openedAt = DateTime.UtcNow;
                failureCount++;
            }
            else if (state == CircuitState.Closed)
            {
                failureCount++;
                if (failureCount >= failureThreshold)
                {
                    state = CircuitState.Open;
                    openedAt = DateTime.UtcNow;
                }
            }
        }

        public void RecordSuccess()
        {
            if (state == CircuitState.HalfOpen)
            {
                successCount++;
                if (successCount >= successThreshold)
                {
                    state = CircuitState.Closed;
                    failureCount = 0;
                    successCount = 0;
                }
            }
            else if (state == CircuitState.Closed)
            {
                failureCount = 0;
            }
        }
    }
}

/// <summary>
/// To run this example:
///
/// 1. Check circuit breaker status:
///    curl http://localhost:5000/api/gateway/circuitbreaker/critical-service
///
/// 2. Monitor state changes:
///    watch -n 1 'curl -s http://localhost:5000/api/gateway/circuitbreaker/critical-service | jq .status'
///
/// 3. Reset circuit breaker:
///    curl -X POST http://localhost:5000/api/gateway/circuitbreaker/critical-service/reset
///
/// Key Benefits:
/// - Prevents cascading failures
/// - Enables fast failure detection
/// - Provides time for recovery
/// - Reduces resource waste
/// - Improves user experience (fail-fast vs timeout)
/// </summary>
