# RequestCoalescingService

Represents a single coalesced request in a request-coalescing pattern. When multiple callers attempt to fetch the same resource concurrently, the first caller creates a `RequestCoalescingService` instance and initiates the actual work; subsequent callers attach to the same instance via `IncrementFollowers` and await the same `Tcs` task. The instance is disposed after the work completes and all followers have been served.

## API

### `public RequestCoalescingService()`

Initializes a new instance of the `RequestCoalescingService` class.  
Sets `CreatedAt` to the current UTC time and creates a new `TaskCompletionSource<byte[]?>` for the `Tcs` property.

### `public async Task<byte[]?> GetOrCoalesceAsync`

Initiates or joins the coalesced operation.  
This method is intended to be called by the first caller to start the actual asynchronous work (e.g., fetching data from an upstream service). It should be awaited to obtain the result.  
The exact behavior depends on the implementation; typically the method performs the work and completes the `Tcs` with the result or an exception.

**Returns**  
A `Task<byte[]?>` that completes with the result of the coalesced operation, or `null` if the operation yields no data.

**Exceptions**  
May throw exceptions originating from the underlying work (e.g., network errors, timeouts). The exception is propagated through the `Tcs` to all followers.

### `public void Dispose()`

Releases any resources held by the instance.  
After disposal, the instance should no longer be used. Calling `Dispose` does not cancel the `Tcs`; it only cleans up unmanaged resources (if any).

### `public TaskCompletionSource<byte[]?> Tcs`

The `TaskCompletionSource` that represents the outcome of the coalesced operation.  
The first caller should complete this source (via `SetResult`, `SetException`, or `SetCanceled`) after performing the work. All followers await `Tcs.Task` to receive the same result or exception.

### `public DateTimeOffset CreatedAt`

The UTC timestamp when this instance was created. Useful for tracking the age of the coalesced request and for implementing time‑based eviction policies.

### `public void IncrementFollowers()`

Increments the count of followers that are waiting on this coalesced request.  
Should be called by every caller that joins an existing coalescing instance (i.e., not the first caller). Thread‑safe.

### `public void DecrementFollowers()`

Decrements the count of followers.  
Typically called when a follower completes (either by receiving the result or by being cancelled). The instance may be disposed when the follower count reaches zero and the `Tcs` is already completed.

## Usage

### Example 1: Basic coalescing pattern

```csharp
private static readonly ConcurrentDictionary<string, RequestCoalescingService> _pendingRequests = new();

public async Task<byte[]?> GetDataAsync(string key)
{
    // Try to create a new coalescing entry
    var coalescer = new RequestCoalescingService();
    var existing = _pendingRequests.GetOrAdd(key, coalescer);

    if (existing != coalescer)
    {
        // Another caller already started; join as a follower
        existing.IncrementFollowers();
        try
        {
            return await existing.Tcs.Task;
        }
        finally
        {
            existing.DecrementFollowers();
        }
    }

    // We are the first caller – perform the actual work
    try
    {
        var result = await FetchFromUpstreamAsync(key);
        coalescer.Tcs.TrySetResult(result);
        return result;
    }
    catch (Exception ex)
    {
        coalescer.Tcs.TrySetException(ex);
        throw;
    }
    finally
    {
        _pendingRequests.TryRemove(key, out _);
        coalescer.Dispose();
    }
}
```

### Example 2: Using `GetOrCoalesceAsync` as the work method

```csharp
public class DataService
{
    private readonly ConcurrentDictionary<string, RequestCoalescingService> _coalescers = new();

    public async Task<byte[]?> GetResourceAsync(string resourceId)
    {
        var coalescer = new RequestCoalescingService();
        var existing = _coalescers.GetOrAdd(resourceId, coalescer);

        if (existing != coalescer)
        {
            // Follower path
            existing.IncrementFollowers();
            try
            {
                return await existing.Tcs.Task;
            }
            finally
            {
                existing.DecrementFollowers();
            }
        }

        // First caller – use GetOrCoalesceAsync to perform the work
        try
        {
            // GetOrCoalesceAsync is expected to complete the Tcs internally
            return await coalescer.GetOrCoalesceAsync();
        }
        finally
        {
            _coalescers.TryRemove(resourceId, out _);
            coalescer.Dispose();
        }
    }
}
```

## Notes

- **Thread safety**: `IncrementFollowers` and `DecrementFollowers` are designed to be called from multiple threads concurrently. The `Tcs` property itself is thread‑safe (as `TaskCompletionSource` is). However, the overall coalescing logic (e.g., dictionary operations) must be synchronized externally.
- **Disposal**: `Dispose` does not affect the `Tcs`. The instance can be safely disposed after all followers have completed and the `Tcs` is finalized. Disposing before all followers have awaited may lead to resource leaks if the instance holds unmanaged resources.
- **Edge case – cancellation**: If a follower is cancelled, it should still call `DecrementFollowers` to maintain the follower count. The `Tcs` may remain incomplete; the first caller should handle cancellation appropriately (e.g., by calling `TrySetCanceled`).
- **Edge case – timeout**: The `CreatedAt` timestamp can be used to implement a timeout policy: if the coalesced request has been pending too long, the first caller may abort and set an exception on the `Tcs`.
- **Public `Tcs`**: Exposing the `TaskCompletionSource` directly allows external code to complete it. This is intentional for flexibility but must be used with care to avoid multiple completions or misuse.
