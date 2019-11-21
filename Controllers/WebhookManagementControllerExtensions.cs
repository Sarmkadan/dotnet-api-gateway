#nullable enable

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Models;
using System.Linq;

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
    public static IActionResult GetWebhookSubscriptionsByEventType(this WebhookManagementController controller, string eventType)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        var allSubscriptions = controller.GetAllWebhookSubscriptions();

        if (allSubscriptions is not OkObjectResult okResult)
            return allSubscriptions;

        var subscriptions = (IEnumerable<WebhookSubscription>)okResult.Value!;

        var filteredSubscriptions = eventType switch
        {
            "*" => subscriptions,
            _ => subscriptions.Where(s => s.EventTypes.Contains(eventType) || s.EventTypes.Contains("*")).ToList()
        };

        return new OkObjectResult(filteredSubscriptions);
    }

    /// <summary>
    /// Pauses a webhook subscription (sets Active to false without deleting).
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to pause.</param>
    /// <returns>No content result.</returns>
    public static IActionResult PauseWebhookSubscription(this WebhookManagementController controller, string id)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        var subscriptionResult = controller.GetWebhookSubscription(id);

        if (subscriptionResult is not OkObjectResult okResult)
            return subscriptionResult;

        var subscription = (WebhookSubscription)okResult.Value!;
        subscription.Active = false;

        var updateRequest = new UpdateWebhookSubscriptionRequest
        {
            EventTypes = subscription.EventTypes
        };

        return controller.UpdateWebhookSubscription(id, updateRequest);
    }

    /// <summary>
    /// Resumes a paused webhook subscription (sets Active to true).
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to resume.</param>
    /// <returns>No content result.</returns>
    public static IActionResult ResumeWebhookSubscription(this WebhookManagementController controller, string id)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        var subscriptionResult = controller.GetWebhookSubscription(id);

        if (subscriptionResult is not OkObjectResult okResult)
            return subscriptionResult;

        var subscription = (WebhookSubscription)okResult.Value!;
        subscription.Active = true;

        var updateRequest = new UpdateWebhookSubscriptionRequest
        {
            EventTypes = subscription.EventTypes
        };

        return controller.UpdateWebhookSubscription(id, updateRequest);
    }

    /// <summary>
    /// Updates the retry policy for a webhook subscription.
    /// </summary>
    /// <param name="controller">The webhook management controller instance.</param>
    /// <param name="id">The subscription ID to update.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>The updated subscription.</returns>
    public static IActionResult UpdateWebhookRetryPolicy(
        this WebhookManagementController controller,
        string id,
        int maxRetries)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        var subscriptionResult = controller.GetWebhookSubscription(id);

        if (subscriptionResult is not OkObjectResult okResult)
            return subscriptionResult;

        var subscription = (WebhookSubscription)okResult.Value!;

        var updateRequest = new UpdateWebhookSubscriptionRequest
        {
            EventTypes = subscription.EventTypes,
            MaxRetries = maxRetries
        };

        return controller.UpdateWebhookSubscription(id, updateRequest);
    }
}