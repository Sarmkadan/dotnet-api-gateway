#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetApiGateway.Utilities;
using FluentAssertions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Extension methods for <see cref="ValidationUtilityTests"/> that provide additional
/// validation utilities and fluent assertion helpers for testing validation scenarios.
/// </summary>
public static class ValidationUtilityTestsExtensions
{
	/// <summary>
	/// Validates that a collection contains only valid email addresses.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="emails">The collection of email addresses to validate.</param>
	/// <param name="expectedInvalidCount">The expected number of invalid emails in the collection.</param>
	/// <exception cref="ArgumentNullException"><paramref name="emails"/> is null.</exception>
	public static void AllEmailsValid(
		this ValidationUtilityTests test,
		[NotNull] IEnumerable<string> emails,
		int expectedInvalidCount = 0)
	{
		ArgumentNullException.ThrowIfNull(emails);

		var invalidEmails = new List<string>();
		var index = 0;

		foreach (var email in emails)
		{
			if (!ValidationUtility.IsValidEmail(email))
			{
				invalidEmails.Add($"Index {index}: '{email}'");
			}
			index++;
		}

		invalidEmails.Should().HaveCount(expectedInvalidCount,
			$"Expected {expectedInvalidCount} invalid emails but found {invalidEmails.Count}. Invalid emails: {string.Join(", ", invalidEmails)}");
	}

	/// <summary>
	/// Validates that a collection contains only valid URLs.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="urls">The collection of URLs to validate.</param>
	/// <param name="expectedInvalidCount">The expected number of invalid URLs in the collection.</param>
	/// <exception cref="ArgumentNullException"><paramref name="urls"/> is null.</exception>
	public static void AllUrlsValid(
		this ValidationUtilityTests test,
		[NotNull] IEnumerable<string> urls,
		int expectedInvalidCount = 0)
	{
		ArgumentNullException.ThrowIfNull(urls);

		var invalidUrls = new List<string>();
		var index = 0;

		foreach (var url in urls)
		{
			if (!ValidationUtility.IsValidUrl(url))
			{
				invalidUrls.Add($"Index {index}: '{url}'");
			}
			index++;
		}

		invalidUrls.Should().HaveCount(expectedInvalidCount,
			$"Expected {expectedInvalidCount} invalid URLs but found {invalidUrls.Count}. Invalid URLs: {string.Join(", ", invalidUrls)}");
	}

	/// <summary>
	/// Validates that a collection of key-value pairs contains all required keys.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="dictionary">The dictionary to validate.</param>
	/// <param name="requiredKeys">The required keys that must be present.</param>
	/// <returns>True if all required keys are present; otherwise false.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="requiredKeys"/> is null.</exception>
	public static bool HasAllRequiredKeys(
		this ValidationUtilityTests test,
		[NotNull] Dictionary<string, string?> dictionary,
		[NotNull] params string[] requiredKeys)
	{
		ArgumentNullException.ThrowIfNull(dictionary);
		ArgumentNullException.ThrowIfNull(requiredKeys);

		return ValidationUtility.HasRequiredKeys(dictionary, requiredKeys);
	}

	/// <summary>
	/// Validates that a collection of strings are all alphanumeric.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="values">The collection of strings to validate.</param>
	/// <param name="expectedInvalidCount">The expected number of invalid alphanumeric strings.</param>
	/// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
	public static void AllAlphanumeric(
		this ValidationUtilityTests test,
		[NotNull] IEnumerable<string> values,
		int expectedInvalidCount = 0)
	{
		ArgumentNullException.ThrowIfNull(values);

		var invalidValues = new List<string>();
		var index = 0;

		foreach (var value in values)
		{
			if (!ValidationUtility.IsAlphanumeric(value))
			{
				invalidValues.Add($"Index {index}: '{value}'");
			}
			index++;
		}

		invalidValues.Should().HaveCount(expectedInvalidCount,
			$"Expected {expectedInvalidCount} invalid alphanumeric strings but found {invalidValues.Count}. Invalid values: {string.Join(", ", invalidValues)}");
	}
}
