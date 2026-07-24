#nullable enable

namespace DotNetApiGateway.Models;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides validation helpers for <see cref="WebhookSubscription"/> instances.
/// </summary>
[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "No static fields to initialize")]
public static class WebhookSubscriptionValidation
{
    /// <summary>
    /// Validates a webhook subscription and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The webhook subscription to validate.</param>
    /// <returns>An immutable list of validation problems; empty if the subscription is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate([NotNull] this WebhookSubscription? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>(capacity: 16);

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("Id must be a non-empty string.");
        }
        else if (!Guid.TryParse(value.Id, out _))
        {
            problems.Add("Id must be a valid GUID.");
        }

        // Validate CallbackUrl
        if (string.IsNullOrWhiteSpace(value.CallbackUrl))
        {
            problems.Add("CallbackUrl must be a non-empty string.");
        }
        else if (!Uri.TryCreate(value.CallbackUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            problems.Add("CallbackUrl must be a valid absolute HTTP or HTTPS URL.");
        }

        // Validate EventTypes
        if (value.EventTypes is null)
        {
            problems.Add("EventTypes collection must not be null.");
        }
        else if (value.EventTypes.Length == 0)
        {
            problems.Add("EventTypes must contain at least one event type.");
        }
        else
        {
            for (var i = 0; i < value.EventTypes.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(value.EventTypes[i]))
                {
                    problems.Add($"EventTypes[{i}] must be a non-empty string.");
                }
            }
        }

        // Validate CurrentSecret
        if (string.IsNullOrWhiteSpace(value.CurrentSecret))
        {
            problems.Add("CurrentSecret must be a non-empty string.");
        }
        else if (value.CurrentSecret.Length < 16)
        {
            problems.Add("CurrentSecret must be at least 16 characters long.");
        }

        // Validate RetryPolicy
        if (value.RetryPolicy is null)
        {
            problems.Add("RetryPolicy must not be null.");
        }
        else
        {
            if (value.RetryPolicy.MaxRetries < 0)
            {
                problems.Add("RetryPolicy.MaxRetries must be a non-negative integer.");
            }

            if (value.RetryPolicy.InitialDelayMs <= 0)
            {
                problems.Add("RetryPolicy.InitialDelayMs must be a positive integer.");
            }

            if (value.RetryPolicy.MaxDelayMs <= 0)
            {
                problems.Add("RetryPolicy.MaxDelayMs must be a positive integer.");
            }

            if (value.RetryPolicy.MaxDelayMs < value.RetryPolicy.InitialDelayMs)
            {
                problems.Add("RetryPolicy.MaxDelayMs must be greater than or equal to RetryPolicy.InitialDelayMs.");
            }
        }

        // Validate DeliveryStats
        if (value.DeliveryStats is null)
        {
            problems.Add("DeliveryStats must not be null.");
        }
        else
        {
            if (value.DeliveryStats.TotalDeliveries < 0)
            {
                problems.Add("DeliveryStats.TotalDeliveries must be a non-negative integer.");
            }

            if (value.DeliveryStats.SuccessfulDeliveries < 0)
            {
                problems.Add("DeliveryStats.SuccessfulDeliveries must be a non-negative integer.");
            }

            if (value.DeliveryStats.FailedDeliveries < 0)
            {
                problems.Add("DeliveryStats.FailedDeliveries must be a non-negative integer.");
            }

            if (value.DeliveryStats.SuccessfulDeliveries + value.DeliveryStats.FailedDeliveries > value.DeliveryStats.TotalDeliveries)
            {
                problems.Add("DeliveryStats.TotalDeliveries must be greater than or equal to the sum of SuccessfulDeliveries and FailedDeliveries.");
            }

            if (value.DeliveryStats.LastDeliveryTime.HasValue && value.DeliveryStats.LastDeliveryTime.Value > DateTime.UtcNow)
            {
                problems.Add("DeliveryStats.LastDeliveryTime must not be in the future.");
            }
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt must be set to a valid DateTime.");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("CreatedAt must not be in the future.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a webhook subscription is valid.
    /// </summary>
    /// <param name="value">The webhook subscription to check.</param>
    /// <returns>True if the subscription is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this WebhookSubscription? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a webhook subscription is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The webhook subscription to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the subscription is invalid, containing the list of problems.</exception>
    public static void EnsureValid(this WebhookSubscription? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "The webhook subscription is invalid. " +
                string.Join(" ", problems),
                nameof(value));
        }
    }
}