#nullable enable

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="WebhookManagementController"/> providing additional webhook management functionality.
/// </summary>
public static class WebhookManagementControllerExtensions
{
    /// <summary>
    /// Gets webhook subscriptions filtered by event type.
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="eventType">The event type to filter by (use "*" for all).</param>
    /// <returns>Filtered list of webhook subscriptions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="eventType"/> is <see langword="null"/>.</exception>
    public static IActionResult GetWebhookSubscriptionsByEventType(this WebhookManagementController controller, string eventType)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(eventType);

        var allSubscriptions = controller.GetAllWebhookSubscriptions();

        return allSubscriptions switch
        {
            OkObjectResult okResult => HandleGetSubscriptionsByEventType(okResult, eventType),
            _ => allSubscriptions
        };

        static IActionResult HandleGetSubscriptionsByEventType(OkObjectResult okResult, string eventType)
        {
            var subscriptions = (IEnumerable<WebhookSubscription>)okResult.Value!;
            var filteredSubscriptions = eventType switch
            {
                "*" => subscriptions,
                _ => subscriptions.Where(s => s.EventTypes.Contains(eventType) || s.EventTypes.Contains("*")).ToList()
            };
            return new OkObjectResult(filteredSubscriptions);
        }
    }

    /// <summary>
    /// Pauses a webhook subscription (sets Active to false without deleting).
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to pause.</param>
    /// <returns>No content result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public static Task<IActionResult> PauseWebhookSubscription(this WebhookManagementController controller, string id)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(id);

        var subscriptionResult = controller.GetWebhookSubscription(id);

        return subscriptionResult switch
        {
            OkObjectResult okResult => HandlePauseSubscription(controller, okResult, id),
            _ => Task.FromResult(subscriptionResult)
        };

        static Task<IActionResult> HandlePauseSubscription(WebhookManagementController controller, OkObjectResult okResult, string id)
        {
            var subscription = (WebhookSubscription)okResult.Value!;
            subscription.Active = false;

            var updateRequest = new UpdateWebhookSubscriptionRequest
            {
                EventTypes = subscription.EventTypes
            };

            return controller.UpdateWebhookSubscription(id, updateRequest);
        }
    }

    /// <summary>
    /// Resumes a paused webhook subscription (sets Active to true).
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to resume.</param>
    /// <returns>No content result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public static Task<IActionResult> ResumeWebhookSubscription(this WebhookManagementController controller, string id)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(id);

        var subscriptionResult = controller.GetWebhookSubscription(id);

        return subscriptionResult switch
        {
            OkObjectResult okResult => HandleResumeSubscription(controller, okResult, id),
            _ => Task.FromResult(subscriptionResult)
        };

        static Task<IActionResult> HandleResumeSubscription(WebhookManagementController controller, OkObjectResult okResult, string id)
        {
            var subscription = (WebhookSubscription)okResult.Value!;
            subscription.Active = true;

            var updateRequest = new UpdateWebhookSubscriptionRequest
            {
                EventTypes = subscription.EventTypes
            };

            return controller.UpdateWebhookSubscription(id, updateRequest);
        }
    }

    /// <summary>
    /// Updates the retry policy for a webhook subscription.
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to update.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>The updated subscription.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative.</exception>
    public static Task<IActionResult> UpdateWebhookRetryPolicy(
        this WebhookManagementController controller,
        string id,
        int maxRetries)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var subscriptionResult = controller.GetWebhookSubscription(id);

        return subscriptionResult switch
        {
            OkObjectResult okResult => HandleUpdateRetryPolicy(controller, okResult, id, maxRetries),
            _ => Task.FromResult(subscriptionResult)
        };

        static Task<IActionResult> HandleUpdateRetryPolicy(WebhookManagementController controller, OkObjectResult okResult, string id, int maxRetries)
        {
            var subscription = (WebhookSubscription)okResult.Value!;

            var updateRequest = new UpdateWebhookSubscriptionRequest
            {
                EventTypes = subscription.EventTypes,
                MaxRetries = maxRetries
            };

            return controller.UpdateWebhookSubscription(id, updateRequest);
        }
    }
}