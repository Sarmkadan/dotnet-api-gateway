#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Primitives;

namespace DotNetApiGateway.Repositories;

/// <summary>
/// Change token source that notifies when route configuration changes.
/// Used for hot-reload of route configurations without restarting the gateway.
/// </summary>
public sealed class RouteConfigurationChangeTokenSource : IChangeToken
{
    private CancellationTokenSource _cts = new();
    private int _changeCount = 0;
    private readonly object _lock = new();

    /// <summary>
    /// Gets a value indicating whether a change has occurred.
    /// </summary>
    public bool HasChanged => _cts.IsCancellationRequested;

    /// <summary>
    /// Gets a value indicating whether this token will proactively raise callbacks.
    /// </summary>
    public bool ActiveChangeCallbacks => true;

    /// <summary>
    /// Registers a callback to be invoked when a change occurs.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="state">The state object to pass to the callback.</param>
    /// <returns>An <see cref="IDisposable"/> that can be disposed to unregister the callback.</returns>
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        if (HasChanged)
        {
            callback(state);
            return NullDisposable.Instance;
        }

        var registration = _cts.Token.Register(() => callback(state));
        return new CallbackRegistration(registration);
    }

    /// <summary>
    /// Signals that the route configuration has changed and notifies all registered callbacks.
    /// </summary>
    public void SignalChange()
    {
        lock (_lock)
        {
            _changeCount++;
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    /// <summary>
    /// Gets the current change count (version).
    /// </summary>
    public int GetChangeCount()
    {
        lock (_lock)
        {
            return _changeCount;
        }
    }

    private sealed class CallbackRegistration : IDisposable
    {
        private readonly IDisposable _registration;

        public CallbackRegistration(IDisposable registration)
        {
            _registration = registration;
        }

        public void Dispose()
        {
            _registration.Dispose();
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose() { }
    }
}