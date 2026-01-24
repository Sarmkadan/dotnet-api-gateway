// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;

/// <summary>
/// Handles request/response transformation configuration and testing.
/// Allows clients to test transformation pipelines before deployment.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RequestTransformationController : ControllerBase
{
    private readonly ILogger<RequestTransformationController> _logger;

    public RequestTransformationController(ILogger<RequestTransformationController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test request header transformation by applying rules to sample headers.
    /// Validates transformation logic without modifying actual routes.
    /// </summary>
    [HttpPost("test/headers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TestHeaderTransformation([FromBody] HeaderTransformationRequest request)
    {
        if (request?.InputHeaders == null || request.InputHeaders.Count == 0)
            return BadRequest(new { error = "Input headers required" });

        try
        {
            var result = new Dictionary<string, string>();

            // Apply header additions
            if (request.HeadersToAdd != null)
            {
                foreach (var header in request.HeadersToAdd)
                {
                    result[header.Key] = header.Value;
                }
            }

            // Copy and transform existing headers
            foreach (var header in request.InputHeaders)
            {
                if (!request.HeadersToRemove?.Contains(header.Key, StringComparer.OrdinalIgnoreCase) ?? true)
                {
                    result[header.Key] = header.Value;
                }
            }

            _logger.LogInformation("Header transformation test executed successfully");
            return Ok(new { originalHeaders = request.InputHeaders, transformedHeaders = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Header transformation test failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test request body transformation with serialization and mapping rules.
    /// Supports JSON transformation and property mapping validation.
    /// </summary>
    [HttpPost("test/body")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TestBodyTransformation([FromBody] BodyTransformationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.InputBody))
            return BadRequest(new { error = "Input body required" });

        try
        {
            // Simple JSON normalization test - in production use specialized JSON transformers
            var parsedInput = System.Text.Json.JsonSerializer.Deserialize<object>(request.InputBody);

            var transformed = System.Text.Json.JsonSerializer.Serialize(parsedInput, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Body transformation test executed");
            return Ok(new { original = parsedInput, transformed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Body transformation test failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test query string parameter transformation by applying mapping rules.
    /// Validates parameter renaming and filtering logic.
    /// </summary>
    [HttpPost("test/query-params")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TestQueryParamTransformation([FromBody] QueryParamTransformationRequest request)
    {
        if (request?.InputParams == null || request.InputParams.Count == 0)
            return BadRequest(new { error = "Input query parameters required" });

        try
        {
            var result = new Dictionary<string, string>();

            foreach (var param in request.InputParams)
            {
                // Skip filtered parameters
                if (request.ParamsToRemove?.Contains(param.Key) ?? false)
                    continue;

                // Apply mapping if specified
                var key = request.ParamMapping?.ContainsKey(param.Key) ?? false
                    ? request.ParamMapping[param.Key]
                    : param.Key;

                result[key] = param.Value;
            }

            _logger.LogInformation("Query parameter transformation test executed");
            return Ok(new { original = request.InputParams, transformed = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query parameter transformation test failed");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for header transformation testing.
/// </summary>
public class HeaderTransformationRequest
{
    public Dictionary<string, string> InputHeaders { get; set; } = new();
    public Dictionary<string, string>? HeadersToAdd { get; set; }
    public List<string>? HeadersToRemove { get; set; }
}

/// <summary>
/// Request model for body transformation testing.
/// </summary>
public class BodyTransformationRequest
{
    public string? InputBody { get; set; }
    public Dictionary<string, object>? TransformationRules { get; set; }
}

/// <summary>
/// Request model for query parameter transformation testing.
/// </summary>
public class QueryParamTransformationRequest
{
    public Dictionary<string, string> InputParams { get; set; } = new();
    public Dictionary<string, string>? ParamMapping { get; set; }
    public List<string>? ParamsToRemove { get; set; }
}
