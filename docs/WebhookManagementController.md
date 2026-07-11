# WebhookManagementController

The `WebhookManagementController` is an ASP.NET Core controller responsible for managing webhook subscriptions within the `dotnet-api-gateway` project. It provides endpoints to create, retrieve, update, delete, and test webhook subscriptions, enabling external systems to receive event notifications via HTTP callbacks.

## API

### `WebhookManagementController`
**Purpose**: Initializes a new instance of the `WebhookManagementController`. This constructor is typically called by the ASP.NET Core dependency injection framework.

---

### `IActionResult CreateWebhookSubscription`
**Purpose**: Registers a new webhook subscription for specified event types.
**Parameters**:
- `callbackUrl` (string, required): The URL to which webhook notifications will be delivered.
- `eventTypes` (string[], required): The list of event types for which notifications should be sent.
- `maxRetries` (int, optional): The maximum number of retry attempts for failed deliveries. Defaults to a system-defined value if not provided.
- `initialDelayMs` (int, optional): The initial delay in milliseconds before the first retry attempt. Defaults to a system-defined value if not provided.
- `maxDelayMs` (int, optional): The maximum delay in milliseconds between retry attempts. Defaults to a system-defined value if not provided.
**Returns**: An `IActionResult` indicating success (`201 Created`) or failure (`400 Bad Request` if validation fails).
**Throws**: Returns `400 Bad Request` if `callbackUrl` or `eventTypes` are invalid or missing.

---

### `IActionResult GetAllWebhookSubscriptions`
**Purpose**: Retrieves all active webhook subscriptions.
**Returns**: An `IActionResult` containing a list of webhook subscriptions (`200 OK`). Returns an empty list if no subscriptions exist.
**Throws**: None.

---

### `IActionResult GetWebhookSubscription`
**Purpose**: Retrieves a specific webhook subscription by its unique identifier.
**Parameters**:
- `id` (string, required): The unique identifier of the webhook subscription.
**Returns**: An `IActionResult` containing the subscription (`200 OK`) if found.
**Throws**: Returns `404 Not Found` if the subscription does not exist.

---

### `IActionResult UpdateWebhookSubscription`
**Purpose**: Updates an existing webhook subscription with new parameters.
**Parameters**:
- `id` (string, required): The unique identifier of the webhook subscription.
- `callbackUrl` (string, optional): The updated URL for webhook notifications.
- `eventTypes` (string[], optional): The updated list of event types.
- `maxRetries` (int, optional): The updated maximum retry attempts.
- `initialDelayMs` (int, optional): The updated initial delay in milliseconds.
- `maxDelayMs` (int, optional): The updated maximum delay in milliseconds.
**Returns**: An `IActionResult` indicating success (`200 OK`) or failure (`400 Bad Request` if validation fails, `404 Not Found` if the subscription does not exist).
**Throws**: Returns `400 Bad Request` if `callbackUrl` or `eventTypes` are invalid.

---

### `IActionResult DeleteWebhookSubscription`
**Purpose**: Deletes a webhook subscription by its unique identifier.
**Parameters**:
- `id` (string, required): The unique identifier of the webhook subscription.
**Returns**: An `IActionResult` indicating success (`204 No Content`) or failure (`404 Not Found` if the subscription does not exist).
**Throws**: None.

---

### `Task<IActionResult> TestWebhookDelivery`
**Purpose**: Sends a test notification to the specified webhook subscription to verify delivery.
**Parameters**:
- `id` (string, required): The unique identifier of the webhook subscription.
**Returns**: An `IActionResult` indicating success (`200 OK` with delivery status) or failure (`404 Not Found` if the subscription does not exist, `500 Internal Server Error` if delivery fails).
**Throws**: Returns `404 Not Found` if the subscription does not exist.

---

### Public Properties
#### `CallbackUrl`
**Type**: `string?`
**Purpose**: The URL for webhook notifications. This property is exposed for serialization or internal use but should not be modified directly.

#### `EventTypes`
**Type**: `string[]?`
**Purpose**: The list of event types for which notifications are sent. This property is exposed for serialization or internal use but should not be modified directly.

#### `MaxRetries`
**Type**: `int?`
**Purpose**: The maximum number of retry attempts for failed deliveries. This property is exposed for serialization or internal use but should not be modified directly.

#### `InitialDelayMs`
**Type**: `int?`
**Purpose**: The initial delay in milliseconds before the first retry attempt. This property is exposed for serialization or internal use but should not be modified directly.

#### `MaxDelayMs`
**Type**: `int?`
**Purpose**: The maximum delay in milliseconds between retry attempts. This property is exposed for serialization or internal use but should not be modified directly.

## Usage

### Example 1: Creating and Testing a Webhook Subscription
