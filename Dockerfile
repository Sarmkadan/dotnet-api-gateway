# =============================================================================
# Multi-stage Dockerfile for DotNet API Gateway
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY DotNetApiGateway.csproj .
RUN dotnet restore "DotNetApiGateway.csproj"

# Copy source code
COPY . .

# Build the application in Release mode
RUN dotnet build "DotNetApiGateway.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "DotNetApiGateway.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnetcore:10.0
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Create non-root user for security
RUN useradd -m -u 1000 gateway && chown -R gateway:gateway /app
USER gateway

# Expose ports
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "DotNetApiGateway.dll"]
