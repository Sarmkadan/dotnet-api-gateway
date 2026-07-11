# WebhookRegistryExtensions
The `WebhookRegistryExtensions` class provides a set of static methods for managing webhooks in a registry, allowing for registration, unregistration, and querying of event subscriptions. This class is designed to be used in conjunction with a webhook registry, providing a simple and efficient way to manage webhooks.

## API
* `public static int RegisterRange`: Registers a range of webhooks in the registry. The return value represents the number of successfully registered webhooks. This method does not throw any exceptions.
* `public static int UnregisterRange`: Unregisters a range of webhooks from the registry. The return value represents the number of successfully unregistered webhooks. This method does not throw any exceptions.
* `public static bool HasSubscriptionsForEvent`: Checks if there are any subscriptions for a given event. The return value is `true` if there are subscriptions, `false` otherwise. This method does not throw any exceptions.
* `public static int CountSubscriptionsForEvent`: Returns the number of subscriptions for a given event. This method does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `WebhookRegistryExtensions` class:
```csharp
// Register a range of webhooks
int registeredCount = WebhookRegistryExtensions.RegisterRange();
Console.WriteLine($"Registered {registeredCount} webhooks");

// Check if there are subscriptions for an event
bool hasSubscriptions = WebhookRegistryExtensions.HasSubscriptionsForEvent("MyEvent");
if (hasSubscriptions)
{
    Console.WriteLine("There are subscriptions for MyEvent");
}
else
{
    Console.WriteLine("There are no subscriptions for MyEvent");
}
```

## Notes
When using the `WebhookRegistryExtensions` class, note that the `RegisterRange` and `UnregisterRange` methods do not throw exceptions, but instead return the number of successfully registered or unregistered webhooks. This allows for easy handling of partial successes. Additionally, the `HasSubscriptionsForEvent` and `CountSubscriptionsForEvent` methods are thread-safe, allowing for concurrent access to the webhook registry. However, the registry itself may not be thread-safe, so care should be taken when accessing it from multiple threads. Edge cases, such as registering or unregistering a range of webhooks that is empty or null, should be handled carefully to avoid unexpected behavior.
