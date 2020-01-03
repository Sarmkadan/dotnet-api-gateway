# WebhookManagementControllerExtensions

Provides extension methods for managing webhook subscriptions within the API gateway. These methods encapsulate operations such as querying subscriptions by event type, pausing and resuming delivery, and updating retry policies, returning standardized `IActionResult` responses suitable for direct use in ASP.NET Core controller actions.

## API

### GetWebhookSubscriptionsByEventType

Retrieves all webhook subscriptions registered for a specified event type.

**Parameters:**
- `HttpContext context` — The current HTTP context, used to access registered services and the request pipeline.
- `string eventType` — The event type identifier for which subscriptions should be fetched. Must not be null or empty.

**Returns:**
`IActionResult` — A 200 OK result containing the collection of matching subscriptions, or a 400 Bad Request if the event type is invalid.

**Throws:**
`ArgumentNullException` — When `eventType` is null or whitespace.

---

### PauseWebhookSubscription

Temporarily suspends delivery of webhook events to the specified subscription.

**Parameters:**
- `HttpContext context` — The current HTTP context.
- `string subscriptionId` — The unique identifier of the subscription to pause. Must not be null or empty.

**Returns:**
`IActionResult` — A 200 OK result if the subscription was successfully paused, a 404 Not Found if no subscription matches the given ID, or a 409 Conflict if the subscription is already in a paused state.

**Throws:**
`ArgumentNullException` — When `subscriptionId` is null or whitespace.

---

### ResumeWebhookSubscription

Resumes delivery of webhook events to a previously paused subscription.

**Parameters:**
- `HttpContext context` — The current HTTP context.
- `string subscriptionId` — The unique identifier of the subscription to resume. Must not be null or empty.

**Returns:**
`IActionResult` — A 200 OK result if the subscription was successfully resumed, a 404 Not Found if no subscription matches the given ID, or a 409 Conflict if the subscription is not currently paused.

**Throws:**
`ArgumentNullException` — When `subscriptionId` is null or whitespace.

---

### UpdateWebhookRetryPolicy

Modifies the retry policy configuration for a specific webhook subscription.

**Parameters:**
- `HttpContext context` — The current HTTP context.
- `string subscriptionId` — The unique identifier of the subscription to update. Must not be null or empty.
- `RetryPolicy newPolicy` — The new retry policy object containing updated retry count, intervals, and backoff strategy.

**Returns:**
`IActionResult` — A 200 OK result with the updated subscription details, a 404 Not Found if no subscription matches the given ID, or a 400 Bad Request if the provided policy fails validation.

**Throws:**
`ArgumentNullException` — When `subscriptionId` is null or whitespace, or when `newPolicy` is null.

---

## Usage

### Example 1: Retrieving Subscriptions and Pausing One

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    [HttpGet("subscriptions/{eventType}")]
    public IActionResult GetSubscriptions(string eventType)
    {
        return WebhookManagementControllerExtensions
            .GetWebhookSubscriptionsByEventType(HttpContext, eventType);
    }

    [HttpPost("subscriptions/{subscriptionId}/pause")]
    public IActionResult Pause(string subscriptionId)
    {
        return WebhookManagementControllerExtensions
            .PauseWebhookSubscription(HttpContext, subscriptionId);
    }
}
```

### Example 2: Updating a Retry Policy After Resuming

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhookPolicyController : ControllerBase
{
    [HttpPut("subscriptions/{subscriptionId}/retry-policy")]
    public IActionResult UpdateRetryPolicy(string subscriptionId, [FromBody] RetryPolicy newPolicy)
    {
        // First ensure the subscription is active
        var resumeResult = WebhookManagementControllerExtensions
            .ResumeWebhookSubscription(HttpContext, subscriptionId);

        if (resumeResult is ConflictObjectResult)
        {
            // Already active, proceed with update
        }

        return WebhookManagementControllerExtensions
            .UpdateWebhookRetryPolicy(HttpContext, subscriptionId, newPolicy);
    }
}
```

## Notes

- All methods require a valid `HttpContext` with a fully configured dependency injection container. Invoking them outside an active request pipeline or with a null context will result in undefined behavior or `NullReferenceException`.
- State-modifying methods (`PauseWebhookSubscription`, `ResumeWebhookSubscription`, `UpdateWebhookRetryPolicy`) are not inherently thread-safe. Concurrent requests targeting the same subscription ID may produce race conditions where one caller observes a 409 Conflict due to an intermediate state change applied by another request.
- The `RetryPolicy` object passed to `UpdateWebhookRetryPolicy` undergoes validation internally. Callers should consult the validation helpers introduced for webhook subscriptions to ensure policy objects are well-formed before submission.
- These extension methods are designed to be invoked directly from controller actions. They are stateless and do not maintain any internal cache or session affinity across calls.
