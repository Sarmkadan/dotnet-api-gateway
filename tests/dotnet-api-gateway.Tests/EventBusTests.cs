using DotNetApiGateway.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for EventBus class covering multiple handlers, unsubscribe, and exception isolation.
/// </summary>
public class EventBusTests
{
    private readonly Mock<ILogger<EventBus>> _loggerMock;
    private readonly EventBus _eventBus;

    public EventBusTests()
    {
        _loggerMock = new Mock<ILogger<EventBus>>();
        _eventBus = new EventBus(_loggerMock.Object);
    }

    [Fact]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Func<RouteCreatedEvent, Task> nullHandler = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _eventBus.Subscribe<RouteCreatedEvent>(nullHandler));
    }

    [Fact]
    public async Task Subscribe_WithValidHandler_AddsHandlerToSubscribers()
    {
        // Arrange
        var handlerCalled = false;
        Task Handler(RouteCreatedEvent _) { handlerCalled = true; return Task.CompletedTask; }

        // Act
        _eventBus.Subscribe<RouteCreatedEvent>(Handler);

        // Assert
        var count = _eventBus.GetSubscriberCount<RouteCreatedEvent>();
        Assert.Equal(1, count);

        // Publish to verify handler was actually added
        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };
        await _eventBus.PublishAsync(evt);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task PublishAsync_WithSingleHandler_InvokesHandler()
    {
        // Arrange
        var eventReceived = false;
        Task Handler(RouteCreatedEvent evt) { eventReceived = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler);

        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };

        // Act
        await _eventBus.PublishAsync(evt);

        // Assert
        Assert.True(eventReceived);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_InvokesAllHandlers()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        var handler3Called = false;

        Task Handler1(RouteCreatedEvent _) { handler1Called = true; return Task.CompletedTask; }
        Task Handler2(RouteCreatedEvent _) { handler2Called = true; return Task.CompletedTask; }
        Task Handler3(RouteCreatedEvent _) { handler3Called = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler1);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler2);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler3);

        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };

        // Act
        await _eventBus.PublishAsync(evt);

        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
        Assert.True(handler3Called);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEventTypes_OnlyInvokesCorrectHandlers()
    {
        // Arrange
        var routeHandlerCalled = false;
        var circuitBreakerHandlerCalled = false;

        Task RouteHandler(RouteCreatedEvent _) { routeHandlerCalled = true; return Task.CompletedTask; }
        Task CircuitBreakerHandler(CircuitBreakerStateChangedEvent _) { circuitBreakerHandlerCalled = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(RouteHandler);
        _eventBus.Subscribe<CircuitBreakerStateChangedEvent>(CircuitBreakerHandler);

        var routeEvent = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };
        var circuitBreakerEvent = new CircuitBreakerStateChangedEvent { TargetId = "target-1", OldState = "closed", NewState = "open" };

        // Act
        await _eventBus.PublishAsync(routeEvent);
        await _eventBus.PublishAsync(circuitBreakerEvent);

        // Assert
        Assert.True(routeHandlerCalled);
        Assert.True(circuitBreakerHandlerCalled);
    }

    [Fact]
    public async Task Unsubscribe_WithRegisteredHandler_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        Task Handler(RouteCreatedEvent _) { handlerCalled = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler);
        Assert.Equal(1, _eventBus.GetSubscriberCount<RouteCreatedEvent>());

        // Act
        _eventBus.Unsubscribe<RouteCreatedEvent>(Handler);

        // Assert
        Assert.Equal(0, _eventBus.GetSubscriberCount<RouteCreatedEvent>());
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Unsubscribe_WithNullHandler_DoesNotThrow()
    {
        // Arrange & Act & Assert
        _eventBus.Unsubscribe<RouteCreatedEvent>(null!);
    }

    [Fact]
    public async Task Unsubscribe_WithUnregisteredHandler_DoesNotThrow()
    {
        // Arrange
        Task Handler(RouteCreatedEvent _) => Task.CompletedTask;

        // Act & Assert
        _eventBus.Unsubscribe<RouteCreatedEvent>(Handler); // Never subscribed
    }

    [Fact]
    public async Task PublishAsync_WithHandlerException_IsolatesExceptionAndContinuesProcessing()
    {
        // Arrange
        var successfulHandlerCalled = false;
        var failedHandlerCalled = false;

        Task SuccessfulHandler(RouteCreatedEvent _)
        {
            successfulHandlerCalled = true;
            return Task.CompletedTask;
        }

        Task FailingHandler(RouteCreatedEvent _)
        {
            failedHandlerCalled = true;
            throw new InvalidOperationException("Handler failed");
        }

        _eventBus.Subscribe<RouteCreatedEvent>(SuccessfulHandler);
        _eventBus.Subscribe<RouteCreatedEvent>(FailingHandler);
        _eventBus.Subscribe<RouteCreatedEvent>(SuccessfulHandler); // Add another successful handler

        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };

        // Act
        await _eventBus.PublishAsync(evt);

        // Assert
        Assert.True(successfulHandlerCalled);
        Assert.True(failedHandlerCalled);
        Assert.Equal(3, _eventBus.GetSubscriberCount<RouteCreatedEvent>()); // All handlers still registered
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _eventBus.PublishAsync<RouteCreatedEvent>(null!));
    }

    [Fact]
    public void GetSubscriberCount_WithNoSubscribers_ReturnsZero()
    {
        // Act
        var count = _eventBus.GetSubscriberCount<RouteCreatedEvent>();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetSubscriberCount_WithMultipleSubscribers_ReturnsCorrectCount()
    {
        // Arrange
        Task Handler1(RouteCreatedEvent _) => Task.CompletedTask;
        Task Handler2(RouteCreatedEvent _) => Task.CompletedTask;
        Task Handler3(RouteCreatedEvent _) => Task.CompletedTask;

        _eventBus.Subscribe<RouteCreatedEvent>(Handler1);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler2);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler3);

        // Act
        var count = _eventBus.GetSubscriberCount<RouteCreatedEvent>();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Clear_WithMultipleSubscribers_RemovesAllHandlers()
    {
        // Arrange
        Task Handler1(RouteCreatedEvent _) => Task.CompletedTask;
        Task Handler2(CircuitBreakerStateChangedEvent _) => Task.CompletedTask;
        Task Handler3(RateLimitExceededEvent _) => Task.CompletedTask;

        _eventBus.Subscribe<RouteCreatedEvent>(Handler1);
        _eventBus.Subscribe<CircuitBreakerStateChangedEvent>(Handler2);
        _eventBus.Subscribe<RateLimitExceededEvent>(Handler3);

        Assert.Equal(1, _eventBus.GetSubscriberCount<RouteCreatedEvent>());
        Assert.Equal(1, _eventBus.GetSubscriberCount<CircuitBreakerStateChangedEvent>());
        Assert.Equal(1, _eventBus.GetSubscriberCount<RateLimitExceededEvent>());

        // Act
        _eventBus.Clear();

        // Assert
        Assert.Equal(0, _eventBus.GetSubscriberCount<RouteCreatedEvent>());
        Assert.Equal(0, _eventBus.GetSubscriberCount<CircuitBreakerStateChangedEvent>());
        Assert.Equal(0, _eventBus.GetSubscriberCount<RateLimitExceededEvent>());
    }

    [Fact]
    public async Task PublishAsync_WithDifferentEventTypes_DoesNotMixHandlers()
    {
        // Arrange
        var routeHandlerCalled = false;
        var rateLimitHandlerCalled = false;

        Task RouteHandler(RouteCreatedEvent _) { routeHandlerCalled = true; return Task.CompletedTask; }
        Task RateLimitHandler(RateLimitExceededEvent _) { rateLimitHandlerCalled = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(RouteHandler);
        _eventBus.Subscribe<RateLimitExceededEvent>(RateLimitHandler);

        var routeEvent = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };
        var rateLimitEvent = new RateLimitExceededEvent { ClientId = "client-1", RouteId = "route-1", RequestCount = 100, Limit = 50 };

        // Act
        await _eventBus.PublishAsync(routeEvent);
        await _eventBus.PublishAsync(rateLimitEvent);

        // Assert
        Assert.True(routeHandlerCalled);
        Assert.True(rateLimitHandlerCalled);
    }

    [Fact]
    public async Task PublishAsync_WithAsyncHandlers_AllHandlersComplete()
    {
        // Arrange
        var handler1Completed = false;
        var handler2Completed = false;

        async Task Handler1(RouteCreatedEvent _)
        {
            await Task.Delay(10);
            handler1Completed = true;
        }

        async Task Handler2(RouteCreatedEvent _)
        {
            await Task.Delay(5);
            handler2Completed = true;
        }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler1);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler2);

        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };

        // Act
        await _eventBus.PublishAsync(evt);

        // Assert
        Assert.True(handler1Completed);
        Assert.True(handler2Completed);
    }

    [Fact]
    public async Task Unsubscribe_RemovesOnlySpecificHandler()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        Task Handler1(RouteCreatedEvent _) { handler1Called = true; return Task.CompletedTask; }
        Task Handler2(RouteCreatedEvent _) { handler2Called = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler1);
        _eventBus.Subscribe<RouteCreatedEvent>(Handler2);
        Assert.Equal(2, _eventBus.GetSubscriberCount<RouteCreatedEvent>());

        // Act - unsubscribe only handler1
        _eventBus.Unsubscribe<RouteCreatedEvent>(Handler1);

        // Assert
        Assert.Equal(1, _eventBus.GetSubscriberCount<RouteCreatedEvent>());
        Assert.False(handler1Called);

        // Publish should only call handler2
        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };
        await _eventBus.PublishAsync(evt);
        Assert.True(handler2Called);
    }

    [Fact]
    public async Task PublishAsync_AfterClear_DoesNotInvokeAnyHandlers()
    {
        // Arrange
        var handlerCalled = false;
        Task Handler(RouteCreatedEvent _) { handlerCalled = true; return Task.CompletedTask; }

        _eventBus.Subscribe<RouteCreatedEvent>(Handler);
        Assert.Equal(1, _eventBus.GetSubscriberCount<RouteCreatedEvent>());

        _eventBus.Clear();
        Assert.Equal(0, _eventBus.GetSubscriberCount<RouteCreatedEvent>());

        var evt = new RouteCreatedEvent { RouteId = "route-1", RouteName = "test-route" };

        // Act
        await _eventBus.PublishAsync(evt);

        // Assert
        Assert.False(handlerCalled);
    }
}
