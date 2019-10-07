# Getting Started with DotNet API Gateway

This guide will help you set up and run the DotNet API Gateway in your local environment.

## Prerequisites

Before you begin, ensure you have:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** - Required for building and running
- **Git** - For cloning the repository
- **A text editor or IDE** - VS Code, Visual Studio, or JetBrains Rider recommended
- **Basic command-line knowledge** - For running dotnet commands
- **curl or Postman** - For testing API endpoints (optional but recommended)

### System Requirements

- **Minimum**: 512MB RAM, 100MB disk space
- **Recommended**: 2GB RAM, 1GB disk space
- **Supported OS**: Windows, macOS, Linux

## Installation Steps

### Step 1: Clone the Repository

```bash
git clone https://github.com/Sarmkadan/dotnet-api-gateway.git
cd dotnet-api-gateway
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

This downloads all required NuGet packages specified in `DotNetApiGateway.csproj`.

### Step 3: Build the Project

```bash
dotnet build
```

For Release mode (optimized):
```bash
dotnet build -c Release
```

### Step 4: Run the Application

```bash
dotnet run
```

You should see output similar to:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

The gateway is now running and ready to accept requests.

## First API Call

### Test Basic Connectivity

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-05-04T10:00:00Z"
}
```

### Configure Your First Route

Edit `appsettings.json` and add a route to an existing API:

```json
{
  "GatewayConfiguration": {
    "Port": 5000,
    "Routes": [
      {
        "name": "jsonplaceholder",
        "pattern": "^/api/posts(/.*)?$",
        "method": "GET",
        "targets": [
          {
            "url": "https://jsonplaceholder.typicode.com/posts",
            "weight": 100
          }
        ]
      }
    ]
  }
}
```

Restart the gateway:
```bash
# Stop the running instance (Ctrl+C), then run again
dotnet run
```

### Make Your First Routed Request

```bash
curl http://localhost:5000/api/posts/1
```

You should receive the JSON response from the backend API, forwarded through your gateway.

## Configuration Basics

### Basic Configuration Structure

The `appsettings.json` file controls gateway behavior:

```json
{
  "GatewayConfiguration": {
    "Port": 5000,
    "EnableHttps": false,
    "RequestTimeoutSeconds": 30,
    "Routes": []
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Essential Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Port` | 5000 | HTTP listening port |
| `EnableHttps` | false | Enable HTTPS (requires certificate) |
| `RequestTimeoutSeconds` | 30 | Request timeout in seconds |
| `MaxRequestBodySizeBytes` | 10485760 | Max request body (10MB) |
| `Routes` | [] | List of configured routes |

### Adding Your First Route

```json
{
  "name": "my-service",
  "pattern": "^/api/myservice(/.*)?$",
  "method": "ANY",
  "targets": [
    {
      "url": "http://localhost:3000/api/myservice",
      "weight": 100
    }
  ]
}
```

**Route Fields**:
- `name`: Unique route identifier
- `pattern`: Regex pattern to match request paths
- `method`: HTTP method (GET, POST, PUT, DELETE, PATCH, ANY)
- `targets`: List of backend services to route to
- `url`: Backend service URL
- `weight`: Load balancing weight (1-100)

## Environment Setup

### Development Environment

For local development with hot reload:

```bash
dotnet watch run
```

Changes to `.cs` files will automatically recompile and restart the application.

### Environment Variables

Override settings with environment variables:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5000
dotnet run
```

### Multiple Environments

Create environment-specific configuration files:
- `appsettings.Development.json`
- `appsettings.Production.json`
- `appsettings.Staging.json`

Example `appsettings.Production.json`:

```json
{
  "GatewayConfiguration": {
    "Port": 80,
    "EnableHttps": true,
    "CertificatePath": "/etc/ssl/certs/gateway.pfx",
    "CertificatePassword": "${CERT_PASSWORD}"
  }
}
```

Load based on environment:
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

## Basic Features

### 1. Rate Limiting

Limit requests per client:

```json
{
  "routes": [
    {
      "name": "limited-api",
      "pattern": "^/api/limited(/.*)?$",
      "rateLimitPolicy": {
        "enabled": true,
        "requestsPerMinute": 60,
        "requestsPerSecond": 2
      }
    }
  ]
}
```

Test rate limiting:
```bash
# Make more than 2 requests per second
for i in {1..5}; do
  curl http://localhost:5000/api/limited
