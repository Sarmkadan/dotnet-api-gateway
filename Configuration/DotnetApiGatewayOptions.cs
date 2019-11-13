#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Configuration;

using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

/// <summary>
/// Configuration settings for the API gateway
/// </summary>
public sealed class DotnetApiGatewayOptions : IValidatableObject
{
    public const string SectionName = "DotnetApiGateway";

    [Required]
    [MinLength(1)]
    public string ApplicationName { get; set; } = "DotNetApiGateway";

    [Required]
    [RegularExpression(@"^\d+\.\d+\.\d+$")]
    public string Version { get; set; } = "1.0.0";

    [Range(1024, int.MaxValue)]
    public int MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10 MB

    [Range(1, 300)]
    public int DefaultTimeoutSeconds { get; set; } = 30;

    [Range(1, int.MaxValue)]
    public int MaxConcurrentRequests { get; set; } = 100;

    public bool EnableCors { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public bool EnableLogging { get; set; } = true;

    [Required]
    public string LogLevel { get; set; } = "Information";

    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthCheck { get; set; } = true;

    [Required]
    public string HealthCheckPath { get; set; } = "/health";

    public JwtValidationOptions JwtValidation { get; set; } = new();

    public List<Models.GatewayRoute> Routes { get; set; } = new();

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext)
    {
        if (JwtValidation.Enabled)
        {
            if (string.IsNullOrWhiteSpace(JwtValidation.SecretKey) && string.IsNullOrWhiteSpace(JwtValidation.Issuer))
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("JwtValidation.SecretKey or JwtValidation.Issuer is required when JwtValidation is enabled.", new[] { nameof(JwtValidation) });
            }
        }
    }
}

public sealed class JwtValidationOptions
{
    public bool Enabled { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? SecretKey { get; set; }
}
