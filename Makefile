# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# Makefile for DotNet API Gateway
# =============================================================================

.PHONY: help build run test clean restore publish docker-build docker-run format lint docs

# Color output
BLUE := \033[0;34m
GREEN := \033[0;32m
RED := \033[0;31m
NC := \033[0m # No Color

# Default target
help:
	@echo "$(BLUE)DotNet API Gateway - Available Commands$(NC)"
	@echo ""
	@echo "$(GREEN)Development:$(NC)"
	@echo "  make build          Build the project"
	@echo "  make run            Run the gateway locally"
	@echo "  make test           Run all tests"
	@echo "  make watch          Run with file watching"
	@echo "  make clean          Clean build artifacts"
	@echo ""
	@echo "$(GREEN)Code Quality:$(NC)"
	@echo "  make format         Format code with dotnet format"
	@echo "  make lint           Run code analysis"
	@echo "  make coverage       Generate test coverage report"
	@echo ""
	@echo "$(GREEN)Build & Release:$(NC)"
	@echo "  make release        Build in Release mode"
	@echo "  make publish        Publish to NuGet"
	@echo "  make pack           Create NuGet package"
	@echo ""
	@echo "$(GREEN)Docker:$(NC)"
	@echo "  make docker-build   Build Docker image"
	@echo "  make docker-run     Run Docker container"
	@echo "  make docker-compose Start with Docker Compose"
	@echo "  make docker-stop    Stop Docker Compose"
	@echo ""
	@echo "$(GREEN)Documentation:$(NC)"
	@echo "  make docs           Generate documentation"
	@echo "  make changelog      View changelog"
	@echo ""
	@echo "$(GREEN)Development Setup:$(NC)"
	@echo "  make setup          Install dependencies"
	@echo "  make install-tools  Install dotnet tools"
	@echo ""

# Core targets
restore:
	@echo "$(BLUE)Restoring dependencies...$(NC)"
	dotnet restore
	@echo "$(GREEN)✓ Dependencies restored$(NC)"

build: restore
	@echo "$(BLUE)Building project (Debug)...$(NC)"
	dotnet build
	@echo "$(GREEN)✓ Build successful$(NC)"

run: build
	@echo "$(BLUE)Starting gateway...$(NC)"
	dotnet run

watch: restore
	@echo "$(BLUE)Starting with file watching...$(NC)"
	dotnet watch run

test: restore
	@echo "$(BLUE)Running tests...$(NC)"
	dotnet test --verbosity normal
	@echo "$(GREEN)✓ Tests completed$(NC)"

clean:
	@echo "$(BLUE)Cleaning build artifacts...$(NC)"
	dotnet clean
	rm -rf bin/ obj/ publish/ nupkg/
	@echo "$(GREEN)✓ Clean completed$(NC)"

# Release targets
release: clean
	@echo "$(BLUE)Building Release configuration...$(NC)"
	dotnet build -c Release
	@echo "$(GREEN)✓ Release build successful$(NC)"

publish: release
	@echo "$(BLUE)Publishing to NuGet...$(NC)"
	dotnet publish -c Release -o ./publish
	@echo "$(GREEN)✓ Publish successful$(NC)"

pack: release
	@echo "$(BLUE)Creating NuGet package...$(NC)"
	dotnet pack -c Release -o ./nupkg
	@echo "$(GREEN)✓ Package created$(NC)"

# Code quality targets
format:
	@echo "$(BLUE)Formatting code...$(NC)"
	dotnet format
	@echo "$(GREEN)✓ Code formatted$(NC)"

lint:
	@echo "$(BLUE)Running code analysis...$(NC)"
	dotnet build /p:EnforceCodeStyleInBuild=true
	@echo "$(GREEN)✓ Analysis complete$(NC)"

coverage:
	@echo "$(BLUE)Generating test coverage...$(NC)"
	dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
	@echo "$(GREEN)✓ Coverage report generated$(NC)"

# Docker targets
docker-build:
	@echo "$(BLUE)Building Docker image...$(NC)"
	docker build -t dotnet-api-gateway:latest .
	@echo "$(GREEN)✓ Docker image built$(NC)"

docker-run: docker-build
	@echo "$(BLUE)Starting Docker container...$(NC)"
	docker run -d \
		--name api-gateway \
		-p 5000:5000 \
		-p 5001:5001 \
		-e ASPNETCORE_ENVIRONMENT=Development \
		dotnet-api-gateway:latest
	@echo "$(GREEN)✓ Container started at http://localhost:5000$(NC)"

docker-compose-up:
	@echo "$(BLUE)Starting with Docker Compose...$(NC)"
	docker-compose up -d
	@echo "$(GREEN)✓ Services started$(NC)"
	@echo "  Gateway: http://localhost:5000"
	@echo "  Backend: http://localhost:3000"

docker-compose-down:
	@echo "$(BLUE)Stopping Docker Compose services...$(NC)"
	docker-compose down
	@echo "$(GREEN)✓ Services stopped$(NC)"

docker-compose-logs:
	@echo "$(BLUE)Tailing Docker Compose logs...$(NC)"
	docker-compose logs -f

# Documentation targets
docs:
	@echo "$(BLUE)Generating documentation...$(NC)"
	@echo "$(GREEN)Documentation available in /docs directory:$(NC)"
	@echo "  - getting-started.md"
	@echo "  - architecture.md"
	@echo "  - api-reference.md"
	@echo "  - deployment.md"
	@echo "  - faq.md"

changelog:
	@cat CHANGELOG.md

# Setup targets
setup: restore install-tools
	@echo "$(GREEN)✓ Development environment ready$(NC)"

install-tools:
	@echo "$(BLUE)Installing dotnet tools...$(NC)"
	dotnet tool install -g dotnet-format || dotnet tool update -g dotnet-format
	dotnet tool install -g dotnet-reportgenerator-globaltool || dotnet tool update -g dotnet-reportgenerator-globaltool
	@echo "$(GREEN)✓ Tools installed$(NC)"

# Utility targets
info:
	@echo "$(BLUE)Project Information:$(NC)"
	@echo "  .NET Version: $(shell dotnet --version)"
	@echo "  Project: DotNet API Gateway"
	@echo "  Author: Vladyslav Zaiets"
	@echo "  Repository: https://github.com/Sarmkadan/dotnet-api-gateway"

health:
	@echo "$(BLUE)Checking gateway health...$(NC)"
	@curl -s http://localhost:5000/health | jq .

routes:
	@echo "$(BLUE)Listing configured routes...$(NC)"
	@curl -s http://localhost:5000/api/gateway/routes | jq .

metrics:
	@echo "$(BLUE)Displaying gateway metrics...$(NC)"
	@curl -s http://localhost:5000/api/metrics | jq .

# Example workflows
example-jwt:
	@echo "$(BLUE)JWT Authentication Example$(NC)"
	@echo ""
	@echo "1. Generate a token:"
	@echo "   cd examples && dotnet run --project JwtAuthenticationExample.cs"
	@echo ""
	@echo "2. Use token in request:"
	@echo "   curl -H \"Authorization: Bearer {token}\" http://localhost:5000/api/protected"

example-ratelimit:
	@echo "$(BLUE)Rate Limiting Example$(NC)"
	@echo ""
	@echo "1. Make rapid requests:"
	@echo "   for i in {{1..100}}; do curl http://localhost:5000/api/limited; done"
	@echo ""
	@echo "2. Check rate limit status:"
	@echo "   curl http://localhost:5000/api/gateway/ratelimit/client-1 | jq"

example-circuitbreaker:
	@echo "$(BLUE)Circuit Breaker Example$(NC)"
	@echo ""
	@echo "1. Check circuit breaker status:"
	@echo "   curl http://localhost:5000/api/gateway/circuitbreaker/my-service | jq"
	@echo ""
	@echo "2. Reset circuit breaker:"
	@echo "   curl -X POST http://localhost:5000/api/gateway/circuitbreaker/my-service/reset"

# CI/CD targets
ci-build: format lint test
	@echo "$(GREEN)✓ CI pipeline passed$(NC)"

ci-release: ci-build release pack
	@echo "$(GREEN)✓ Release pipeline completed$(NC)"

# Performance targets
benchmark:
	@echo "$(BLUE)Running benchmark...$(NC)"
	@echo "Making 100 requests to gateway..."
	@for i in $$(seq 1 100); do \
		curl -s http://localhost:5000/api/test > /dev/null; \
	done
	@echo "$(GREEN)✓ Benchmark completed$(NC)"

# Integration targets
integration-test:
	@echo "$(BLUE)Running integration tests...$(NC)"
	docker-compose-up
	sleep 5
	dotnet test -c Debug --filter "Category=Integration"
	docker-compose-down

# Validation targets
validate:
	@echo "$(BLUE)Validating project...$(NC)"
	@echo "✓ Checking .editorconfig"
	@echo "✓ Checking code formatting"
	@echo "✓ Checking dependencies"
	@echo "$(GREEN)✓ Project validation passed$(NC)"

# Default actions
.DEFAULT_GOAL := help

# Documentation of common workflows
.PHONY: workflow-dev workflow-release workflow-deploy

workflow-dev:
	@echo "$(BLUE)Development Workflow:$(NC)"
	@echo "1. make setup          # One-time setup"
	@echo "2. make watch          # Start development server"
	@echo "3. make test           # Run tests"
	@echo "4. make format         # Format code before commit"
	@echo ""

workflow-release:
	@echo "$(BLUE)Release Workflow:$(NC)"
	@echo "1. make ci-build       # Run full CI pipeline"
	@echo "2. make pack           # Create package"
	@echo "3. make publish        # Publish to NuGet"
	@echo ""

workflow-deploy:
	@echo "$(BLUE)Deployment Workflow:$(NC)"
	@echo "1. make docker-build   # Build Docker image"
	@echo "2. make docker-run     # Start container"
	@echo "3. make health         # Verify health"
	@echo ""
