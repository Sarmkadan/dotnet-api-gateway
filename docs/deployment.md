# Deployment Guide - DotNet API Gateway

Comprehensive guide for deploying the DotNet API Gateway to production environments.

## Deployment Strategies

### Strategy 1: Docker Container (Recommended)

**Pros:**
- Consistent across environments
- Easy scaling and orchestration
- Simple rollback and updates

**Cons:**
- Additional Docker overhead
- Requires Docker infrastructure

### Strategy 2: Kubernetes

**Pros:**
- Native auto-scaling
- Built-in health monitoring
- Zero-downtime deployments

**Cons:**
- Operational complexity
- Requires Kubernetes cluster

### Strategy 3: Traditional Hosting

**Pros:**
- Simple deployment
- Direct control
- Minimal overhead

**Cons:**
- Manual scaling
- More operational burden

---

## Docker Deployment

### Build Docker Image

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DotNetApiGateway.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnetcore:10.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "DotNetApiGateway.dll"]
```

### Build Image

```bash
docker build -t dotnet-api-gateway:1.0 .
```

### Run Container

```bash
docker run -d \
  --name api-gateway \
  -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /path/to/appsettings.json:/app/appsettings.json \
  dotnet-api-gateway:1.0
```

### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  api-gateway:
    build: .
    container_name: dotnet-api-gateway
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5000
    volumes:
      - ./appsettings.Production.json:/app/appsettings.json
      - ./certs:/app/certs:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Optional: Add backend services for testing
  backend-service:
    image: nginx:latest
    container_name: backend-api
    ports:
      - "3000:80"
    restart: unless-stopped

networks:
  default:
    name: gateway-network
```

Run with:
```bash
docker-compose up -d
```

---

## Kubernetes Deployment

### Create Namespace

```bash
kubectl create namespace api-gateway
```

### Create ConfigMap

```bash
kubectl create configmap gateway-config \
  --from-file=appsettings.json \
  -n api-gateway
```

### Create Deployment

Create `k8s-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-api-gateway
  namespace: api-gateway
  labels:
    app: api-gateway
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
        version: v1.0
    spec:
      containers:
      - name: api-gateway
        image: dotnet-api-gateway:1.0
        imagePullPolicy: Always
        ports:
        - name: http
          containerPort: 5000
          protocol: TCP
        
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: ASPNETCORE_URLS
          value: http://+:5000
        
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        
        volumeMounts:
        - name: config
          mountPath: /app/appsettings.json
          subPath: appsettings.json
          readOnly: true
      
      volumes:
      - name: config
        configMap:
          name: gateway-config
```

Deploy:
```bash
kubectl apply -f k8s-deployment.yaml
```

### Create Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: api-gateway-service
  namespace: api-gateway
spec:
  type: LoadBalancer
  selector:
    app: api-gateway
  ports:
  - name: http
    port: 80
    targetPort: 5000
    protocol: TCP
