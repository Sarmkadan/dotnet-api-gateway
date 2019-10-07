# API Reference - DotNet API Gateway

Complete documentation of all API endpoints provided by the gateway for monitoring, configuration, and management.

## Base URL

```
http://localhost:5000
https://localhost:5001 (if HTTPS enabled)
```

## Authentication

### JWT Bearer Token

```http
Authorization: Bearer {token}
```

### API Key Header

```http
X-API-Key: {api-key}
```

---

## Health & Monitoring Endpoints

### Get Gateway Health

Returns overall gateway and backend services health status.

```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "healthy",
  "uptime": 3600,
  "timestamp": "2026-05-04T10:00:00Z",
  "services": {
    "user-service": {
      "status": "healthy",
      "responseTime": 45,
      "lastCheck": "2026-05-04T10:00:00Z"
    },
    "order-service": {
      "status": "unhealthy",
      "responseTime": 0,
      "lastCheck": "2026-05-04T09:59:30Z",
      "error": "Connection timeout"
    }
  }
}
```

**Possible Status Values:**
- `healthy` - All services responsive
- `degraded` - Some services unresponsive
- `unhealthy` - Critical services down

---

### Get Service Health

Check specific backend service health.

```http
GET /health/{serviceName}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| serviceName | string | Yes | Name of the route/service |

**Example:**
```http
GET /health/user-service
```

**Response (200 OK):**
```json
{
  "serviceName": "user-service",
  "status": "healthy",
  "responseTime": 42,
  "lastCheck": "2026-05-04T10:00:00Z",
  "consecutiveFailures": 0,
  "consecutiveSuccesses": 127
}
```

---

## Route Management Endpoints

### List All Routes

Get all configured routes in the gateway.

```http
GET /api/gateway/routes
```

**Response (200 OK):**
```json
{
  "totalRoutes": 3,
  "routes": [
    {
      "name": "user-service",
      "pattern": "^/api/users(/.*)?$",
      "method": "ANY",
      "description": "User service routing",
      "requiresAuthentication": false,
      "targets": [
        {
          "url": "http://localhost:3001/api/users",
          "weight": 100,
          "healthCheckUrl": "http://localhost:3001/health",
          "isHealthy": true
        }
      ],
      "rateLimitPolicy": {
        "enabled": true,
        "requestsPerMinute": 100,
        "requestsPerSecond": 10
      },
      "cachingPolicy": {
        "enabled": true,
        "ttlSeconds": 300
      },
      "circuitBreakerPolicy": {
        "enabled": true,
        "failureThreshold": 5,
        "successThreshold": 2,
        "timeoutSeconds": 60
      }
    }
  ]
}
```

---

### Get Route Details

Get detailed information about a specific route.

```http
GET /api/gateway/routes/{routeName}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| routeName | string | Yes | Route identifier |

**Example:**
```http
GET /api/gateway/routes/user-service
```

**Response (200 OK):**
```json
{
  "name": "user-service",
  "pattern": "^/api/users(/.*)?$",
  "method": "ANY",
  "description": "User service routing",
  "requiresAuthentication": false,
  "requiredRoles": [],
  "targets": [...],
  "rateLimitPolicy": {...},
  "cachingPolicy": {...},
  "circuitBreakerPolicy": {...},
  "retryPolicy": {
    "enabled": true,
    "maxRetries": 3,
    "backoffMultiplier": 2.0,
    "initialBackoffMs": 100
  },
  "requestTransformPolicy": {
    "addHeaders": {
      "X-Forwarded-By": "api-gateway"
    },
    "removeHeaders": ["Cookie"],
    "modifyHeaders": {}
  }
}
```

**Possible Status Codes:**
- `200 OK` - Route found
- `404 Not Found` - Route does not exist

---

### Create Route

Add a new route to the gateway.

```http
POST /api/gateway/routes
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "new-service",
  "pattern": "^/api/new(/.*)?$",
  "method": "GET",
  "description": "New service route",
  "requiresAuthentication": false,
  "targets": [
    {
      "url": "http://localhost:3002/api/new",
      "weight": 100,
      "healthCheckUrl": "http://localhost:3002/health"
    }
  ],
  "rateLimitPolicy": {
    "enabled": true,
    "requestsPerMinute": 100
  },
  "cachingPolicy": {
    "enabled": false
  },
  "circuitBreakerPolicy": {
    "enabled": true,
    "failureThreshold": 5
  }
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Route 'new-service' created successfully",
  "route": {
    "name": "new-service",
    ...
  }
}
```

