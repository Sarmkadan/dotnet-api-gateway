#nullable enable

using DotNetApiGateway.Constants;
using DotNetApiGateway.Exceptions;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for CircuitBreakerService focusing on state transitions and exception throwing behavior
/// </summary>
public sealed class CircuitBreakerServiceBehaviorTests
{
    private readonly CircuitBreakerRepository _repository;
    private readonly CircuitBreakerService _service;
    private readonly Mock<ILogger<CircuitBreakerService>> _loggerMock;
    private readonly CircuitBreakerPolicy _defaultPolicy;

    public CircuitBreakerServiceBehaviorTests()
    {
        _repository = new CircuitBreakerRepository();
        _loggerMock = new Mock<ILogger<CircuitBreakerService>>();
        _service = new CircuitBreakerService(_repository, _loggerMock.Object);

        _defaultPolicy = new CircuitBreakerPolicy
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            TimeoutSeconds = 1,
            Enabled = true
        };
    }

    [Fact]
    public async Task CanAttempt_ClosedToOpen_AfterFailureThresholdReached()
    {
        // Arrange
        var serviceName = "payment-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 3, Enabled = true };

        // Act - record failures up to threshold
        await _service.RecordFailureAsync(serviceName, "timeout", policy);
        await _service.RecordFailureAsync(serviceName, "timeout", policy);

        // Third failure should open the circuit
        await _service.RecordFailureAsync(serviceName, "timeout", policy);

        // Assert - circuit is now open
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
        status.FailureCount.Should().Be(3);
    }

    [Fact]
    public async Task CanAttempt_OpenState_ThrowsCircuitBreakerExceptionImmediately()
    {
        // Arrange
        var serviceName = "downstream-api";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 60, Enabled = true };

        // Open the circuit
        await _service.RecordFailureAsync(serviceName, "service unavailable", policy);

        // Act & Assert - should throw immediately without waiting
        var act = () => _service.CanAttemptAsync(serviceName, policy);
        await act.Should().ThrowAsync<CircuitBreakerException>();
    }

    [Fact]
    public async Task CanAttempt_OpenToHalfOpen_AfterTimeoutElapsed()
    {
        // Arrange
        var serviceName = "external-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 1, Enabled = true };

        // Open the circuit
        await _service.RecordFailureAsync(serviceName, "gateway timeout", policy);

        // Verify circuit is open
        var statusBefore = await _service.GetStatusAsync(serviceName);
        statusBefore.Should().NotBeNull();
        statusBefore!.State.Should().Be(CircuitBreakerState.Open);

        // Wait for timeout to elapse (slightly more than TimeoutSeconds)
        await Task.Delay(1100);

        // Act - attempt should transition to HalfOpen and allow the attempt
        var canAttempt = await _service.CanAttemptAsync(serviceName, policy);

        // Assert
        canAttempt.Should().BeTrue();
        var statusAfter = await _service.GetStatusAsync(serviceName);
        statusAfter.Should().NotBeNull();
        statusAfter!.State.Should().Be(CircuitBreakerState.HalfOpen);
    }

    [Fact]
    public async Task CanAttempt_HalfOpenState_AllowsAttempts()
    {
        // Arrange
        var serviceName = "database-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, SuccessThreshold = 2, TimeoutSeconds = 1, Enabled = true };

        // Open circuit
        await _service.RecordFailureAsync(serviceName, "connection failed", policy);

        // Wait for timeout
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Verify state
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.HalfOpen);

        // Act - should allow attempt in HalfOpen state
        var canAttempt = await _service.CanAttemptAsync(serviceName, policy);

        // Assert
        canAttempt.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSuccess_HalfOpenToClosed_AfterSuccessThresholdMet()
    {
        // Arrange
        var serviceName = "cache-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, SuccessThreshold = 3, TimeoutSeconds = 1, Enabled = true };

        // Open circuit
        await _service.RecordFailureAsync(serviceName, "timeout", policy);

        // Wait for timeout
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Record successes up to threshold
        await _service.RecordSuccessAsync(serviceName, policy);
        await _service.RecordSuccessAsync(serviceName, policy);

        // Third success should close the circuit
        await _service.RecordSuccessAsync(serviceName, policy);

        // Assert - circuit is now closed
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Closed);
        status.SuccessCount.Should().Be(0); // Reset on close
        status.FailureCount.Should().Be(0); // Reset on close
    }

    [Fact]
    public async Task RecordFailure_HalfOpenToOpen_ImmediatelyReopensOnFailure()
    {
        // Arrange
        var serviceName = "auth-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, SuccessThreshold = 2, TimeoutSeconds = 1, Enabled = true };

        // Open circuit
        await _service.RecordFailureAsync(serviceName, "unauthorized", policy);

        // Wait for timeout
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Record a failure in HalfOpen state
        await _service.RecordFailureAsync(serviceName, "still failing", policy);

        // Assert - circuit should immediately reopen
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task CircuitBreakerException_ContainsServiceNameAndRetryAfterSeconds()
    {
        // Arrange
        var serviceName = "third-party-api";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 30, Enabled = true };

        // Open the circuit
        await _service.RecordFailureAsync(serviceName, "network error", policy);

        // Act
        CircuitBreakerException? exception = null;
        try
        {
            await _service.CanAttemptAsync(serviceName, policy);
        }
        catch (CircuitBreakerException ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().NotBeNull();
        exception!.ServiceName.Should().Be(serviceName);
        exception.RetryAfterSeconds.Should().BeGreaterThan(0);
        exception.Message.Should().Contain(serviceName);
        exception.ErrorCode.Should().Be("CIRCUIT_BREAKER_OPEN");
    }

    [Fact]
    public async Task CanAttempt_OpenCircuit_RejectsWithCorrectRetryTime()
    {
        // Arrange
        var serviceName = "slow-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 60, Enabled = true };

        // Open the circuit
        await _service.RecordFailureAsync(serviceName, "timeout", policy);

        // Act
        CircuitBreakerException? exception = null;
        try
        {
            await _service.CanAttemptAsync(serviceName, policy);
        }
        catch (CircuitBreakerException ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().NotBeNull();
        exception!.RetryAfterSeconds.Should().BeGreaterThanOrEqualTo(59); // Should be close to 60 seconds
    }

    [Fact]
    public async Task GetOrCreateStatusAsync_CreatesStatusForNewService()
    {
        // Arrange
        var serviceName = "new-service";

        // Act
        var status = await _service.GetOrCreateStatusAsync(serviceName);

        // Assert
        status.Should().NotBeNull();
        status.ServiceName.Should().Be(serviceName);
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task IsCircuitOpenAsync_ReturnsCorrectState()
    {
        // Arrange
        var closedService = "available-service";
        var openService = "failed-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        // Open circuit for one service
        await _service.RecordFailureAsync(openService, "error", policy);

        // Act
        var isClosedOpen = await _service.IsCircuitOpenAsync(closedService);
        var isOpenOpen = await _service.IsCircuitOpenAsync(openService);

        // Assert
        isClosedOpen.Should().BeFalse();
        isOpenOpen.Should().BeTrue();
    }

    [Fact]
    public async Task ResetCircuitAsync_ReturnsCircuitToClosedState()
    {
        // Arrange
        var serviceName = "temporary-failure-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 2, Enabled = true };

        // Open the circuit
        await _service.RecordFailureAsync(serviceName, "error", policy);
        await _service.RecordFailureAsync(serviceName, "error", policy);

        var statusBefore = await _service.GetStatusAsync(serviceName);
        statusBefore.Should().NotBeNull();
        statusBefore!.State.Should().Be(CircuitBreakerState.Open);

        // Act
        await _service.ResetCircuitAsync(serviceName);

        // Assert
        var statusAfter = await _service.GetStatusAsync(serviceName);
        statusAfter.Should().NotBeNull();
        statusAfter!.State.Should().Be(CircuitBreakerState.Closed);
        statusAfter.FailureCount.Should().Be(0);
        statusAfter.SuccessCount.Should().Be(0);
    }
}
