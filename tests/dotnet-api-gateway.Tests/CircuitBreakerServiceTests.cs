#nullable enable

using FluentAssertions;

namespace DotNetApiGateway.Tests.Services;

public sealed class CircuitBreakerServiceTests
{
    private readonly CircuitBreakerRepository _repository = new();
    private readonly CircuitBreakerService _service;

    public CircuitBreakerServiceTests()
    {
        _service = new CircuitBreakerService(_repository);
    }

    [Fact]
    public async Task GetOrCreateStatusAsync_FirstCallCreatesStatus_SecondCallReturnsExistingStatus()
    {
        const string serviceName = "inventory-service";

        var created = await _service.GetOrCreateStatusAsync(serviceName);
        var existing = await _service.GetOrCreateStatusAsync(serviceName);

        created.ServiceName.Should().Be(serviceName);
        created.State.Should().Be(CircuitBreakerState.Closed);
        existing.Should().BeSameAs(created);
        (await _repository.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new CircuitBreakerService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ServiceNameGuards_NullOrEmptyValue_ThrowArgumentException(string? serviceName)
    {
        var policy = new CircuitBreakerPolicy();

        var actions = new Func<Task>[]
        {
            () => _service.GetOrCreateStatusAsync(serviceName!),
            () => _service.IsCircuitOpenAsync(serviceName!),
            () => _service.CanAttemptAsync(serviceName!, policy),
            () => _service.RecordSuccessAsync(serviceName!, policy),
            () => _service.RecordFailureAsync(serviceName, "failure", policy),
            () => _service.GetStatusAsync(serviceName!),
            () => _service.ResetCircuitAsync(serviceName!)
        };

        foreach (var action in actions)
            await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PolicyGuards_NullPolicy_ThrowArgumentNullException()
    {
        const string serviceName = "guarded-service";
        var actions = new Func<Task>[]
        {
            () => _service.CanAttemptAsync(serviceName, null!),
            () => _service.RecordSuccessAsync(serviceName, null!),
            () => _service.RecordFailureAsync(serviceName, "failure", null!)
        };

        foreach (var action in actions)
            await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RecordFailureAsync_NullOrEmptyError_ThrowsArgumentException(string? error)
    {
        var act = () => _service.RecordFailureAsync(
            "guarded-service",
            error!,
            new CircuitBreakerPolicy());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CanAttemptAsync_DisabledPolicy_ReturnsTrueEvenWhenCircuitIsOpen()
    {
        const string serviceName = "payment-service";
        var status = await _service.GetOrCreateStatusAsync(serviceName);
        status.ChangeState(CircuitBreakerState.Open);
        await _repository.UpdateAsync(status);

        var canAttempt = await _service.CanAttemptAsync(
            serviceName,
            new CircuitBreakerPolicy { Enabled = false });

        canAttempt.Should().BeTrue();
        status.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task CanAttemptAsync_OpenCircuitAfterTimeout_TransitionsToHalfOpenAndReturnsTrue()
    {
        const string serviceName = "reporting-service";
        var policy = new CircuitBreakerPolicy
        {
            Enabled = true,
            FailureThreshold = 1,
            TimeoutSeconds = 30
        };
        var status = await OpenCircuitAsync(serviceName, policy);
        status.LastStateChangeAt = DateTime.UtcNow - TimeSpan.FromSeconds(policy.TimeoutSeconds + 1);
        await _repository.UpdateAsync(status);

        var canAttempt = await _service.CanAttemptAsync(serviceName, policy);

        canAttempt.Should().BeTrue();
        status.State.Should().Be(CircuitBreakerState.HalfOpen);
    }

    [Fact]
    public async Task CanAttemptAsync_OpenCircuitBeforeTimeout_ThrowsCircuitBreakerException()
    {
        const string serviceName = "shipping-service";
        var policy = new CircuitBreakerPolicy
        {
            Enabled = true,
            FailureThreshold = 1,
            TimeoutSeconds = 60
        };
        await OpenCircuitAsync(serviceName, policy);

        var act = () => _service.CanAttemptAsync(serviceName, policy);

        await act.Should().ThrowAsync<CircuitBreakerException>()
            .Where(exception => exception.ServiceName == serviceName)
            .Where(exception => exception.RetryAfterSeconds > 0);
    }

    private async Task<CircuitBreakerStatus> OpenCircuitAsync(
        string serviceName,
        CircuitBreakerPolicy policy)
    {
        await _service.RecordFailureAsync(serviceName, "downstream failure", policy);
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
        return status;
    }
}