**Validation Rules:**
- `name`: Required, unique, alphanumeric
- `pattern`: Required, valid regex
- `method`: GET, POST, PUT, DELETE, PATCH, ANY
- `targets`: At least one target required
- `url`: Valid HTTP(S) URL

---

### Update Route

Modify an existing route configuration.

```http
PUT /api/gateway/routes/{routeName}
Content-Type: application/json
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| routeName | string | Yes | Route identifier |

**Request Body:**
```json
{
  "description": "Updated description",
  "rateLimitPolicy": {
    "enabled": true,
    "requestsPerMinute": 200
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Route updated successfully",
  "route": {...}
}
```

---

### Delete Route

Remove a route from the gateway.

```http
DELETE /api/gateway/routes/{routeName}
```

**Example:**
```http
DELETE /api/gateway/routes/user-service
```

**Response (204 No Content):**
No body

**Response (404 Not Found):**
```json
{
  "error": "Route not found"
}
```

---

## Rate Limiting Endpoints

### Get Rate Limit Status

Check current rate limit status for a client.

```http
GET /api/gateway/ratelimit/{clientId}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | string | Yes | Client identifier |

**Example:**
```http
GET /api/gateway/ratelimit/client-123
```

**Response (200 OK):**
```json
{
  "clientId": "client-123",
  "routeName": "user-service",
  "requestsRemaining": 85,
  "requestsLimit": 100,
  "requestsPerMinute": 100,
  "resetTime": "2026-05-04T10:01:00Z",
  "currentWindow": {
    "startTime": "2026-05-04T10:00:00Z",
    "endTime": "2026-05-04T10:01:00Z",
    "requestsUsed": 15
  }
}
```

---

### Reset Rate Limit

Reset rate limit counter for a client.

```http
POST /api/gateway/ratelimit/{clientId}/reset
```

**Example:**
```http
POST /api/gateway/ratelimit/client-123/reset
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Rate limit reset for client-123"
}
```

---

## Circuit Breaker Endpoints

### Get Circuit Breaker Status

Get circuit breaker state for a route.

```http
GET /api/gateway/circuitbreaker/{routeName}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| routeName | string | Yes | Route identifier |

**Example:**
```http
GET /api/gateway/circuitbreaker/user-service
```

**Response (200 OK):**
```json
{
  "routeName": "user-service",
  "status": "CLOSED",
  "failureCount": 2,
  "successCount": 127,
  "failureThreshold": 5,
  "successThreshold": 2,
  "lastFailureTime": "2026-05-04T09:55:30Z",
  "lastSuccessTime": "2026-05-04T09:59:50Z",
  "openedAt": null,
  "willResetAt": null
}
```

**Possible Status Values:**
- `CLOSED` - Normal operation
- `OPEN` - Circuit broken, requests rejected
- `HALF_OPEN` - Testing recovery

---

### Reset Circuit Breaker

Manually reset circuit breaker to CLOSED state.

```http
POST /api/gateway/circuitbreaker/{routeName}/reset
```

**Example:**
```http
POST /api/gateway/circuitbreaker/user-service/reset
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Circuit breaker for route 'user-service' reset to CLOSED"
}
```

---

## Metrics & Analytics Endpoints

### Get Gateway Metrics

Get comprehensive gateway metrics and statistics.

```http
GET /api/metrics
```

**Response (200 OK):**
```json
{
  "timestamp": "2026-05-04T10:00:00Z",
  "uptime": 3600,
  "totalRequests": 10000,
  "totalErrors": 50,
  "totalSuccesses": 9950,
  "errorRate": 0.005,
  "averageLatencyMs": 125,
  "medianLatencyMs": 95,
  "p95LatencyMs": 500,
  "p99LatencyMs": 1200,
  "requestsPerSecond": 2.78,
  "routeMetrics": {
    "user-service": {
      "totalRequests": 5000,
      "totalErrors": 10,
      "errorRate": 0.002,
      "averageLatencyMs": 105,
      "p95LatencyMs": 450
    },
    "order-service": {
      "totalRequests": 3000,
      "totalErrors": 30,
      "errorRate": 0.01,
      "averageLatencyMs": 200,
      "p95LatencyMs": 800
    }
  },
  "cacheMetrics": {
    "totalCacheHits": 2000,
    "totalCacheMisses": 3000,
    "cacheHitRate": 0.4,
    "cachedResponses": 45
  },
  "rateLimitMetrics": {
    "totalViolations": 50,
    "violations": {
      "client-1": 20,
      "client-2": 30
    }
  },
  "circuitBreakerMetrics": {
    "totalBreaks": 3,
    "openCircuits": 1,
    "halfOpenCircuits": 0
  }
}
```

---

### Get Route Metrics

Get detailed metrics for a specific route.

```http
GET /api/metrics/routes/{routeName}
```

**Example:**
```http
GET /api/metrics/routes/user-service
```

**Response (200 OK):**
```json
{
  "routeName": "user-service",
  "pattern": "^/api/users(/.*)?$",
  "totalRequests": 5000,
  "totalErrors": 10,
  "totalSuccesses": 4990,
  "errorRate": 0.002,
  "successRate": 0.998,
  "latency": {
    "averageMs": 105,
    "medianMs": 85,
    "p50Ms": 85,
    "p75Ms": 200,
    "p95Ms": 450,
    "p99Ms": 800,
    "minMs": 12,
    "maxMs": 2500
  },
  "requestsPerSecond": 1.39,
  "httpStatusCodes": {
    "200": 4980,
    "400": 5,
    "500": 5,
    "503": 10
  },
  "topErrorTypes": [
    {
      "type": "ConnectionTimeout",
      "count": 5
    },
    {
      "type": "Http500",
      "count": 5
    }
  ]
}
```

---

## Webhook Endpoints

### Register Webhook

Register a webhook for gateway events.

```http
POST /api/webhooks/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "url": "https://myapp.com/webhooks/gateway",
  "events": [
    "route-created",
    "route-deleted",
    "circuitbreaker-opened",
    "health-check-failed"
  ],
  "retryPolicy": {
    "maxRetries": 3,
    "backoffMs": 1000
  },
  "active": true
}
```

**Response (201 Created):**
```json
{
  "webhookId": "webhook-123",
  "url": "https://myapp.com/webhooks/gateway",
  "events": [...],
  "createdAt": "2026-05-04T10:00:00Z",
  "active": true
}
```

---

### List Webhooks

Get all registered webhooks.

```http
GET /api/webhooks
```

**Response (200 OK):**
```json
{
  "totalWebhooks": 2,
  "webhooks": [
    {
      "webhookId": "webhook-123",
      "url": "https://myapp.com/webhooks/gateway",
      "events": [...],
      "active": true,
      "createdAt": "2026-05-04T10:00:00Z",
      "lastDeliveryAt": "2026-05-04T09:59:00Z",
      "lastDeliveryStatus": 200
    }
  ]
}
```

---

### Delete Webhook

Remove a webhook.

```http
DELETE /api/webhooks/{webhookId}
```

**Example:**
```http
DELETE /api/webhooks/webhook-123
```

**Response (204 No Content):**
No body

---

## Error Responses

### Standard Error Format

All error responses follow this format:

```json
{
  "error": "Error title",
  "message": "Detailed error message",
  "timestamp": "2026-05-04T10:00:00Z",
  "traceId": "request-trace-id",
  "details": {
    "field": "Additional error details"
  }
}
```

### Common HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | Request succeeded |
| 201 | Created | New route created |
| 204 | No Content | Successful deletion |
| 400 | Bad Request | Invalid request body |
| 401 | Unauthorized | Invalid JWT token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Route not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Gateway error |
| 502 | Bad Gateway | Backend service error |
| 503 | Service Unavailable | Overloaded or down |
| 504 | Gateway Timeout | Request timeout |

---

## Rate Limits

### Gateway API Rate Limits

Management endpoints are rate-limited to prevent abuse:

| Endpoint Type | Limit |
|---------------|-------|
| Health checks | 1000 req/min |
| Route management | 100 req/min |
| Metrics queries | 500 req/min |
| Webhook operations | 50 req/min |

---

## Code Examples

### cURL

```bash
# List routes
curl http://localhost:5000/api/gateway/routes

# Create route
curl -X POST http://localhost:5000/api/gateway/routes \
  -H "Content-Type: application/json" \
  -d @route.json

# Check rate limit
curl http://localhost:5000/api/gateway/ratelimit/client-123
```

### PowerShell

```powershell
# Get metrics
$metrics = Invoke-WebRequest http://localhost:5000/api/metrics | ConvertFrom-Json
$metrics.routeMetrics

# Check health
Invoke-WebRequest http://localhost:5000/health | Select-Object StatusCode
```

### C# with HttpClient

```csharp
using var client = new HttpClient { BaseAddress = new("http://localhost:5000") };

// Get routes
var response = await client.GetAsync("/api/gateway/routes");
var json = await response.Content.ReadAsStringAsync();

// Get metrics
var metrics = await client.GetStringAsync("/api/metrics");
```

---

For more information, see the main [README](../README.md) or [Configuration Guide](./configuration.md).
