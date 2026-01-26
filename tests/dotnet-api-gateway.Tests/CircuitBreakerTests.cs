// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

public class CircuitBreakerStatusTests
{
    [Fact]
    public void RecordSuccess_InitialState_IncrementsAllCounters()
    {
        // Arrange
        var status = new CircuitBreakerStatus();

        // Act
        status.RecordSuccess();

        // Assert
        status.SuccessCount.Should().Be(1);
        status.TotalSuccesses.Should().Be(1);
        status.TotalRequests.Should().Be(1);
        status.LastError.Should().BeNull();
        status.LastSuccessAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_SetsLastErrorAndIncrements()
    {
        // Arrange
        var status = new CircuitBreakerStatus();
        const string errorMessage = "Connection refused";

        // Act
        status.RecordFailure(errorMessage);

        // Assert
        status.FailureCount.Should().Be(1);
        status.TotalFailures.Should().Be(1);
        status.TotalRequests.Should().Be(1);
        status.LastError.Should().Be(errorMessage);
        status.LastFailureAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordSuccess_ClearsLastError()
    {
        // Arrange
        var status = new CircuitBreakerStatus { LastError = "previous timeout" };

        // Act
        status.RecordSuccess();

        // Assert
        status.LastError.Should().BeNull();
    }

    [Fact]
    public void GetSuccessRate_WithNoRequests_ReturnsOne()
    {
        var status = new CircuitBreakerStatus();
        status.GetSuccessRate().Should().Be(1.0);
    }

    [Fact]
    public void GetSuccessRate_WithMixedRequests_CalculatesCorrectly()
    {
        // Arrange — 7 out of 10 requests succeeded
        var status = new CircuitBreakerStatus { TotalRequests = 10, TotalSuccesses = 7 };

        // Act / Assert
        status.GetSuccessRate().Should().BeApproximately(0.7, 0.001);
        status.GetFailureRate().Should().BeApproximately(0.3, 0.001);
    }

    [Fact]
    public void GetFailureRate_PlusSuccessRate_AlwaysEqualsOne()
    {
        var status = new CircuitBreakerStatus { TotalRequests = 20, TotalSuccesses = 13 };
        (status.GetSuccessRate() + status.GetFailureRate()).Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void ChangeState_ToClosed_ResetsFailureAndSuccessCounters()
    {
        // Arrange
        var status = new CircuitBreakerStatus
        {
            State = CircuitBreakerState.Open,
            FailureCount = 5,
            SuccessCount = 2
        };

        // Act
        status.ChangeState(CircuitBreakerState.Closed);

        // Assert
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
    }

    [Fact]
    public void ChangeState_ToHalfOpen_ResetsSuccessCountOnly()
    {
        // Arrange
        var status = new CircuitBreakerStatus
        {
            State = CircuitBreakerState.Open,
            FailureCount = 5,
            SuccessCount = 1
        };

        // Act
        status.ChangeState(CircuitBreakerState.HalfOpen);

        // Assert
        status.State.Should().Be(CircuitBreakerState.HalfOpen);
        status.SuccessCount.Should().Be(0);
        status.FailureCount.Should().Be(5); // failure count is preserved in HalfOpen
    }

    [Fact]
    public void ChangeState_SameState_DoesNotResetCounters()
    {
        // Arrange — already Closed with accumulated counts
        var status = new CircuitBreakerStatus
        {
            State = CircuitBreakerState.Closed,
            FailureCount = 2,
            SuccessCount = 3
        };

        // Act — calling ChangeState with the same state is a no-op
        status.ChangeState(CircuitBreakerState.Closed);

        // Assert
        status.FailureCount.Should().Be(2);
        status.SuccessCount.Should().Be(3);
    }

    [Fact]
    public void Reset_ClearsStateCountersAndError()
    {
        // Arrange
        var status = new CircuitBreakerStatus
        {
            State = CircuitBreakerState.Open,
            FailureCount = 8,
            SuccessCount = 1,
            LastError = "upstream timeout"
        };

        // Act
        status.Reset();

        // Assert
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
        status.LastError.Should().BeNull();
    }
}

public class CircuitBreakerServiceTests
{
    private static CircuitBreakerPolicy DefaultPolicy(int failureThreshold = 3, int successThreshold = 2) =>
        new() { FailureThreshold = failureThreshold, SuccessThreshold = successThreshold, TimeoutSeconds = 60 };

    [Fact]
    public async Task RecordFailure_BeyondThreshold_OpensCircuit()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(failureThreshold: 3);

        // Act — repeated failures push the failure count above the threshold
        await service.RecordFailureAsync("payment-service", "timeout", policy);
        await service.RecordFailureAsync("payment-service", "timeout", policy);
        await service.RecordFailureAsync("payment-service", "timeout", policy);

        // Assert
        var status = await service.GetStatusAsync("payment-service");
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task RecordSuccess_InHalfOpen_MeetsSuccessThreshold_ClosesCircuit()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(successThreshold: 2);

        // Seed a half-open circuit directly via the repository
        var status = await service.GetOrCreateStatusAsync("inventory-service");
        status.ChangeState(CircuitBreakerState.HalfOpen);
        await repository.UpdateAsync(status);

        // Act
        await service.RecordSuccessAsync("inventory-service", policy);
        await service.RecordSuccessAsync("inventory-service", policy);

        // Assert
        var finalStatus = await service.GetStatusAsync("inventory-service");
        finalStatus!.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task RecordFailure_InHalfOpen_ReopensCircuit()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy();

        var status = await service.GetOrCreateStatusAsync("email-service");
        status.ChangeState(CircuitBreakerState.HalfOpen);
        await repository.UpdateAsync(status);

        // Act — a single failure in HalfOpen transitions back to Open
        await service.RecordFailureAsync("email-service", "connection error", policy);

        // Assert
        var finalStatus = await service.GetStatusAsync("email-service");
        finalStatus!.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task CanAttempt_WhenPolicyDisabled_BypassesCircuitState()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var disabledPolicy = new CircuitBreakerPolicy { Enabled = false };

        // Force circuit into Open state
        var status = await service.GetOrCreateStatusAsync("cache-service");
        status.ChangeState(CircuitBreakerState.Open);
        await repository.UpdateAsync(status);

        // Act — disabled policy skips circuit check entirely
        var result = await service.CanAttemptAsync("cache-service", disabledPolicy);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetCircuit_OpensCircuit_RestoresClosed()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(failureThreshold: 3);

        // Open the circuit by recording failures
        await service.RecordFailureAsync("auth-service", "unavailable", policy);
        await service.RecordFailureAsync("auth-service", "unavailable", policy);
        await service.RecordFailureAsync("auth-service", "unavailable", policy);

        // Act
        await service.ResetCircuitAsync("auth-service");

        // Assert
        var status = await service.GetStatusAsync("auth-service");
        status!.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
    }

    /// <summary>
    /// Demonstrates mocking IRepository&lt;CircuitBreakerStatus&gt; to isolate the
    /// repository contract from any concrete storage implementation.
    /// </summary>
    [Fact]
    public async Task IRepository_MockSetup_ReturnsExpectedStatus()
    {
        // Arrange
        var expectedStatus = new CircuitBreakerStatus
        {
            ServiceName = "mocked-service",
            State = CircuitBreakerState.Open,
            FailureCount = 5
        };

        var mockRepo = new Mock<IRepository<CircuitBreakerStatus>>();
        mockRepo.Setup(r => r.GetByIdAsync(expectedStatus.Id))
                .ReturnsAsync(expectedStatus);
        mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new[] { expectedStatus });

        // Act
        var all = await mockRepo.Object.GetAllAsync();
        var retrieved = await mockRepo.Object.GetByIdAsync(expectedStatus.Id);

        // Assert
        all.Should().ContainSingle();
        retrieved.Should().NotBeNull();
        retrieved!.State.Should().Be(CircuitBreakerState.Open);
        retrieved.FailureCount.Should().Be(5);
        mockRepo.Verify(r => r.GetByIdAsync(expectedStatus.Id), Times.Once);
        mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
