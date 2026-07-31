using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DotNetApiGateway.Events;

namespace DotNetApiGateway.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="EventBus"/> class.
/// Covers subscription management, publishing, subscriber counting and clearing.
/// </summary>
[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private List<Func<DummyEvent, Task>> _handlers = null!;
    private DummyEvent _event = null!;

    /// <summary>
    /// Number of handlers to register for each benchmark run.
    /// </summary>
    [Params(10, 100, 1000)]
    public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Prepare a collection of no‑op handlers and a dummy event instance.
        _handlers = new List<Func<DummyEvent, Task>>(SubscriberCount);
        for (int i = 0; i < SubscriberCount; i++)
        {
            _handlers.Add(_ => Task.CompletedTask);
        }

        _event = new DummyEvent();
    }

    private EventBus CreateBus()
    {
        // Use a null logger to avoid I/O overhead during benchmarks.
        ILogger<EventBus> logger = NullLogger<EventBus>.Instance;
        return new EventBus(logger);
    }

    [Benchmark]
    public void Subscribe()
    {
        var bus = CreateBus();

        foreach (var handler in _handlers)
        {
            bus.Subscribe<DummyEvent>(handler);
        }
    }

    [Benchmark]
    public void Unsubscribe()
    {
        var bus = CreateBus();

        // First subscribe all handlers, then unsubscribe them.
        foreach (var handler in _handlers)
        {
            bus.Subscribe<DummyEvent>(handler);
        }

        foreach (var handler in _handlers)
        {
            bus.Unsubscribe<DummyEvent>(handler);
        }
    }

    [Benchmark]
    public async Task PublishAsync()
    {
        var bus = CreateBus();

        // Subscribe all handlers before publishing.
        foreach (var handler in _handlers)
        {
            bus.Subscribe<DummyEvent>(handler);
        }

        await bus.PublishAsync(_event);
    }

    [Benchmark]
    public int GetSubscriberCount()
    {
        var bus = CreateBus();

        foreach (var handler in _handlers)
        {
            bus.Subscribe<DummyEvent>(handler);
        }

        return bus.GetSubscriberCount<DummyEvent>();
    }

    [Benchmark]
    public void Clear()
    {
        var bus = CreateBus();

        foreach (var handler in _handlers)
        {
            bus.Subscribe<DummyEvent>(handler);
        }

        bus.Clear();
    }

    /// <summary>
    /// Minimal implementation of <see cref="IGatewayEvent"/> used for benchmarking.
    /// </summary>
    private sealed class DummyEvent : IGatewayEvent
    {
        public DateTime Timestamp => DateTime.UtcNow;
        public string EventType => nameof(DummyEvent);
    }
}