```

### Auto-scaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-gateway-hpa
  namespace: api-gateway
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: dotnet-api-gateway
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

---

## Production Configuration

### appsettings.Production.json

```json
{
  "DotnetApiGateway": {
    "Port": 80,
    "EnableHttps": true,
    "CertificatePath": "/app/certs/gateway.pfx",
    "CertificatePassword": "${CERT_PASSWORD}",
    "RequestTimeoutSeconds": 30,
    "MaxRequestBodySizeBytes": 5242880,
    "EnableRequestLogging": true,
    "EnableMetrics": true,
    "EnableHealthChecks": true,
    "HealthCheckIntervalSeconds": 60,
    "Routes": [
      {
        "name": "user-service",
        "pattern": "^/api/users(/.*)?$",
        "method": "ANY",
        "targets": [
          {
            "url": "http://user-service-1:3001/api/users",
            "weight": 50,
            "healthCheckUrl": "http://user-service-1:3001/health"
          },
          {
            "url": "http://user-service-2:3001/api/users",
            "weight": 50,
            "healthCheckUrl": "http://user-service-2:3001/health"
          }
        ],
        "rateLimitPolicy": {
          "enabled": true,
          "requestsPerMinute": 5000,
          "requestsPerSecond": 100
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
        },
        "retryPolicy": {
          "enabled": true,
          "maxRetries": 3,
          "backoffMultiplier": 2.0,
          "initialBackoffMs": 100
        }
      }
    ],
    "JwtValidation": {
      "enabled": true,
      "issuer": "https://auth.example.com",
      "audience": "api.example.com",
      "secretKey": "${JWT_SECRET_KEY}",
      "validateIssuer": true,
      "validateAudience": true,
      "validateLifetime": true,
      "clockSkewSeconds": 5
    },
    "CorsPolicy": {
      "enabled": true,
      "allowedOrigins": [
        "https://webapp.example.com",
        "https://admin.example.com"
      ],
      "allowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "allowedHeaders": ["*"],
      "allowCredentials": true,
      "maxAgeSeconds": 86400
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "ApplicationInsights": {
    "InstrumentationKey": "${APPINSIGHTS_INSTRUMENTATIONKEY}"
  }
}
```

---

## Monitoring & Logging

### Application Insights Integration

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();

builder.Logging.AddApplicationInsights();
```

### Structured Logging with Serilog

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(serviceProvider, TelemetryConverter.Traces)
    .CreateLogger();

try
{
    builder.Host.UseSerilog();
    // Build and run...
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

### Health Check Monitoring

```bash
# Monitor health with curl
watch -n 5 'curl -s http://localhost:5000/health | jq'

# With Prometheus
curl http://localhost:5000/metrics
```

---

## Security Considerations

### 1. HTTPS/TLS

```bash
# Generate self-signed certificate (testing only)
dotnet dev-certs https --trust

# Use proper certificate in production
# Store in /app/certs/gateway.pfx
```

### 2. Secrets Management

**Never hardcode secrets!**

Use environment variables or secret managers:

```bash
# Environment variables
export JWT_SECRET_KEY="your-secret-key"
export CERT_PASSWORD="certificate-password"

# Docker secrets
docker run -e JWT_SECRET_KEY=${JWT_SECRET_KEY} ...

# Kubernetes secrets
kubectl create secret generic gateway-secrets \
  --from-literal=jwt-secret-key=... \
  --from-literal=cert-password=...
```

### 3. Network Security

- Use VPC/Private networks
- Implement firewall rules
- Use load balancer with DDoS protection
- Enable rate limiting on gateway
- Implement request validation

### 4. Access Control

```json
{
  "CorsPolicy": {
    "allowedOrigins": [
      "https://trusted-domain.com"
    ]
  },
  "ApiKeyAuthentication": {
    "enabled": true,
    "keys": {
      "trusted-client": "secret-key"
    }
  },
  "JwtValidation": {
    "enabled": true,
    "requiredRoles": ["api-consumer"]
  }
}
```

---

## Scaling Considerations

### Load Balancing

Deploy multiple gateway instances behind a load balancer:

```
[Clients]
    ↓
[Load Balancer - nginx/HAProxy]
    ↓
[Gateway Instance 1]
[Gateway Instance 2]
[Gateway Instance 3]
    ↓
[Backend Services]
```

### Nginx Configuration

```nginx
upstream api_gateway {
    least_conn;
    server gateway-1:5000 max_fails=3 fail_timeout=30s;
    server gateway-2:5000 max_fails=3 fail_timeout=30s;
    server gateway-3:5000 max_fails=3 fail_timeout=30s;
}

server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://api_gateway;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        
        # Health check
        access_log off;
    }
}
```

### Distributed Caching

For multi-instance deployments, use distributed cache:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis-server:6379";
});
```

---

## Backup & Recovery

### Configuration Backup

```bash
# Backup configuration
tar -czf backup-config-$(date +%Y%m%d).tar.gz appsettings*.json

# Restore
tar -xzf backup-config-20260504.tar.gz
```

### Database Backup (if using)

```bash
# Backup routes/policies stored in database
mysqldump -u root gateway_db > backup-$(date +%Y%m%d).sql
```

---

## Monitoring Checklist

- [ ] Health check endpoint responding
- [ ] Metrics visible at `/api/metrics`
- [ ] Logging configured and working
- [ ] Circuit breakers monitoring enabled
- [ ] Rate limits set appropriately
- [ ] HTTPS certificates valid
- [ ] Secrets managed securely
- [ ] Backup procedures in place
- [ ] Auto-scaling configured
- [ ] Alerts set up for failures

---

## Troubleshooting Deployment

### Application Won't Start

```bash
# Check logs
docker logs api-gateway

# Verify configuration
dotnet DotNetApiGateway.dll --validate-config

# Check port availability
netstat -an | grep 5000
```

### High Memory Usage

```bash
# Monitor memory
docker stats api-gateway

# Check cache size
curl http://localhost:5000/api/metrics | jq '.cacheMetrics'
```

### Certificate Errors

```bash
# Verify certificate
openssl x509 -in gateway.pfx -text -noout

# Update certificate
cp /path/to/new-cert.pfx /app/certs/gateway.pfx
docker restart api-gateway
```

---

## Performance Tuning

### .NET Runtime Optimizations

```bash
# Use tiered compilation
DOTNET_TieredCompilation=1

# Use write cache
DOTNET_ReadyToRun=1

# GC tuning
DOTNET_gcServer=1
DOTNET_gcRetainVM=1
```

### HTTP Optimization

```json
{
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 5242880,
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

---

## Post-Deployment Validation

```bash
# Test health endpoint
curl -f http://api.example.com/health || exit 1

# Test routing
curl http://api.example.com/api/users

# Test rate limiting
for i in {1..150}; do
  curl -s http://api.example.com/api/users > /dev/null
done

# Verify metrics
curl http://api.example.com/api/metrics | jq '.totalRequests'
```

---

For more information, see [Getting Started](./getting-started.md) or the main [README](../README.md).
