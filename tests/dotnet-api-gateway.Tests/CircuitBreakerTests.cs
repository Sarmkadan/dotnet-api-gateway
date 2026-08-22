#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Constants;
using DotNetApiGateway.Exceptions;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

public sealed class CircuitBreakerStatusTests
{
    private readonly ILogger<CircuitBreakerStatusTests> _logger = NullLogger<CircuitBreakerStatusTests>.Instance;

    [Fact]
    public void RecordSuccess_InitialState_IncrementsAllCounters()
    {
        // Arrange
        var status = new CircuitBreakerStatus();
        _logger.LogInformation("Recording first success on a fresh CircuitBreakerStatus");

        // Act
        status.RecordSuccess();

        // Assert
        status.SuccessCount.Should().Be(1);
        status.TotalSuccesses.Should().Be(1);
        status.TotalRequests.Should().Be(1);
        status.LastError.Should().BeNull();
        status.LastSuccessAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        _logger.LogInformation(
            "Success recorded: SuccessCount={SuccessCount}, TotalSuccesses={TotalSuccesses}, TotalRequests={TotalRequests}",
            status.SuccessCount, status.TotalSuccesses, status.TotalRequests);
    }

    [Fact]
    public void RecordFailure_SetsLastErrorAndIncrements()
    {
        // Arrange
        var status = new CircuitBreakerStatus();
        const string errorMessage = "Connection refused";
        _logger.LogInformation("Recording failure with error {ErrorMessage}", errorMessage);

        // Act
        status.RecordFailure(errorMessage);

        // Assert
        status.FailureCount.Should().Be(1);
        status.TotalFailures.Should().Be(1);
        status.TotalRequests.Should().Be(1);
        status.LastError.Should().Be(errorMessage);
        status.LastFailureAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        _logger.LogInformation(
            "Failure recorded: FailureCount={FailureCount}, TotalFailures={TotalFailures}, LastError={LastError}",
            status.FailureCount, status.TotalFailures, status.LastError);
    }

    [Fact]
    public void RecordSuccess_ClearsLastError()
    {
        // Arrange
        var status = new CircuitBreakerStatus { LastError = "previous timeout" };
        _logger.LogInformation("Recording success to clear previous error {PreviousError}", status.LastError);

        // Act
        status.RecordSuccess();

        // Assert
        status.LastError.Should().BeNull();
        _logger.LogInformation("LastError after success: {LastError}", status.LastError);
    }

    [Fact]
    public void GetSuccessRate_WithNoRequests_ReturnsOne()
    {
        _logger.LogInformation("Computing success rate for a status with no recorded requests");
        var status = new CircuitBreakerStatus();
        status.GetSuccessRate().Should().Be(1.0);
        _logger.LogInformation("Success rate with no requests: {SuccessRate}", status.GetSuccessRate());
    }

    [Fact]
    public void GetSuccessRate_WithMixedRequests_CalculatesCorrectly()
    {
        // Arrange — 7 out of 10 requests succeeded
        var status = new CircuitBreakerStatus { TotalRequests = 10, TotalSuccesses = 7 };
        _logger.LogInformation(
            "Computing rates for {TotalRequests} requests with {TotalSuccesses} successes",
            status.TotalRequests, status.TotalSuccesses);

        // Act / Assert
        status.GetSuccessRate().Should().BeApproximately(0.7, 0.001);
        status.GetFailureRate().Should().BeApproximately(0.3, 0.001);
        _logger.LogInformation(
            "Computed rates: SuccessRate={SuccessRate}, FailureRate={FailureRate}",
            status.GetSuccessRate(), status.GetFailureRate());
    }

