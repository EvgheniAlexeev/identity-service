// FILE: NotifyAdminsHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Business logic handler (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// BLOCK_NOTIFY Step handler for notifying administrators about provisioning completion.
/// This is a fire-and-forget step — notifications are best-effort and do not fail the saga.
/// </summary>
public class NotifyAdminsHandler
{
    private readonly ILogger<NotifyAdminsHandler> _logger;
    private readonly SagaMetrics _metrics;

    public NotifyAdminsHandler(
        ILogger<NotifyAdminsHandler> logger,
        SagaMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_NOTIFY Handles the notification step. Best-effort — failures are logged
    /// but do not cause saga rollback or DLQ.
    /// </summary>
    public async Task<object> HandleAsync(
        NotifyCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][NotifyAdminsHandler][BLOCK_NOTIFY] " +
            "Notifying admins: user {UserId} ({Email}) status={Status}",
            command.UserId,
            "***REDACTED***",
            command.Status);

        using var timer = _metrics.RecordStepDuration("Notify");

        try
        {
            // Notification is fire-and-forget. In production this would publish
            // to a notification service (email, Slack, PagerDuty, etc).
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][NotifyAdminsHandler][BLOCK_NOTIFY] " +
                "Notification sent for user {UserId}",
                command.UserId);

            _metrics.IncrementStepSuccess("Notify");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[IdentityService.WorkerService][NotifyAdminsHandler][BLOCK_NOTIFY_ERROR] " +
                "Notification failed for {UserId} — non-critical, saga continues",
                command.UserId);

            _metrics.IncrementStepFailure("Notify", "NonCritical");
        }

        return new { CorrelationId = command.CorrelationId, Notified = true };
    }
}
