#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

/// <summary>
/// Configuration settings for the API gateway
/// </summary>
public sealed class GatewayConfiguration
{
    public string ApplicationName { get; set; } = "DotNetApiGateway";
    public string Version { get; set; } = "1.0.0";
    public int MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10 MB
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 100;
    public bool EnableCors { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthCheck { get; set; } = true;
    public string HealthCheckPath { get; set; } = "/health";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApplicationName))
            throw new ArgumentException("ApplicationName cannot be empty");

        if (MaxRequestBodySize < 1024)
            throw new ArgumentException("MaxRequestBodySize must be at least 1 KB");

        if (DefaultTimeoutSeconds < 1 || DefaultTimeoutSeconds > 300)
            throw new ArgumentException("DefaultTimeoutSeconds must be between 1 and 300");

        if (MaxConcurrentRequests < 1)
            throw new ArgumentException("MaxConcurrentRequests must be at least 1");
    }
}
