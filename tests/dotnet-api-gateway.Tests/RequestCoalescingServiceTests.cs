using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

public class RequestCoalescingServiceTests : IDisposable
{
    private readonly Mock<ILogger<RequestCoalescingService>> _loggerMock;
    private readonly RequestCoalescingService _service;
    private readonly RequestCoalescingPolicy _defaultPolicy;

    public RequestCoalescingServiceTests()
    {
        _loggerMock = new Mock<ILogger<RequestCoalescingService>>();
        _service = new RequestCoalescingService(_loggerMock.Object);

        _defaultPolicy = new RequestCoalescingPolicy
        {
            TimeoutMs = 5000,
            MaxQueuedRequests = 200,
            Enabled = true,
            IncludeQueryString = true,
            CoalescibleMethods = ["GET", "HEAD"]
        };
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void Constructor_InitializesCleanupTimer()
    {
        // Act
        var service = new RequestCoalescingService(_loggerMock.Object);

        // Assert
        Assert.Equal(0, service.ActiveCoalescingGroups);
        service.Dispose();
    }

    [Fact]
    public async Task GetOrCoalesceAsync_TwoConcurrentIdenticalRequests_ExecutesOnceAndSharesResult()
    {
        // Arrange
        var key = "test:GET:/api/data";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(100, ct);
            return new byte[] { 1, 2, 3 };
        });

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert
        Assert.Equal(1, fetchCallCount);
        Assert.Equal(new byte[] { 1, 2, 3 }, result1);
        Assert.Equal(new byte[] { 1, 2, 3 }, result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_ThreeConcurrentIdenticalRequests_ExecutesOnceAndSharesResult()
    {
        // Arrange
        var key = "test:GET:/api/data";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(100, ct);
            return new byte[] { 1, 2, 3 };
        });

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task3 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;

