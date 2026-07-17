#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetApiGateway.Models;

/// <summary>
/// Provides validation helpers for <see cref="CircuitBreakerPolicy"/> instances
/// </summary>
public static class CircuitBreakerPolicyValidation
{
    /// <summary>
    /// Validates a <see cref="CircuitBreakerPolicy"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="policy">The policy to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable error messages</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null</exception>
    public static IReadOnlyList<string> ValidatePolicy(this CircuitBreakerPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(policy.Id))
            errors.Add("Id cannot be null or whitespace");

        if (policy.FailureThreshold < 1)
            errors.Add("FailureThreshold must be at least 1");

        if (policy.SuccessThreshold < 1)
            errors.Add("SuccessThreshold must be at least 1");

        if (policy.TimeoutSeconds < 10 || policy.TimeoutSeconds > 600)
            errors.Add("TimeoutSeconds must be between 10 and 600");

        if (policy.MaxRetries < 0 || policy.MaxRetries > 10)
            errors.Add("MaxRetries must be between 0 and 10");

        if (policy.RetryDelayMilliseconds < 10 || policy.RetryDelayMilliseconds > 5000)
            errors.Add("RetryDelayMilliseconds must be between 10 and 5000");

        if (policy.FailureStatusCodes is null || policy.FailureStatusCodes.Length == 0)
            errors.Add("FailureStatusCodes cannot be null or empty");
        else
        {
            foreach (var code in policy.FailureStatusCodes)
            {
                if (code < 100 || code > 999)
                    errors.Add($"FailureStatusCode {code} is not a valid HTTP status code (must be 3-digit)");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="CircuitBreakerPolicy"/> instance is valid.
    /// </summary>
    /// <param name="policy">The policy to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null</exception>
    public static bool IsPolicyValid(this CircuitBreakerPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.ValidatePolicy().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="CircuitBreakerPolicy"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with detailed error messages if it is not.
    /// </summary>
    /// <param name="policy">The policy to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="policy"/> is not valid, with detailed error messages</exception>
    public static void EnsurePolicyValid(this CircuitBreakerPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var errors = policy.ValidatePolicy();

        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"CircuitBreakerPolicy validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }
}