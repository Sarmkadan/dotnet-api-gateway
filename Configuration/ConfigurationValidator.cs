#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using DotNetApiGateway.Models;
using DotNetApiGateway.Utilities;

/// <summary>
/// Validator for gateway configuration and route definitions.
/// Ensures all routes and policies have valid configurations before deployment.
/// </summary>
public sealed class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate gateway configuration.
    /// </summary>
    public ValidationResult ValidateGatewayConfig(DotnetApiGatewayOptions config)
    {
        var result = new ValidationResult();

        if (config is null)
        {
            result.AddError("Gateway configuration is null");
            return result;
        }

        // Additional complex validation can be added here if needed,
        // though most simple validations are handled by DataAnnotations.

        return result;
    }

    /// <summary>
    /// Validate route configuration.
    /// </summary>
    public ValidationResult ValidateRoute(GatewayRoute route)
    {
        var result = new ValidationResult();

        if (route is null)
        {
            result.AddError("Route is null");
            return result;
        }

        if (string.IsNullOrWhiteSpace(route.Id))
            result.AddError("Route ID is required");

        if (string.IsNullOrWhiteSpace(route.Name))
            result.AddError("Route name is required");

        if (string.IsNullOrWhiteSpace(route.PathPattern))
            result.AddError("Route path pattern is required");

        if (route.Targets is null || route.Targets.Length == 0)
            result.AddError("Route must have at least one target");
        else
        {
            foreach (var target in route.Targets)
            {
                var targetResult = ValidateRouteTarget(target);
                if (!targetResult.IsValid)
                    result.Errors.AddRange(targetResult.Errors);
            }
        }

        // Validate HTTP methods
        if (route.AllowedMethods is not null && route.AllowedMethods.Length > 0)
        {
            foreach (var method in route.AllowedMethods)
            {
                if (!ValidationUtility.IsValidHttpMethod(method))
                    result.AddError($"Invalid HTTP method: {method}");
            }
        }

        // Validate policies
        if (route.RateLimitPolicy is not null)
        {
            var policyResult = ValidateRateLimitPolicy(route.RateLimitPolicy);
            if (!policyResult.IsValid)
                result.Errors.AddRange(policyResult.Errors);
        }

        if (route.CircuitBreakerPolicy is not null)
        {
            var policyResult = ValidateCircuitBreakerPolicy(route.CircuitBreakerPolicy);
            if (!policyResult.IsValid)
                result.Errors.AddRange(policyResult.Errors);
        }

        return result;
    }

    /// <summary>
    /// Validate route target configuration.
    /// </summary>
    public ValidationResult ValidateRouteTarget(RouteTarget target)
    {
        var result = new ValidationResult();

        if (target is null)
        {
            result.AddError("Route target is null");
            return result;
        }

        if (string.IsNullOrWhiteSpace(target.Name))
            result.AddError("Target name is required");

        if (!ValidationUtility.IsValidUrl(target.BaseUrl))
            result.AddError($"Target base URL is invalid: {target.BaseUrl}");

        if (target.Port.HasValue && !ValidationUtility.IsValidPort(target.Port.Value))
            result.AddError($"Target port is invalid: {target.Port}");

        if (target.Weight < 0 || target.Weight > 100)
            result.AddError("Target weight must be between 0 and 100");

        return result;
    }

    /// <summary>
    /// Validate rate limit policy configuration.
    /// </summary>
    public ValidationResult ValidateRateLimitPolicy(RateLimitPolicy policy)
    {
        var result = new ValidationResult();

        if (policy is null)
        {
            result.AddError("Rate limit policy is null");
            return result;
        }

        if (policy.RequestsPerMinute <= 0 && policy.RequestsPerHour <= 0)
            result.AddError("Rate limit policy must specify either RequestsPerMinute or RequestsPerHour");

        if (policy.BurstSize < 1)
            result.AddError("Burst size must be at least 1");

        return result;
    }

    /// <summary>
    /// Validate circuit breaker policy configuration.
    /// </summary>
    public ValidationResult ValidateCircuitBreakerPolicy(CircuitBreakerPolicy policy)
    {
        var result = new ValidationResult();

        if (policy is null)
        {
            result.AddError("Circuit breaker policy is null");
            return result;
        }

        if (policy.FailureThreshold <= 0)
            result.AddError("Circuit breaker failure threshold must be greater than 0");

        if (policy.TimeoutSeconds <= 0)
            result.AddError("Circuit breaker timeout must be greater than 0");

        if (policy.SuccessThreshold < 1)
            result.AddError("Circuit breaker success threshold must be at least 1");

        return result;
    }

    /// <summary>
    /// Validate cache policy configuration.
    /// </summary>
    public ValidationResult ValidateCachePolicy(CachePolicy policy)
    {
        var result = new ValidationResult();

        if (policy is null)
        {
            result.AddError("Cache policy is null");
            return result;
        }

        if (policy.DurationSeconds <= 0)
            result.AddError("Cache TTL must be greater than 0");

        return result;
    }
}

/// <summary>
/// Result of configuration validation.
/// </summary>
public sealed class ValidationResult
{
    public List<string> Errors { get; } = new();

    public bool IsValid => Errors.Count == 0;

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            Errors.Add(error);
    }

    public string GetErrorSummary()
    {
        if (IsValid)
            return "Configuration is valid";

        return $"Configuration has {Errors.Count} error(s):\n" + string.Join("\n- ", Errors.Select(e => $"- {e}"));
    }
}
