// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Formatters;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Formatter for consistent JSON response formatting across the gateway.
/// Provides standard structure for success and error responses.
/// </summary>
public static class JsonResponseFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = new List<JsonConverter> { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Format successful response with data.
    /// </summary>
    public static string FormatSuccess<T>(T data, string? message = null) where T : class
    {
        var response = new SuccessResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation successful",
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Format successful response with message only (no data).
    /// </summary>
    public static string FormatSuccess(string message = "Operation successful")
    {
        var response = new SuccessResponse<object>
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Format error response with error code and message.
    /// </summary>
    public static string FormatError(string errorCode, string message, int statusCode = 400, Dictionary<string, object>? details = null)
    {
        var response = new ErrorResponse
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            StatusCode = statusCode,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Format validation error response with field-specific errors.
    /// </summary>
    public static string FormatValidationError(Dictionary<string, string> fieldErrors, string message = "Validation failed")
    {
        var details = fieldErrors.ToDictionary<KeyValuePair<string, string>, string, object>(
            kvp => kvp.Key,
            kvp => (object)kvp.Value
        );

        return FormatError("VALIDATION_ERROR", message, 400, details);
    }

    /// <summary>
    /// Format paginated response with data and pagination metadata.
    /// </summary>
    public static string FormatPaginated<T>(List<T> items, int pageNumber, int pageSize, long totalCount) where T : class
    {
        var response = new PaginatedResponse<T>
        {
            Success = true,
            Data = items,
            Pagination = new PaginationMetadata
            {
                Page = pageNumber,
                PageSize = pageSize,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            },
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Format raw JSON bytes response.
    /// </summary>
    public static byte[] FormatBytes<T>(T data) where T : class
    {
        var json = JsonSerializer.Serialize(data, Options);
        return Encoding.UTF8.GetBytes(json);
    }
}

/// <summary>
/// Standard success response envelope.
/// </summary>
public class SuccessResponse<T> where T : class
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Standard error response envelope.
/// </summary>
public class ErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "UNKNOWN_ERROR";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "An error occurred";

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 400;

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Paginated response envelope with metadata.
/// </summary>
public class PaginatedResponse<T> where T : class
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationMetadata? Pagination { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Pagination metadata.
/// </summary>
public class PaginationMetadata
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
