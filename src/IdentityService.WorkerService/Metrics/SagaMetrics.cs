// FILE: SagaMetrics.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: M-IDENTITY-WORKER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using Prometheus;

namespace IdentityService.WorkerService.Metrics;

/// <summary>
/// BLOCK_PROMETHEUS_METRICS Prometheus metrics for saga orchestration observability.
/// Tracks: saga starts, completions, failures, DLQ publications, retry attempts, step durations.
/// </summary>
public sealed class SagaMetrics
{
    private readonly Counter _sagaStarted;
    private readonly Counter _sagaCompleted;
    private readonly Counter _sagaFailed;
    private readonly Counter _dlqPublished;
    private readonly Counter _keycloakRetries;
    private readonly Counter _keycloakRetriesExhausted;
    private readonly Counter _stepSuccess;
    private readonly Counter _stepFailure;
    private readonly Histogram _sagaDuration;
    private readonly Histogram _stepDuration;

    public SagaMetrics()
    {
        _sagaStarted = Metrics.CreateCounter(
            "identity_saga_started_total",
            "Total number of sagas started",
            new CounterConfiguration
            {
                LabelNames = new[] { "saga_type" }
            });

        _sagaCompleted = Metrics.CreateCounter(
            "identity_saga_completed_total",
            "Total number of sagas completed successfully",
            new CounterConfiguration
            {
                LabelNames = new[] { "saga_type" }
            });

        _sagaFailed = Metrics.CreateCounter(
            "identity_saga_failed_total",
            "Total number of sagas that failed",
            new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "failure_cause" }
            });

        _dlqPublished = Metrics.CreateCounter(
            "identity_dlq_published_total",
            "Total number of events published to DLQ",
            new CounterConfiguration
            {
                LabelNames = new[] { "failed_step" }
            });

        _keycloakRetries = Metrics.CreateCounter(
            "identity_keycloak_retries_total",
            "Total number of Keycloak HTTP retry attempts");

        _keycloakRetriesExhausted = Metrics.CreateCounter(
            "identity_keycloak_retries_exhausted_total",
            "Total number of times Keycloak retries were exhausted");

        _stepSuccess = Metrics.CreateCounter(
            "identity_saga_step_success_total",
            "Total number of successful saga steps",
            new CounterConfiguration
            {
                LabelNames = new[] { "step_name" }
            });

        _stepFailure = Metrics.CreateCounter(
            "identity_saga_step_failure_total",
            "Total number of failed saga steps",
            new CounterConfiguration
            {
                LabelNames = new[] { "step_name", "error_type" }
            });

        _sagaDuration = Metrics.CreateHistogram(
            "identity_saga_duration_seconds",
            "Saga execution duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "saga_type" },
                Buckets = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
            });

        _stepDuration = Metrics.CreateHistogram(
            "identity_saga_step_duration_seconds",
            "Saga step execution duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "step_name" },
                Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0 }
            });
    }

    /// <summary>
    /// Increment saga started counter.
    /// </summary>
    public void IncrementSagaStarted(string sagaType = "ProvisionUserSaga")
    {
        _sagaStarted.WithLabels(sagaType).Inc();
    }

    /// <summary>
    /// Increment saga completed counter.
    /// </summary>
    public void IncrementSagaCompleted(string sagaType = "ProvisionUserSaga")
    {
        _sagaCompleted.WithLabels(sagaType).Inc();
    }

    /// <summary>
    /// Increment saga failed counter with failure cause.
    /// </summary>
    public void IncrementSagaFailed(string sagaType, string failureCause)
    {
        _sagaFailed.WithLabels(sagaType, failureCause).Inc();
    }

    /// <summary>
    /// Increment DLQ published counter.
    /// </summary>
    public void IncrementDlqPublished(string failedStep)
    {
        _dlqPublished.WithLabels(failedStep).Inc();
    }

    /// <summary>
    /// Increment Keycloak retry counter.
    /// </summary>
    public void IncrementKeycloakRetry()
    {
        _keycloakRetries.Inc();
    }

    /// <summary>
    /// Increment Keycloak retry exhausted counter.
    /// </summary>
    public void IncrementKeycloakRetryExhausted()
    {
        _keycloakRetriesExhausted.Inc();
    }

    /// <summary>
    /// Increment step success counter.
    /// </summary>
    public void IncrementStepSuccess(string stepName)
    {
        _stepSuccess.WithLabels(stepName).Inc();
    }

    /// <summary>
    /// Increment step failure counter.
    /// </summary>
    public void IncrementStepFailure(string stepName, string errorType)
    {
        _stepFailure.WithLabels(stepName, errorType).Inc();
    }

    /// <summary>
    /// Record saga execution duration.
    /// </summary>
    public ITimer RecordSagaDuration(string sagaType = "ProvisionUserSaga")
    {
        return _sagaDuration.WithLabels(sagaType).NewTimer();
    }

    /// <summary>
    /// Record saga step execution duration.
    /// </summary>
    public ITimer RecordStepDuration(string stepName)
    {
        return _stepDuration.WithLabels(stepName).NewTimer();
    }
}