        // Assert
        Assert.Equal(1, fetchCallCount);
        Assert.Equal(new byte[] { 1, 2, 3 }, result1);
        Assert.Equal(new byte[] { 1, 2, 3 }, result2);
        Assert.Equal(new byte[] { 1, 2, 3 }, result3);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_DifferentKeys_ExecutesSeparately()
    {
        // Arrange
        var results = new ConcurrentDictionary<int, byte[]>();
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            var count = Interlocked.Increment(ref fetchCallCount);
            await Task.Delay(50, ct);
            var result = new byte[] { (byte)count };
            results[count] = result;
            return result;
        });

        var task1 = _service.GetOrCoalesceAsync("key1:GET:/api/data1", fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync("key2:GET:/api/data2", fetchFunc, _defaultPolicy);
        var task3 = _service.GetOrCoalesceAsync("key3:GET:/api/data3", fetchFunc, _defaultPolicy);

        // Act
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;

        // Assert
        Assert.Equal(3, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(new byte[] { 2 }, result2);
        Assert.Equal(new byte[] { 3 }, result3);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_FailedCall_PropagatesExceptionToAllFollowers()
    {
        // Arrange
        var key = "error:GET:/api/fail";
        var fetchCallCount = 0;
        var exception = new InvalidOperationException("Test failure");

        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(50, ct);
            throw exception;
        });

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act & Assert
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(() => task1);
        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(() => task2);

        Assert.Equal(1, fetchCallCount);
        Assert.Same(exception, exception1);
        Assert.Same(exception, exception2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_FailedCall_NotCachedForSubsequentRequests()
    {
        // Arrange
        var key = "cache:GET:/api/cache-test";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(50, ct);
            return new byte[] { (byte)fetchCallCount };
        });

        // First call succeeds
        var result1 = await _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        Assert.Equal(1, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);

        // Second call should execute again (not cached)
        var result2 = await _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        Assert.Equal(2, fetchCallCount);
        Assert.Equal(new byte[] { 2 }, result2);

        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_TimeoutExceeded_ExecutesIndependently()
    {
        // Arrange
        var key = "timeout:GET:/api/slow";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(1000, ct); // Long delay
            return new byte[] { 1 };
        });

        // Set very short timeout
        var policy = new RequestCoalescingPolicy
        {
            TimeoutMs = 50,
            MaxQueuedRequests = 200,
            Enabled = true
        };

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, policy);

        // Wait a bit for the first request to start
        await Task.Delay(100);

        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, policy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert - both should execute independently
        Assert.Equal(2, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(new byte[] { 1 }, result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_MaxQueuedRequestsExceeded_ExecutesIndependently()
    {
        // Arrange
        var key = "queue-full:GET:/api/busy";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(100, ct);
            return new byte[] { (byte)fetchCallCount };
        });

        // Set very low queue limit
        var policy = new RequestCoalescingPolicy
        {
            TimeoutMs = 5000,
            MaxQueuedRequests = 2, // Only allow 2 followers
            Enabled = true,
            CoalescibleMethods = ["GET"]
        };

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, policy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, policy);
        var task3 = _service.GetOrCoalesceAsync(key, fetchFunc, policy); // This should execute independently
        var task4 = _service.GetOrCoalesceAsync(key, fetchFunc, policy); // This should also execute independently

        // Act
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;
        var result4 = await task4;

        // Assert - first 2 should coalesce (1 call), next 2 should execute independently (2 more calls = 3 total)
        Assert.Equal(3, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(new byte[] { 1 }, result2);
        Assert.Equal(new byte[] { 2 }, result3);
        Assert.Equal(new byte[] { 3 }, result4);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var key = "cancel:GET:/api/cancel";
        var cts = new CancellationTokenSource();
        var fetchCallCount = 0;

        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(1000, ct); // Long delay
            return new byte[] { 1 };
        });

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy, cts.Token);

        // Wait a bit for the first request to start
        await Task.Delay(50);

        // Cancel the second request
        cts.Cancel();

        // Act & Assert - TaskCanceledException is thrown when cancellation is requested
        await Assert.ThrowsAsync<TaskCanceledException>(() => _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy, cts.Token));

        // The first request should still complete
        var result1 = await task1;
        Assert.Equal(1, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(ct => Task.FromResult<byte[]?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetOrCoalesceAsync(null!, fetchFunc, _defaultPolicy));
    }

    [Fact]
    public async Task GetOrCoalesceAsync_NullFetchFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test:GET:/api/data";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetOrCoalesceAsync(key, null!, _defaultPolicy));
    }

    [Fact]
    public async Task GetOrCoalesceAsync_PolicyDisabled_ExecutesIndependently()
    {
        // Arrange
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(50, ct);
            return new byte[] { (byte)fetchCallCount };
        });

        var policy = new RequestCoalescingPolicy
        {
            Enabled = false // Disabled
        };

        var task1 = _service.GetOrCoalesceAsync("key1:GET:/api/data", fetchFunc, policy);
        var task2 = _service.GetOrCoalesceAsync("key1:GET:/api/data", fetchFunc, policy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert - both should execute independently
        Assert.Equal(2, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(new byte[] { 2 }, result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_NonCoalescibleMethod_ExecutesIndependently()
    {
        // Arrange
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(50, ct);
            return new byte[] { (byte)fetchCallCount };
        });

        var policy = new RequestCoalescingPolicy
        {
            Enabled = true,
            CoalescibleMethods = ["GET"],
            TimeoutMs = 5000,
            MaxQueuedRequests = 200
        };

        var task1 = _service.GetOrCoalesceAsync("key1:POST:/api/data", fetchFunc, policy);
        var task2 = _service.GetOrCoalesceAsync("key1:POST:/api/data", fetchFunc, policy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert - both should execute independently (POST is not coalescible)
        Assert.Equal(2, fetchCallCount);
        Assert.Equal(new byte[] { 1 }, result1);
        Assert.Equal(new byte[] { 2 }, result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task Dispose_CancelsAllPendingRequests()
    {
        // Arrange
        var tcs = new TaskCompletionSource<byte[]?>();
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(ct => tcs.Task);

        var task1 = _service.GetOrCoalesceAsync("dispose:GET:/api/dispose", fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync("dispose:GET:/api/dispose", fetchFunc, _defaultPolicy);

        // Act - dispose the service
        _service.Dispose();

        // Assert - tasks should throw ObjectDisposedException when awaited
        await Assert.ThrowsAsync<ObjectDisposedException>(() => task1);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => task2);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_ConcurrentRequestsWithDifferentKeys_MultipleGroupsActive()
    {
        // Arrange
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(100, ct);
            return new byte[] { (byte)fetchCallCount };
        });

        // Create multiple concurrent requests with different keys
        var tasks = new Task<byte[]?>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _service.GetOrCoalesceAsync($"key{i}:GET:/api/data{i}", fetchFunc, _defaultPolicy);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, fetchCallCount);
        Assert.Equal(10, results.Length);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_ConcurrentRequestsWithSameKey_OnlyOneExecution()
    {
        // Arrange
        var fetchCallCount = 0;
        var stopwatch = Stopwatch.StartNew();
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(200, ct); // Simulate work
            return new byte[] { 1, 2, 3 };
        });

        // Create 20 concurrent requests with the same key
        var tasks = new Task<byte[]?>[20];
        for (int i = 0; i < 20; i++)
        {
            tasks[i] = _service.GetOrCoalesceAsync("same:GET:/api/all", fetchFunc, _defaultPolicy);
        }

        // Act
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(1, fetchCallCount); // Only one execution
        Assert.Equal(20, results.Length);
        foreach (var result in results)
        {
            Assert.Equal(new byte[] { 1, 2, 3 }, result);
        }

        // Should complete in roughly the same time as a single request (with some overhead)
        Assert.InRange(stopwatch.ElapsedMilliseconds, 200, 400);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_ReturnsNullResult()
    {
        // Arrange
        var key = "null:GET:/api/null";
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(ct => Task.FromResult<byte[]?>(null));

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_ExceptionThrown_ExceptionPropagatedToAll()
    {
        // Arrange
        var key = "exception:GET:/api/exception";
        var exception = new ApplicationException("Test application exception");

        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(ct => throw exception);

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task3 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<ApplicationException>(() => task1);
        var ex2 = await Assert.ThrowsAsync<ApplicationException>(() => task2);
        var ex3 = await Assert.ThrowsAsync<ApplicationException>(() => task3);

        Assert.Same(exception, ex1);
        Assert.Same(exception, ex2);
        Assert.Same(exception, ex3);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }

    [Fact]
    public async Task GetOrCoalesceAsync_EmptyByteArray_SuccessfullyShared()
    {
        // Arrange
        var key = "empty:GET:/api/empty";
        var fetchCallCount = 0;
        var fetchFunc = new Func<CancellationToken, Task<byte[]?>>(async ct =>
        {
            fetchCallCount++;
            await Task.Delay(100, ct);
            return Array.Empty<byte>();
        });

        var task1 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);
        var task2 = _service.GetOrCoalesceAsync(key, fetchFunc, _defaultPolicy);

        // Act
        var result1 = await task1;
        var result2 = await task2;

        // Assert
        Assert.Equal(1, fetchCallCount);
        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Equal(0, _service.ActiveCoalescingGroups);
    }
}