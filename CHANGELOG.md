# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added

- **Distributed Caching**: Redis integration for multi-instance deployments
- **Webhook Management**: Webhook registration and delivery system
- **Request Transformation**: Header manipulation and request body transformation policies
- **Metrics Export**: Prometheus endpoint for metrics collection
- **Health Check Enhancements**: Per-service health monitoring with response times
- **Performance Analytics**: Real-time performance analysis and bottleneck detection
- **Request Aggregation**: Combine multiple backend requests into single response
- **CLI Configuration**: Command-line interface for gateway management
- **API Documentation**: Comprehensive OpenAPI/Swagger documentation

### Changed

- **Configuration Format**: Extended configuration schema for new policies
- **Rate Limiting Algorithm**: Improved token bucket with burst support
- **Circuit Breaker**: Enhanced state machine with half-open improvements
- **Logging**: Structured logging with Serilog integration
- **Authentication**: Support for multiple authentication methods (JWT, API Key)

### Fixed

- Rate limiting edge case with concurrent requests
- Circuit breaker state transition race condition
- Cache invalidation timing issues
- CORS header handling for preflight requests
- Request timeout handling in gateway middleware

### Deprecated

- Legacy configuration format (will be removed in v2.0)
- `StaticConfiguration` attribute (use dependency injection instead)

### Security

- Added HTTPS/TLS support with certificate management
- Implemented request size limits to prevent attacks
- Added IP whitelist/blacklist functionality
- Enhanced JWT validation with configurable claims

## [1.1.0] - 2026-04-15

### Added

- **Rate Limiting**: Token bucket algorithm for per-client request limiting
- **Circuit Breaker Pattern**: Fault tolerance with automatic recovery
- **Response Caching**: In-memory caching with TTL expiration
- **Load Balancing**: Weighted round-robin distribution across multiple targets
- **JWT Validation**: Token verification with role-based access control
- **Health Monitoring**: Background health checks for backend services
- **Metrics Collection**: Request metrics, latency tracking, error rates
- **Request Logging**: Complete request/response logging with timestamps
- **Error Handling**: Comprehensive exception handling and error responses
- **CORS Support**: Configurable cross-origin resource sharing

### Changed

- Improved request routing performance with compiled regex
- Enhanced middleware pipeline for better request processing
- Optimized memory usage in cache service
- Better error messages with detailed context

### Fixed

- Route pattern matching edge cases
- Request body size handling
- Header propagation in request forwarding
- Graceful shutdown handling

## [1.0.0] - 2026-03-01

### Added

- **Core Gateway Functionality**: HTTP request routing and forwarding
- **Route Management**: Dynamic route configuration via configuration file
- **Request Routing**: Pattern-based routing with wildcard support
- **Multiple Targets**: Load distribution across multiple backend services
- **Middleware Pipeline**: Request/response processing pipeline
- **Dependency Injection**: Service configuration via built-in DI container
- **Configuration Management**: JSON-based configuration with environment support
- **Logging**: Console and file-based logging infrastructure
- **Exception Handling**: Custom exception types for specific error scenarios
- **Repository Pattern**: In-memory data access layer abstraction

### Security

- Initial security review and hardening
- Input validation for route patterns
- Request size limits
- Basic authentication framework

---

## Upgrade Guide

### Upgrading from 1.0.0 to 1.1.0

1. Update configuration to include new policies:
   ```json
   {
     "rateLimitPolicy": { "enabled": true, "requestsPerMinute": 100 },
     "circuitBreakerPolicy": { "enabled": true, "failureThreshold": 5 }
   }
   ```

2. Update route targets to include health check URLs:
   ```json
   {
     "url": "http://backend:3000",
     "healthCheckUrl": "http://backend:3000/health"
   }
   ```

### Upgrading from 1.1.0 to 1.2.0

1. Update to .NET 10 SDK (required)

2. Enable metrics endpoint:
   ```json
   {
     "GatewayConfiguration": {
       "EnableMetrics": true,
       "EnableRequestLogging": true
     }
   }
   ```

3. Configure Redis for distributed caching (optional):
   ```csharp
   services.AddStackExchangeRedisCache(options => {
       options.Configuration = "redis-server:6379";
   });
   ```

4. Register webhooks for event notifications:
   ```bash
   curl -X POST http://localhost:5000/api/webhooks/register \
     -H "Content-Type: application/json" \
     -d '{ "url": "https://myapp.com/webhook", "events": ["route-created"] }'
   ```

---

## Known Issues

### v1.2.0

- Distributed caching with Redis may have slight synchronization delays
- WebSocket connections not supported (planned for v1.3)
- GraphQL federation not yet implemented

### v1.1.0

- Circuit breaker state machine may race under extreme concurrency
- Cache TTL expiration is approximate (within 1-5 seconds)

---

## Future Roadmap

### v1.3.0 (Q2 2026)

- WebSocket support
- GraphQL federation
- Distributed request tracking
- Advanced authorization policies
- Custom rate limiting algorithms

### v1.4.0 (Q3 2026)

- HTTP/3 QUIC support
- Rate limiting by custom attributes
- Request/response streaming
- Service mesh integration

### v2.0.0 (Q4 2026)

- Complete rewrite with async first design
- Native gRPC support
- Breaking changes to configuration format
- Enhanced performance (target: 50k req/sec per instance)

---

## Contributing

We welcome contributions! See CONTRIBUTING.md for guidelines.

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

See LICENSE file for full details.
