// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetApiGateway.Services;

/// <summary>
/// Coalesces duplicate concurrent requests so that only one upstream call is made
/// per unique request key, with all concurrent callers receiving the same response.
/// Thread-safe; designed for singleton registration.
/// </summary>
public sealed class RequestCoalescingService : IDisposable
{
    private readonly ConcurrentDictionary<string, CoalescedEntry> _inFlight = new();
    private readonly ILogger<RequestCoalescingService> _logger;
    private readonly Timer _cleanupTimer;

    private static readonly TimeSpan StaleEntryThreshold = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Initialises the service and starts a periodic cleanup timer for stale entries.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public RequestCoalescingService(ILogger<RequestCoalescingService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(PurgeStaleEntries, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>Gets the number of coalescing groups currently in flight.</summary>
    public int ActiveCoalescingGroups => _inFlight.Count;

    /// <summary>
    /// Executes <paramref name="fetchFunc"/> once for the first request matching
    /// <paramref name="key"/> and shares its result with all concurrent callers
    /// awaiting the same key. Callers that exceed <see cref="RequestCoalescingPolicy.MaxQueuedRequests"/>
    /// or whose wait exceeds <see cref="RequestCoalescingPolicy.TimeoutMs"/> execute independently.
    /// </summary>
    /// <param name="key">Deterministic key that identifies the logical request.</param>
    /// <param name="fetchFunc">Factory performing the actual upstream call.</param>
    /// <param name="policy">Policy that controls coalescing behaviour.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Response bytes from the upstream call.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> or <paramref name="fetchFunc"/> is <see langword="null"/>.
    /// </exception>
    public async Task<byte[]?> GetOrCoalesceAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> fetchFunc,
        RequestCoalescingPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(fetchFunc);

        var candidate = new CoalescedEntry();

        if (_inFlight.TryAdd(key, candidate))
            return await ExecuteAsLeaderAsync(key, candidate, fetchFunc, cancellationToken);

        // A leader is already running — try to join it.
        if (_inFlight.TryGetValue(key, out var existing) &&
            existing.FollowerCount < policy.MaxQueuedRequests)
        {
            existing.IncrementFollowers();
            _logger.LogDebug(
                "Coalescing request for key '{Key}'; queued followers: {Count}",
                key, existing.FollowerCount);

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(policy.TimeoutMs);
                return await existing.Task.WaitAsync(linked.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Coalescing wait timed out; fall back to an independent call.
                _logger.LogWarning(
                    "Coalescing wait timed out for key '{Key}'; executing independently", key);
                return await fetchFunc(cancellationToken);
            }
            finally
            {
                existing.DecrementFollowers();
            }
        }

        // Queue capacity exceeded or leader already finished — execute independently.
        _logger.LogDebug(
            "Coalescing queue full or entry gone for key '{Key}'; executing independently", key);
        return await fetchFunc(cancellationToken);
    }

    private async Task<byte[]?> ExecuteAsLeaderAsync(
        string key,
        CoalescedEntry entry,
        Func<CancellationToken, Task<byte[]?>> fetchFunc,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Leading coalesced request for key '{Key}'", key);
        try
        {
            var result = await fetchFunc(cancellationToken);
            entry.Tcs.TrySetResult(result);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            entry.Tcs.TrySetCanceled(ex.CancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            entry.Tcs.TrySetException(ex);
            throw;
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    private void PurgeStaleEntries(object? state)
    {
        var cutoff = DateTimeOffset.UtcNow - StaleEntryThreshold;
        var staleKeys = _inFlight
            .Where(kvp => kvp.Value.CreatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var k in staleKeys)
        {
            if (_inFlight.TryRemove(k, out var stale))
            {
                stale.Tcs.TrySetException(
                    new TimeoutException($"Coalesced entry for '{k}' exceeded the stale threshold."));
                _logger.LogWarning("Purged stale coalescing entry for key '{Key}'", k);
            }
        }

        if (staleKeys.Count > 0)
            _logger.LogInformation("Purged {Count} stale coalescing entries", staleKeys.Count);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cleanupTimer.Dispose();

        foreach (var entry in _inFlight.Values)
            entry.Tcs.TrySetException(new ObjectDisposedException(nameof(RequestCoalescingService)));
    }

    // ---------------------------------------------------------------------------
    // Inner types
    // ---------------------------------------------------------------------------

    private sealed class CoalescedEntry
    {
        private int _followerCount;

        /// <summary>Completion source shared with all followers waiting on this entry.</summary>
        public TaskCompletionSource<byte[]?> Tcs { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>The task followers await.</summary>
        public Task<byte[]?> Task => Tcs.Task;

        /// <summary>UTC timestamp at which this entry was created; used for stale-entry cleanup.</summary>
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

        /// <summary>Current number of follower requests joined to this entry.</summary>
        public int FollowerCount => _followerCount;

        /// <summary>Atomically increments the follower count.</summary>
        public void IncrementFollowers() => Interlocked.Increment(ref _followerCount);

        /// <summary>Atomically decrements the follower count.</summary>
        public void DecrementFollowers() => Interlocked.Decrement(ref _followerCount);
    }
}