done
```

You'll receive 429 (Too Many Requests) when limit is exceeded.

### 2. Caching Responses

Cache backend responses:

```json
{
  "routes": [
    {
      "name": "cached-api",
      "pattern": "^/api/data(/.*)?$",
      "cachingPolicy": {
        "enabled": true,
        "ttlSeconds": 300
      }
    }
  ]
}
```

Cached responses return within milliseconds.

### 3. Circuit Breaker Protection

Fail fast when backend is down:

```json
{
  "routes": [
    {
      "name": "protected-api",
      "pattern": "^/api/protected(/.*)?$",
      "circuitBreakerPolicy": {
        "enabled": true,
        "failureThreshold": 5,
        "timeoutSeconds": 60
      }
    }
  ]
}
```

When threshold is reached, requests fail immediately without hitting the backend.

### 4. JWT Authentication

Require valid JWT tokens:

```json
{
  "GatewayConfiguration": {
    "JwtValidation": {
      "enabled": true,
      "secretKey": "your-secret-key-here",
      "issuer": "https://auth-server.com",
      "audience": "api.gateway"
    }
  },
  "routes": [
    {
      "name": "secure-api",
      "pattern": "^/api/secure(/.*)?$",
      "requiresAuthentication": true
    }
  ]
}
```

Test with a JWT token:
```bash
curl -H "Authorization: Bearer {your-jwt-token}" http://localhost:5000/api/secure
```

## Common Tasks

### Check Gateway Health

```bash
curl http://localhost:5000/health -s | jq
```

### List All Routes

```bash
curl http://localhost:5000/api/gateway/routes -s | jq
```

### Monitor Metrics

```bash
curl http://localhost:5000/api/metrics -s | jq '.totalRequests'
```

### View Request Logs

Enable debug logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "DotNetApiGateway": "Debug"
    }
  }
}
```

Restart and watch for detailed logs:
```bash
dotnet run 2>&1 | grep -i "request\|error"
```

## Next Steps

After completing this guide:

1. **Read the [Configuration Reference](./configuration.md)** - Learn all available options
2. **Review [Usage Examples](../README.md#usage-examples)** - See practical implementations
3. **Explore [Deployment Guide](./deployment.md)** - Deploy to production
4. **Check [Architecture](./architecture.md)** - Understand internal design
5. **Visit [FAQ](./faq.md)** - Find answers to common questions

## Troubleshooting

### Gateway Won't Start

**Error**: `Address already in use`

```bash
# Change port in appsettings.json
# Or find process using port 5000:
lsof -i :5000
kill -9 <PID>
```

### Routes Not Working

**Error**: 404 Not Found

- Check route pattern matches request path
- Verify backend URL is correct
- Test backend directly: `curl http://backend:3000/path`

### Certificate Errors (HTTPS)

**Error**: `File not found` for certificate

```bash
# Generate self-signed certificate for testing
dotnet dev-certs https --trust
```

### Memory Issues

**Symptom**: Gateway becomes slow over time

- Reduce cache TTL in configuration
- Check available disk space
- Monitor with: `dotnet trace` or task manager

## Getting Help

- **Documentation**: See `/docs` directory
- **Issues**: GitHub Issues on the repository
- **Discussion**: GitHub Discussions for questions
- **Examples**: Check `/examples` directory for working samples

## Next Steps

1. Configure additional routes for your services
2. Enable authentication and rate limiting
3. Set up monitoring and health checks
4. Deploy to your infrastructure (Docker, Kubernetes, etc.)

For detailed deployment instructions, see [Deployment Guide](./deployment.md).
