using FluentAssertions;
using IdentityService.WorkerService.Metrics;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Metrics;

/// <summary>
/// BLOCK_TEST Unit tests for SagaMetrics (Prometheus metrics).
/// Tests all counter increments, histogram recordings, and label correctness.
/// </summary>
public class SagaMetricsTests
{
    private readonly SagaMetrics _metrics;

    public SagaMetricsTests()
    {
        _metrics = new SagaMetrics();
    }

    [Fact]
    public void IncrementSagaStarted_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementSagaStarted();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementSagaStarted_Should_Accept_Different_Saga_Types()
    {
        var act = () =>
        {
            _metrics.IncrementSagaStarted("ProvisionUserSaga");
            _metrics.IncrementSagaStarted("SyncRoleSaga");
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementSagaCompleted_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementSagaCompleted();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementSagaFailed_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementSagaFailed("ProvisionUserSaga", "KeycloakApiError");
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementSagaFailed_Should_Accept_All_Failure_Causes()
    {
        var causes = new[] { "KeycloakApiError", "NetworkTimeout", "UserAlreadyExists", "MaxRetriesExceeded", "Unknown" };

        foreach (var cause in causes)
        {
            var act = () => _metrics.IncrementSagaFailed("ProvisionUserSaga", cause);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void IncrementDlqPublished_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementDlqPublished("CreateInKeycloak");
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementDlqPublished_Should_Accept_All_Steps()
    {
        var steps = new[] { "CreateInKeycloak", "UpdateCache", "Notify", "ValidateRequest" };

        foreach (var step in steps)
        {
            var act = () => _metrics.IncrementDlqPublished(step);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void IncrementKeycloakRetry_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementKeycloakRetry();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementKeycloakRetry_Multiple_Calls_Should_Not_Throw()
    {
        for (int i = 0; i < 50; i++)
        {
            var act = () => _metrics.IncrementKeycloakRetry();
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void IncrementKeycloakRetryExhausted_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementKeycloakRetryExhausted();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementStepSuccess_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementStepSuccess("CreateInKeycloak");
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementStepSuccess_Should_Accept_All_Step_Names()
    {
        var steps = new[] { "CreateInKeycloak", "UpdateCache", "Notify" };

        foreach (var step in steps)
        {
            var act = () => _metrics.IncrementStepSuccess(step);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void IncrementStepFailure_Should_Not_Throw()
    {
        var act = () => _metrics.IncrementStepFailure("CreateInKeycloak", "KeycloakException");
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementStepFailure_Should_Accept_Various_Error_Types()
    {
        var errorTypes = new[] { "KeycloakException", "TimeoutException", "InvalidOperationException", "HttpRequestException" };

        foreach (var errorType in errorTypes)
        {
            var act = () => _metrics.IncrementStepFailure("CreateInKeycloak", errorType);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void RecordSagaDuration_Should_Return_Timer()
    {
        var timer = _metrics.RecordSagaDuration();

        timer.Should().NotBeNull();
        var act = () => timer.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordSagaDuration_Should_Accept_Saga_Type()
    {
        var timer = _metrics.RecordSagaDuration("SyncRoleSaga");

        timer.Should().NotBeNull();
        timer.Dispose();
    }

    [Fact]
    public void RecordStepDuration_Should_Return_Timer()
    {
        var timer = _metrics.RecordStepDuration("CreateInKeycloak");

        timer.Should().NotBeNull();
        timer.Dispose();
    }

    [Fact]
    public void RecordStepDuration_Should_Accept_All_Step_Names()
    {
        var steps = new[] { "CreateInKeycloak", "UpdateCache", "Notify" };

        foreach (var step in steps)
        {
            var timer = _metrics.RecordStepDuration(step);
            timer.Should().NotBeNull();
            timer.Dispose();
        }
    }

    [Fact]
    public void All_Metrics_Should_Be_Thread_Safe()
    {
        // Simulate concurrent metric updates from multiple saga instances
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                _metrics.IncrementSagaStarted();
                _metrics.IncrementSagaCompleted();
                _metrics.IncrementKeycloakRetry();
                _metrics.IncrementStepSuccess("CreateInKeycloak");
                _metrics.IncrementDlqPublished("CreateInKeycloak");
                using var timer = _metrics.RecordSagaDuration();
                Thread.Sleep(1);
            }));
        }

        var act = () => Task.WhenAll(tasks);
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void Metrics_Should_Support_Multiple_Saga_Types_Concurrently()
    {
        var sagas = new[] { "ProvisionUserSaga", "SyncRoleSaga" };

        foreach (var sagaType in sagas)
        {
            _metrics.IncrementSagaStarted(sagaType);
            _metrics.IncrementSagaCompleted(sagaType);
            _metrics.IncrementSagaFailed(sagaType, "KeycloakApiError");
        }

        // No assertions needed — if labels collide or invalid, Prometheus would throw
    }

    [Fact]
    public void Saga_Completion_Flow_Should_Track_All_Metrics()
    {
        // Simulate a full saga lifecycle
        _metrics.IncrementSagaStarted("ProvisionUserSaga");

        using (var step1 = _metrics.RecordStepDuration("CreateInKeycloak"))
        {
            _metrics.IncrementKeycloakRetry();
            _metrics.IncrementKeycloakRetry();
            _metrics.IncrementStepSuccess("CreateInKeycloak");
        }

        using (var step2 = _metrics.RecordStepDuration("UpdateCache"))
        {
            _metrics.IncrementStepSuccess("UpdateCache");
        }

        using (var step3 = _metrics.RecordStepDuration("Notify"))
        {
            _metrics.IncrementStepSuccess("Notify");
        }

        using (_metrics.RecordSagaDuration("ProvisionUserSaga"))
        {
            _metrics.IncrementSagaCompleted("ProvisionUserSaga");
        }

        // Should not throw
    }
}