    [Fact]
    public void GetFailureRate_PlusSuccessRate_AlwaysEqualsOne()
    {
        var status = new CircuitBreakerStatus { TotalRequests = 20, TotalSuccesses = 13 };
        _logger.LogInformation(
            "Verifying rate complement for {TotalRequests} requests with {TotalSuccesses} successes",
            status.TotalRequests, status.TotalSuccesses);
        (status.GetSuccessRate() + status.GetFailureRate()).Should().BeApproximately(1.0, 0.001);
        _logger.LogInformation(
            "Success rate plus failure rate: {RateSum}",
            status.GetSuccessRate() + status.GetFailureRate());
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
        _logger.LogInformation(
            "Changing state from {PreviousState} to {NewState}",
            status.State, CircuitBreakerState.Closed);

        // Act
        status.ChangeState(CircuitBreakerState.Closed);

        // Assert
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
        _logger.LogInformation(
            "State changed to {State}: FailureCount={FailureCount}, SuccessCount={SuccessCount}",
            status.State, status.FailureCount, status.SuccessCount);
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
        _logger.LogInformation(
            "Changing state from {PreviousState} to {NewState}",
            status.State, CircuitBreakerState.HalfOpen);

        // Act
        status.ChangeState(CircuitBreakerState.HalfOpen);

        // Assert
        status.State.Should().Be(CircuitBreakerState.HalfOpen);
        status.SuccessCount.Should().Be(0);
        status.FailureCount.Should().Be(5); // failure count is preserved in HalfOpen
        _logger.LogInformation(
            "State changed to {State}: SuccessCount={SuccessCount}, FailureCount={FailureCount}",
            status.State, status.SuccessCount, status.FailureCount);
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
        _logger.LogInformation(
            "Calling ChangeState with the current state {State}; counters should be preserved",
            status.State);

        // Act — calling ChangeState with the same state is a no-op
        status.ChangeState(CircuitBreakerState.Closed);

        // Assert
        status.FailureCount.Should().Be(2);
        status.SuccessCount.Should().Be(3);
        _logger.LogInformation(
            "Counters preserved after no-op state change: FailureCount={FailureCount}, SuccessCount={SuccessCount}",
            status.FailureCount, status.SuccessCount);
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
        _logger.LogInformation(
            "Resetting circuit from {PreviousState} with FailureCount={FailureCount}, LastError={LastError}",
            status.State, status.FailureCount, status.LastError);

        // Act
        status.Reset();

        // Assert
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
        status.LastError.Should().BeNull();
        _logger.LogInformation("Circuit reset to {State}", status.State);
    }
}

public sealed class CircuitBreakerServiceTests
{
    private readonly CircuitBreakerRepository _repository;
    private readonly CircuitBreakerService _service;
    private readonly Mock<ILogger<CircuitBreakerService>> _loggerMock;
    private readonly CircuitBreakerPolicy _defaultPolicy;

    public CircuitBreakerServiceTests()
    {
        _repository = new CircuitBreakerRepository();
        _loggerMock = new Mock<ILogger<CircuitBreakerService>>();
        _service = new CircuitBreakerService(_repository, _loggerMock.Object);

        _defaultPolicy = new CircuitBreakerPolicy
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            TimeoutSeconds = 2,
            Enabled = true
        };

