#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Utilities;
using System.Globalization;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Validation helpers for URL utility test data to ensure test values meet expected constraints.
/// Provides comprehensive validation of test data against the actual <see cref="UrlUtility"/> behavior.
/// </summary>
public static class UrlUtilityTestsValidation
{
    /// <summary>
    /// Validates URL test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="baseUrl">The base URL to validate.</param>
    /// <param name="path">The path to validate.</param>
    /// <param name="queryString">The query string to validate.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateCombineUrl(
        string? baseUrl,
        string? path,
        string? queryString)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            problems.Add("BaseUrl is null, empty, or whitespace");
        }
        else if (!UrlUtility.IsValidUrl(baseUrl))
        {
            problems.Add("BaseUrl is not a valid URL");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            problems.Add("Path is null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(queryString))
        {
            problems.Add("QueryString is null, empty, or whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates URL test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="expectedIsValid">Expected result of <see cref="UrlUtility.IsValidUrl"/>.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateIsValidUrl(string? url, bool expectedIsValid)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            problems.Add("Url is null, empty, or whitespace");
        }
        else if (UrlUtility.IsValidUrl(url) != expectedIsValid)
        {
            problems.Add(expectedIsValid
                ? "Url is valid but expected to be invalid"
                : "Url is invalid but expected to be valid");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates hostname extraction test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="expectedHostname">Expected hostname result.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateGetHostname(string? url, string? expectedHostname)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            if (expectedHostname is not null)
            {
                problems.Add("Url is null/empty but expectedHostname is not null");
            }
        }
        else
        {
            var actualHostname = UrlUtility.GetHostname(url);
            if (actualHostname != expectedHostname)
            {
                problems.Add(
                    $"GetHostname({url}) returned '{actualHostname}' but expected '{expectedHostname}'");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates port extraction test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="expectedPort">Expected port result.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateGetPort(string? url, int expectedPort)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            problems.Add("Url is null, empty, or whitespace");
        }
        else
        {
            var actualPort = UrlUtility.GetPort(url);
            if (actualPort != expectedPort)
            {
                problems.Add(
                    $"GetPort({url}) returned {actualPort} but expected {expectedPort}");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates query parameter presence test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="paramName">The parameter name to check.</param>
    /// <param name="expectedHasParam">Expected result of <see cref="UrlUtility.HasQueryParameter"/>.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateHasQueryParameter(
        string? url,
        string? paramName,
        bool expectedHasParam)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            problems.Add("Url is null, empty, or whitespace");
        }
        else if (string.IsNullOrWhiteSpace(paramName))
        {
            problems.Add("Parameter name is null, empty, or whitespace");
        }
        else
        {
            var actualHasParam = UrlUtility.HasQueryParameter(url, paramName);
            if (actualHasParam != expectedHasParam)
            {
                problems.Add(expectedHasParam
                    ? $"UrlUtility.HasQueryParameter({url}, {paramName}) returned false but expected true"
                    : $"UrlUtility.HasQueryParameter({url}, {paramName}) returned true but expected false");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates URL sanitization test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="expectedSanitized">Expected sanitized URL.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateSanitizeUrl(string? url, string? expectedSanitized)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            problems.Add("Url is null, empty, or whitespace");
        }
        else if (string.IsNullOrWhiteSpace(expectedSanitized))
        {
            problems.Add("Expected sanitized URL is null, empty, or whitespace");
        }
        else
        {
            var actualSanitized = UrlUtility.SanitizeUrl(url);
            if (actualSanitized != expectedSanitized)
            {
                problems.Add(
                    $"SanitizeUrl({url}) returned '{actualSanitized}' but expected '{expectedSanitized}'");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates URL combination test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="path">The path to combine.</param>
    /// <param name="expectedCombined">Expected combined URL result.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateCombineUrlResult(
        string? baseUrl,
        string? path,
        string? expectedCombined)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(expectedCombined))
        {
            problems.Add("Expected combined URL is null, empty, or whitespace");
        }
        else if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
        {
            // One of the inputs is invalid, so we can't validate the output
            // The test itself should handle this case
        }
        else
        {
            var actualCombined = UrlUtility.CombineUrl(baseUrl, path);
            if (actualCombined != expectedCombined)
            {
                problems.Add(
                    $"CombineUrl({baseUrl}, {path}) returned '{actualCombined}' but expected '{expectedCombined}'");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates query string parsing test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <param name="expectedParameters">Expected parsed parameters.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateParseQueryString(
        string? queryString,
        IReadOnlyDictionary<string, string> expectedParameters)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(queryString) && expectedParameters.Count > 0)
        {
            problems.Add("QueryString is null/empty but expectedParameters has values");
        }
        else if (!string.IsNullOrWhiteSpace(queryString))
        {
            var actualParameters = UrlUtility.ParseQueryString(queryString);
            if (actualParameters.Count != expectedParameters.Count)
            {
                problems.Add(
                    $"ParseQueryString returned {actualParameters.Count} parameters but expected {expectedParameters.Count}");
            }
            else
            {
                foreach (var kvp in expectedParameters)
                {
                    if (!actualParameters.TryGetValue(kvp.Key, out var actualValue))
                    {
                        problems.Add($"Missing parameter: {kvp.Key}");
                    }
                    else if (actualValue != kvp.Value)
                    {
                        problems.Add(
                            $"Parameter '{kvp.Key}' has value '{actualValue}' but expected '{kvp.Value}'");
                    }
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates query string building test data and returns a list of human-readable problems.
    /// </summary>
    /// <param name="parameters">The parameters to build.</param>
    /// <param name="expectedQueryString">Expected built query string.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateBuildQueryString(
        IReadOnlyDictionary<string, string>? parameters,
        string? expectedQueryString)
    {
        var problems = new List<string>();

        if (parameters is null || parameters.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(expectedQueryString) && expectedQueryString != "?")
            {
                problems.Add("Parameters is null/empty but expectedQueryString is not empty");
            }
        }
        else
        {
            var actualQueryString = UrlUtility.BuildQueryString(new Dictionary<string, string>(parameters));
            if (actualQueryString != expectedQueryString)
            {
                problems.Add(
                    $"BuildQueryString returned '{actualQueryString}' but expected '{expectedQueryString}'");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified URL test data is valid.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="path">The path.</param>
    /// <param name="queryString">The query string.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidCombineUrl(
        string? baseUrl,
        string? path,
        string? queryString)
    {
        return ValidateCombineUrl(baseUrl, path, queryString).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified URL test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedIsValid">Expected validity.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIsValidUrl(string? url, bool expectedIsValid)
    {
        return ValidateIsValidUrl(url, expectedIsValid).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified hostname extraction test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedHostname">Expected hostname.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidGetHostname(string? url, string? expectedHostname)
    {
        return ValidateGetHostname(url, expectedHostname).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified port extraction test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedPort">Expected port.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidGetPort(string? url, int expectedPort)
    {
        return ValidateGetPort(url, expectedPort).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified query parameter test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="expectedHasParam">Expected result.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidHasQueryParameter(
        string? url,
        string? paramName,
        bool expectedHasParam)
    {
        return ValidateHasQueryParameter(url, paramName, expectedHasParam).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified URL sanitization test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedSanitized">Expected sanitized URL.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidSanitizeUrl(string? url, string? expectedSanitized)
    {
        return ValidateSanitizeUrl(url, expectedSanitized).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified URL combination test data is valid.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="path">The path.</param>
    /// <param name="expectedCombined">Expected combined URL.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidCombineUrlResult(
        string? baseUrl,
        string? path,
        string? expectedCombined)
    {
        return ValidateCombineUrlResult(baseUrl, path, expectedCombined).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified query string parsing test data is valid.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    /// <param name="expectedParameters">Expected parameters.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidParseQueryString(
        string? queryString,
        IReadOnlyDictionary<string, string> expectedParameters)
    {
        return ValidateParseQueryString(queryString, expectedParameters).Count == 0;
    }

    /// <summary>
    /// Determines whether the specified query string building test data is valid.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="expectedQueryString">Expected query string.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidBuildQueryString(
        IReadOnlyDictionary<string, string>? parameters,
        string? expectedQueryString)
    {
        return ValidateBuildQueryString(parameters, expectedQueryString).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified URL test data is valid.
    /// Throws an <see cref="ArgumentException"/> with a detailed message listing all validation problems.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="path">The path.</param>
    /// <param name="queryString">The query string.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidCombineUrl(
        string? baseUrl,
        string? path,
        string? queryString)
    {
        var problems = ValidateCombineUrl(baseUrl, path, queryString);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"URL test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified URL test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedIsValid">Expected validity.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidIsValidUrl(string? url, bool expectedIsValid)
    {
        var problems = ValidateIsValidUrl(url, expectedIsValid);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"URL test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified hostname extraction test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedHostname">Expected hostname.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidGetHostname(string? url, string? expectedHostname)
    {
        var problems = ValidateGetHostname(url, expectedHostname);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Hostname extraction test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified port extraction test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedPort">Expected port.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidGetPort(string? url, int expectedPort)
    {
        var problems = ValidateGetPort(url, expectedPort);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Port extraction test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified query parameter test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="expectedHasParam">Expected result.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidHasQueryParameter(
        string? url,
        string? paramName,
        bool expectedHasParam)
    {
        var problems = ValidateHasQueryParameter(url, paramName, expectedHasParam);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Query parameter test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified URL sanitization test data is valid.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="expectedSanitized">Expected sanitized URL.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidSanitizeUrl(string? url, string? expectedSanitized)
    {
        var problems = ValidateSanitizeUrl(url, expectedSanitized);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"URL sanitization test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified URL combination test data is valid.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="path">The path.</param>
    /// <param name="expectedCombined">Expected combined URL.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidCombineUrlResult(
        string? baseUrl,
        string? path,
        string? expectedCombined)
    {
        var problems = ValidateCombineUrlResult(baseUrl, path, expectedCombined);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"URL combination test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified query string parsing test data is valid.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    /// <param name="expectedParameters">Expected parameters.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidParseQueryString(
        string? queryString,
        IReadOnlyDictionary<string, string> expectedParameters)
    {
        var problems = ValidateParseQueryString(queryString, expectedParameters);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Query string parsing test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified query string building test data is valid.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="expectedQueryString">Expected query string.</param>
    /// <exception cref="ArgumentException">Thrown if the data is not valid.</exception>
    public static void EnsureValidBuildQueryString(
        IReadOnlyDictionary<string, string>? parameters,
        string? expectedQueryString)
    {
        var problems = ValidateBuildQueryString(parameters, expectedQueryString);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Query string building test data is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}