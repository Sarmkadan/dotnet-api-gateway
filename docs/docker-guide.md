# Docker Guide
## Quick Start with Docker
This guide provides instructions for running dotnet-api-gateway using Docker.
### Pull the Image
```bash
docker pull dotnet-api-gateway:latest
```
### Run the Container
```bash
docker run -p 8080:8080 dotnet-api-gateway:latest
```
## Docker Compose Usage
Create a `docker-compose.yml` file:
```yaml
version: '3.8'
services:
  gateway:
    image: dotnet-api-gateway:latest
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./config:/app/config
```
## Environment Variables Reference
| Variable | Description | Default |
|---------|-------------|---------|
| ASPNETCORE_ENVIRONMENT | Environment name | Production |
| GATEWAY_CONFIG_PATH | Configuration file path | /app/config |
## Production Deployment Checklist
- [ ] Set up SSL/TLS
- [ ] Configure health checks
- [ ] Set up monitoring and logging
- [ ] Configure rate limiting
- [ ] Set up authentication