        _loggerMock.Object.LogInformation(
            "Initialized CircuitBreakerServiceTests with FailureThreshold={FailureThreshold}, SuccessThreshold={SuccessThreshold}, TimeoutSeconds={TimeoutSeconds}",
            _defaultPolicy.FailureThreshold, _defaultPolicy.SuccessThreshold, _defaultPolicy.TimeoutSeconds);
    }

    private static CircuitBreakerPolicy DefaultPolicy(int failureThreshold = 3, int successThreshold = 2) =>
        new() { FailureThreshold = failureThreshold, SuccessThreshold = successThreshold, TimeoutSeconds = 60 };

    [Fact]
    public async Task RecordFailure_BeyondThreshold_OpensCircuit()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(failureThreshold: 3);
        _loggerMock.Object.LogInformation(
            "Recording {FailureCount} consecutive failures for {ServiceName} with FailureThreshold={FailureThreshold}",
            3, "payment-service", policy.FailureThreshold);

        // Act — repeated failures push the failure count above the threshold
        await service.RecordFailureAsync("payment-service", "timeout", policy);
        await service.RecordFailureAsync("payment-service", "timeout", policy);
        await service.RecordFailureAsync("payment-service", "timeout", policy);

        // Assert
        var status = await service.GetStatusAsync("payment-service");
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} opened after {FailureCount} failures",
            "payment-service", status.FailureCount);
    }

    [Fact]
    public async Task RecordSuccess_InHalfOpen_MeetsSuccessThreshold_ClosesCircuit()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(successThreshold: 2);
        _loggerMock.Object.LogInformation(
            "Seeding {ServiceName} into {State} with SuccessThreshold={SuccessThreshold}",
            "inventory-service", CircuitBreakerState.HalfOpen, policy.SuccessThreshold);

        // Seed a half-open circuit directly via the repository
        var status = await service.GetOrCreateStatusAsync("inventory-service");
        status.ChangeState(CircuitBreakerState.HalfOpen);
        await repository.UpdateAsync(status);

        // Act
        _loggerMock.Object.LogInformation("Recording successes in HalfOpen for {ServiceName}", "inventory-service");
        await service.RecordSuccessAsync("inventory-service", policy);
        await service.RecordSuccessAsync("inventory-service", policy);

        // Assert
        var finalStatus = await service.GetStatusAsync("inventory-service");
        finalStatus!.State.Should().Be(CircuitBreakerState.Closed);
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} closed after meeting success threshold",
            "inventory-service");
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
        _loggerMock.Object.LogWarning(
            "Recording failure in HalfOpen for {ServiceName}; circuit is expected to reopen",
            "email-service");
        await service.RecordFailureAsync("email-service", "connection error", policy);

        // Assert
        var finalStatus = await service.GetStatusAsync("email-service");
        finalStatus!.State.Should().Be(CircuitBreakerState.Open);
        _loggerMock.Object.LogWarning(
            "Circuit for {ServiceName} reopened to {State} after a HalfOpen failure",
            "email-service", finalStatus.State);
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
        _loggerMock.Object.LogWarning(
            "Policy for {ServiceName} is disabled; bypassing circuit state {State}",
            "cache-service", CircuitBreakerState.Open);
        var result = await service.CanAttemptAsync("cache-service", disabledPolicy);

        // Assert
        result.Should().BeTrue();
        _loggerMock.Object.LogInformation(
            "CanAttempt with disabled policy returned {CanAttempt} for {ServiceName}",
            result, "cache-service");
    }

    [Fact]
    public async Task ResetCircuit_OpensCircuit_RestoresClosed()
    {
        // Arrange
        var repository = new CircuitBreakerRepository();
        var service = new CircuitBreakerService(repository);
        var policy = DefaultPolicy(failureThreshold: 3);
        _loggerMock.Object.LogInformation(
            "Opening circuit for {ServiceName} with FailureThreshold={FailureThreshold}",
            "auth-service", policy.FailureThreshold);

        // Open the circuit by recording failures
        await service.RecordFailureAsync("auth-service", "unavailable", policy);
        await service.RecordFailureAsync("auth-service", "unavailable", policy);
        await service.RecordFailureAsync("auth-service", "unavailable", policy);

        // Act
        _loggerMock.Object.LogInformation("Resetting circuit for {ServiceName}", "auth-service");
        await service.ResetCircuitAsync("auth-service");

        // Assert
        var status = await service.GetStatusAsync("auth-service");
        status!.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} restored to {State} with FailureCount={FailureCount}",
            "auth-service", status.State, status.FailureCount);
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
        _loggerMock.Object.LogInformation(
            "Setting up mocked repository for {ServiceName} in {State}",
            expectedStatus.ServiceName, expectedStatus.State);

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
        _loggerMock.Object.LogInformation(
            "Mock repository returned {ServiceName} in {State} with FailureCount={FailureCount}",
            retrieved.ServiceName, retrieved.State, retrieved.FailureCount);
    }

    [Fact]
    public async Task GetOrCreateStatusAsync_WhenServiceDoesNotExist_CreatesNewStatus()
    {
        // Arrange
        var serviceName = "test-service";
        _loggerMock.Object.LogInformation("Creating status for non-existent service {ServiceName}", serviceName);

        // Act
        var status = await _service.GetOrCreateStatusAsync(serviceName);

        // Assert
        status.Should().NotBeNull();
        status.ServiceName.Should().Be(serviceName);
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
        _loggerMock.Object.LogInformation(
            "Created status for {ServiceName}: State={State}, FailureCount={FailureCount}, SuccessCount={SuccessCount}",
            status.ServiceName, status.State, status.FailureCount, status.SuccessCount);
    }

    [Fact]
    public async Task GetOrCreateStatusAsync_WhenServiceExists_ReturnsExistingStatus()
    {
        // Arrange
        var serviceName = "test-service";
        var initialStatus = new CircuitBreakerStatus { ServiceName = serviceName };
        await _repository.AddAsync(initialStatus);
        _loggerMock.Object.LogInformation("Fetching existing status for {ServiceName}", serviceName);

        // Act
        var status = await _service.GetOrCreateStatusAsync(serviceName);

        // Assert
        status.Should().NotBeNull();
        status.ServiceName.Should().Be(serviceName);
        _loggerMock.Object.LogInformation("Retrieved existing status for {ServiceName}", status.ServiceName);
    }

    [Fact]
    public async Task IsCircuitOpenAsync_WhenCircuitIsClosed_ReturnsFalse()
    {
        // Arrange
        var serviceName = "test-service";
        _loggerMock.Object.LogInformation("Checking circuit state for {ServiceName}", serviceName);

        // Act
        var isOpen = await _service.IsCircuitOpenAsync(serviceName);

        // Assert
        isOpen.Should().BeFalse();
        _loggerMock.Object.LogInformation("Circuit {ServiceName} open: {IsOpen}", serviceName, isOpen);
    }

    [Fact]
    public async Task IsCircuitOpenAsync_WhenCircuitIsOpen_ReturnsTrue()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };
        _loggerMock.Object.LogInformation(
            "Opening circuit for {ServiceName} with FailureThreshold={FailureThreshold}",
            serviceName, policy.FailureThreshold);

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Act
        var isOpen = await _service.IsCircuitOpenAsync(serviceName);

        // Assert
        isOpen.Should().BeTrue();
        _loggerMock.Object.LogWarning("Circuit for {ServiceName} is open: {IsOpen}", serviceName, isOpen);
    }

    [Fact]
    public async Task CanAttemptAsync_WhenPolicyDisabled_ReturnsTrue()
    {
        // Arrange
        var serviceName = "test-service";
        var disabledPolicy = new CircuitBreakerPolicy { Enabled = false };
        _loggerMock.Object.LogWarning(
            "Policy for {ServiceName} is disabled; CanAttempt bypasses circuit state",
            serviceName);

        // Act
        var canAttempt = await _service.CanAttemptAsync(serviceName, disabledPolicy);

        // Assert
        canAttempt.Should().BeTrue();
        _loggerMock.Object.LogInformation("CanAttempt with disabled policy returned {CanAttempt}", canAttempt);
    }

    [Fact]
    public async Task CanAttemptAsync_WhenCircuitIsClosed_ReturnsTrue()
    {
        // Arrange
        var serviceName = "test-service";
        _loggerMock.Object.LogInformation("Checking CanAttempt for closed circuit {ServiceName}", serviceName);

        // Act
        var canAttempt = await _service.CanAttemptAsync(serviceName, _defaultPolicy);

        // Assert
        canAttempt.Should().BeTrue();
        _loggerMock.Object.LogInformation("CanAttempt for closed circuit returned {CanAttempt}", canAttempt);
    }

    [Fact]
    public async Task CanAttemptAsync_WhenCircuitIsOpenAndTimeoutNotElapsed_ThrowsCircuitBreakerException()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 60, Enabled = true };
        _loggerMock.Object.LogWarning(
            "Circuit for {ServiceName} is open; attempt within TimeoutSeconds={TimeoutSeconds} should throw",
            serviceName, policy.TimeoutSeconds);

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Act & Assert
        var act = () => _service.CanAttemptAsync(serviceName, policy);
        await act.Should().ThrowAsync<CircuitBreakerException>()
            .Where(e => e.ServiceName == serviceName)
            .Where(e => e.RetryAfterSeconds > 0);
        _loggerMock.Object.LogWarning(
            "CanAttempt for open circuit {ServiceName} threw CircuitBreakerException as expected",
            serviceName);
    }

    [Fact]
    public async Task CanAttemptAsync_WhenCircuitIsOpenAndTimeoutElapsed_TransitionsToHalfOpenAndReturnsTrue()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 1, Enabled = true };

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Wait for timeout to elapse
        _loggerMock.Object.LogInformation(
            "Waiting {DelayMilliseconds} ms for TimeoutSeconds={TimeoutSeconds} to elapse on {ServiceName}",
            1100, policy.TimeoutSeconds, serviceName);
        await Task.Delay(1100);

        // Act
        var canAttempt = await _service.CanAttemptAsync(serviceName, policy);

        // Assert
        canAttempt.Should().BeTrue();
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.HalfOpen);
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} transitioned to {State} after timeout elapsed",
            serviceName, status.State);
    }

    [Fact]
    public async Task CanAttemptAsync_WhenCircuitIsHalfOpen_ReturnsTrue()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Wait for timeout to elapse
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Act
        _loggerMock.Object.LogInformation("Probing half-open circuit for {ServiceName}", serviceName);
        var canAttempt = await _service.CanAttemptAsync(serviceName, policy);

        // Assert
        canAttempt.Should().BeTrue();
        _loggerMock.Object.LogInformation("CanAttempt for half-open circuit returned {CanAttempt}", canAttempt);
    }

    [Fact]
    public async Task RecordSuccessAsync_WhenPolicyDisabled_DoesNothing()
    {
        // Arrange
        var serviceName = "test-service";
        var disabledPolicy = new CircuitBreakerPolicy { Enabled = false };
        _loggerMock.Object.LogWarning(
            "Policy for {ServiceName} is disabled; RecordSuccess is a no-op",
            serviceName);

        // Act
        await _service.RecordSuccessAsync(serviceName, disabledPolicy);

        // Assert - no exception thrown, nothing happens
        // When policy is disabled, no status is created
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().BeNull();
        _loggerMock.Object.LogInformation(
            "No status created for {ServiceName}: StatusExists={StatusExists}",
            serviceName, status is not null);
    }

    [Fact]
    public async Task RecordSuccessAsync_WhenCircuitIsClosed_DecrementsFailureCount()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 5, Enabled = true };

        // Create some failures
        await _service.RecordFailureAsync(serviceName, "Error 1", policy);
        await _service.RecordFailureAsync(serviceName, "Error 2", policy);

        var statusBefore = await _service.GetStatusAsync(serviceName);
        statusBefore!.FailureCount.Should().Be(2);
        _loggerMock.Object.LogInformation(
            "Recording success for {ServiceName} with FailureCount={FailureCount}",
            serviceName, statusBefore.FailureCount);

        // Act
        await _service.RecordSuccessAsync(serviceName, policy);

        // Assert
        var statusAfter = await _service.GetStatusAsync(serviceName);
        statusAfter!.FailureCount.Should().Be(1);
        _loggerMock.Object.LogInformation(
            "FailureCount for {ServiceName} decremented to {FailureCount} after success",
            serviceName, statusAfter.FailureCount);
    }

    [Fact]
    public async Task RecordSuccessAsync_WhenCircuitIsHalfOpenAndSuccessThresholdMet_ClosesCircuit()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, SuccessThreshold = 2, TimeoutSeconds = 1, Enabled = true };
        _loggerMock.Object.LogInformation(
            "Driving {ServiceName} through HalfOpen with SuccessThreshold={SuccessThreshold}",
            serviceName, policy.SuccessThreshold);

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Wait for timeout to elapse
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Record first success
        await _service.RecordSuccessAsync(serviceName, policy);

        var statusAfterFirst = await _service.GetStatusAsync(serviceName);
        statusAfterFirst!.State.Should().Be(CircuitBreakerState.HalfOpen);
        statusAfterFirst.SuccessCount.Should().Be(1);
        _loggerMock.Object.LogInformation(
            "First HalfOpen success for {ServiceName}: State={State}, SuccessCount={SuccessCount}",
            serviceName, statusAfterFirst.State, statusAfterFirst.SuccessCount);

        // Record second success - should close circuit
        await _service.RecordSuccessAsync(serviceName, policy);

        // Assert
        var statusAfterSecond = await _service.GetStatusAsync(serviceName);
        statusAfterSecond!.State.Should().Be(CircuitBreakerState.Closed);
        statusAfterSecond.SuccessCount.Should().Be(0); // Reset on close
        statusAfterSecond.FailureCount.Should().Be(0); // Reset on close
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} closed after meeting success threshold: State={State}",
            serviceName, statusAfterSecond.State);
    }

    [Fact]
    public async Task RecordFailureAsync_WhenPolicyDisabled_DoesNothing()
    {
        // Arrange
        var serviceName = "test-service";
        var disabledPolicy = new CircuitBreakerPolicy { Enabled = false };
        _loggerMock.Object.LogWarning(
            "Policy for {ServiceName} is disabled; RecordFailure is a no-op",
            serviceName);

        // Act
        await _service.RecordFailureAsync(serviceName, "Test error", disabledPolicy);

        // Assert - no exception thrown, nothing happens
        // When policy is disabled, no status is created
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().BeNull();
        _loggerMock.Object.LogInformation(
            "No status created for {ServiceName}: StatusExists={StatusExists}",
            serviceName, status is not null);
    }

    [Fact]
    public async Task RecordFailureAsync_WhenCircuitIsClosedAndThresholdNotMet_IncrementsFailureCount()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 5, Enabled = true };
        _loggerMock.Object.LogInformation(
            "Recording failure for {ServiceName} with FailureThreshold={FailureThreshold}",
            serviceName, policy.FailureThreshold);

        // Act
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Assert
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.FailureCount.Should().Be(1);
        status.State.Should().Be(CircuitBreakerState.Closed);
        _loggerMock.Object.LogInformation(
            "Failure recorded for {ServiceName}: FailureCount={FailureCount}, State={State}",
            serviceName, status.FailureCount, status.State);
    }

    [Fact]
    public async Task RecordFailureAsync_WhenCircuitIsClosedAndThresholdMet_OpensCircuit()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 2, SuccessThreshold = 2, TimeoutSeconds = 60, Enabled = true };
        _loggerMock.Object.LogWarning(
            "Failure threshold {FailureThreshold} reached for {ServiceName}; circuit should open",
            policy.FailureThreshold, serviceName);

        // Create failures up to threshold
        await _service.RecordFailureAsync(serviceName, "Error 1", policy);
        await _service.RecordFailureAsync(serviceName, "Error 2", policy);

        // Assert
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.FailureCount.Should().Be(2);
        status.State.Should().Be(CircuitBreakerState.Open);
        _loggerMock.Object.LogWarning(
            "Circuit for {ServiceName} opened with FailureCount={FailureCount}",
            serviceName, status.FailureCount);
    }

    [Fact]
    public async Task RecordFailureAsync_WhenCircuitIsHalfOpen_ReopensCircuit()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, SuccessThreshold = 2, TimeoutSeconds = 1, Enabled = true };

        // Trigger circuit to open
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Wait for timeout to elapse
        await Task.Delay(1100);

        // Transition to HalfOpen
        await _service.CanAttemptAsync(serviceName, policy);

        // Record failure in HalfOpen state - should reopen
        _loggerMock.Object.LogWarning(
            "Recording failure in HalfOpen for {ServiceName}; circuit should reopen",
            serviceName);
        await _service.RecordFailureAsync(serviceName, "Test error in half-open", policy);

        // Assert
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        status!.State.Should().Be(CircuitBreakerState.Open);
        _loggerMock.Object.LogWarning(
            "Circuit for {ServiceName} reopened: State={State}",
            serviceName, status.State);
    }

    [Fact]
    public async Task GetOpenCircuitsAsync_ReturnsOnlyOpenCircuits()
    {
        // Arrange
        var service1 = "service-1";
        var service2 = "service-2";
        var service3 = "service-3";

        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };
        _loggerMock.Object.LogInformation(
            "Opening circuits for {Service1} and {Service3}; keeping {Service2} closed",
            service1, service3, service2);

        // Open circuit for service-1
        await _service.RecordFailureAsync(service1, "Error", policy);

        // Keep service-2 closed

        // Open circuit for service-3
        await _service.RecordFailureAsync(service3, "Error", policy);

        // Act
        var openCircuits = await _service.GetOpenCircuitsAsync();

        // Assert
        openCircuits.Should().HaveCount(2);
        openCircuits.Select(s => s.ServiceName).Should().Contain(service1);
        openCircuits.Select(s => s.ServiceName).Should().Contain(service3);
        openCircuits.Select(s => s.ServiceName).Should().NotContain(service2);
        _loggerMock.Object.LogInformation(
            "Found {OpenCircuitCount} open circuits",
            openCircuits.Count());
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectStatus()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Act
        _loggerMock.Object.LogInformation("Fetching status for {ServiceName}", serviceName);
        var status = await _service.GetStatusAsync(serviceName);

        // Assert
        status.Should().NotBeNull();
        status!.ServiceName.Should().Be(serviceName);
        status.State.Should().Be(CircuitBreakerState.Open);
        status.FailureCount.Should().Be(1);
        _loggerMock.Object.LogInformation(
            "Status for {ServiceName}: State={State}, FailureCount={FailureCount}",
            status.ServiceName, status.State, status.FailureCount);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ReturnsAllCircuits()
    {
        // Arrange
        var service1 = "service-1";
        var service2 = "service-2";

        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        await _service.RecordFailureAsync(service1, "Error", policy);
        await _service.RecordFailureAsync(service2, "Error", policy);

        // Act
        _loggerMock.Object.LogInformation("Fetching all circuit statuses");
        var allStatuses = await _service.GetAllStatusesAsync();

        // Assert
        allStatuses.Should().HaveCount(2);
        _loggerMock.Object.LogInformation(
            "Retrieved {StatusCount} circuit statuses",
            allStatuses.Count());
    }

    [Fact]
    public async Task ResetCircuitAsync_ResetsToClosedState()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        await _service.RecordFailureAsync(serviceName, "Test error", policy);
        var statusBefore = await _service.GetStatusAsync(serviceName);
        statusBefore!.State.Should().Be(CircuitBreakerState.Open);

        // Act
        _loggerMock.Object.LogInformation("Resetting circuit for {ServiceName}", serviceName);
        await _service.ResetCircuitAsync(serviceName);

        // Assert
        var statusAfter = await _service.GetStatusAsync(serviceName);
        statusAfter.Should().NotBeNull();
        statusAfter!.State.Should().Be(CircuitBreakerState.Closed);
        statusAfter.FailureCount.Should().Be(0);
        statusAfter.SuccessCount.Should().Be(0);
        _loggerMock.Object.LogInformation(
            "Circuit for {ServiceName} reset: State={State}, FailureCount={FailureCount}, SuccessCount={SuccessCount}",
            serviceName, statusAfter.State, statusAfter.FailureCount, statusAfter.SuccessCount);
    }

    [Fact]
    public async Task ResetAllCircuitsAsync_ResetsAllCircuits()
    {
        // Arrange
        var service1 = "service-1";
        var service2 = "service-2";

        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, Enabled = true };

        await _service.RecordFailureAsync(service1, "Error", policy);
        await _service.RecordFailureAsync(service2, "Error", policy);

        var openCircuitsBefore = await _service.GetOpenCircuitsAsync();
        openCircuitsBefore.Should().HaveCount(2);
        _loggerMock.Object.LogInformation(
            "Resetting all circuits; open circuits before reset: {OpenCircuitCount}",
            openCircuitsBefore.Count());

        // Act
        await _service.ResetAllCircuitsAsync();

        // Assert
        var allStatuses = await _service.GetAllStatusesAsync();
        allStatuses.Should().AllSatisfy(status =>
        {
            status.State.Should().Be(CircuitBreakerState.Closed);
            status.FailureCount.Should().Be(0);
            status.SuccessCount.Should().Be(0);
        });
        _loggerMock.Object.LogInformation(
            "All {StatusCount} circuits reset to {State}",
            allStatuses.Count(), CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ConcurrentAccess_ThreadSafeOperations()
    {
        // Arrange
        var serviceName = "concurrent-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 100, SuccessThreshold = 100, TimeoutSeconds = 1, Enabled = true };
        var tasks = new List<Task>();
        _loggerMock.Object.LogInformation(
            "Running {TaskCount} concurrent operation batches against {ServiceName}",
            10, serviceName);

        // Create many concurrent operations
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await _service.RecordFailureAsync(serviceName, "Error", policy);
                await _service.RecordSuccessAsync(serviceName, policy);
                await _service.CanAttemptAsync(serviceName, policy);
            }));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert - no exceptions thrown, operations completed successfully
        var status = await _service.GetStatusAsync(serviceName);
        status.Should().NotBeNull();
        _loggerMock.Object.LogInformation(
            "Concurrent operations completed for {ServiceName}: StatusExists={StatusExists}",
            serviceName, status is not null);
    }

    [Fact]
    public async Task CircuitBreakerException_ContainsCorrectInformation()
    {
        // Arrange
        var serviceName = "test-service";
        var policy = new CircuitBreakerPolicy { FailureThreshold = 1, TimeoutSeconds = 10, Enabled = true };
        _loggerMock.Object.LogWarning(
            "Opening circuit for {ServiceName}; expecting CircuitBreakerException",
            serviceName);
        await _service.RecordFailureAsync(serviceName, "Test error", policy);

        // Act
        CircuitBreakerException? exception = null;
        try
        {
            await _service.CanAttemptAsync(serviceName, policy);
        }
        catch (CircuitBreakerException ex)
        {
            _loggerMock.Object.LogError(
                ex,
                "Caught CircuitBreakerException for {ServiceName}: {ErrorMessage}",
                serviceName, ex.Message);
            exception = ex;
        }

        // Assert
        exception.Should().NotBeNull();
        exception!.ServiceName.Should().Be(serviceName);
        exception.RetryAfterSeconds.Should().BeGreaterThan(0);
        exception.Message.Should().Contain(serviceName);
        exception.Message.Should().Contain("CIRCUIT_BREAKER_OPEN");
        _loggerMock.Object.LogInformation(
            "CircuitBreakerException details: ServiceName={ServiceName}, RetryAfterSeconds={RetryAfterSeconds}",
            exception.ServiceName, exception.RetryAfterSeconds);
    }
}
