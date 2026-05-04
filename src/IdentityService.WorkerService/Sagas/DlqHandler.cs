using IdentityService.Shared.Events;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// BLOCK_DLQ_HANDLER Dead-Letter Queue handler for failed identity operations.
/// Provides role-based DLQ access: operators review failed saga state and decide
/// whether to retry, manually intervene, or contact the customer.
/// Full audit history is preserved in the FailedIdentityEvent.
/// </summary>
public class DlqHandler
{
    private readonly ILogger<DlqHandler> _logger;

    public DlqHandler(ILogger<DlqHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// BLOCK_PUBLISH_DLQ Handles a FailedIdentityEvent from the DLQ.
    /// Logs the full event context for operator review.
    /// No automatic retry — operators decide disposition.
    /// </summary>
    public Task HandleAsync(FailedIdentityEvent dlqEvent, CancellationToken ct)
    {
        _logger.LogWarning(
            "[IdentityService.WorkerService][DlqHandler][BLOCK_PUBLISH_DLQ] " +
            "DLQ event received: {CorrelationId}, UserId: {UserId}, " +
            "FailedStep: {FailedStep}, Cause: {Cause}, " +
            "Retries: {RetryCount}, Error: {ErrorMessage}",
            dlqEvent.CorrelationId,
            dlqEvent.UserId,
            dlqEvent.FailedStep,
            dlqEvent.Cause,
            dlqEvent.RetryCount,
            dlqEvent.ErrorMessage);

        // DLQ disposition actions based on cause:
        var disposition = dlqEvent.Cause switch
        {
            IdentityFailureCause.KeycloakApiError => "Retry after Keycloak recovery",
            IdentityFailureCause.NetworkTimeout => "Retry with increased timeout",
            IdentityFailureCause.UserAlreadyExists => "Skip — idempotent",
            IdentityFailureCause.MaxRetriesExceeded => "Manual investigation required",
            IdentityFailureCause.ValidationError => "Reject — invalid request",
            _ => "Manual investigation required"
        };

        _logger.LogInformation(
            "[IdentityService.WorkerService][DlqHandler][BLOCK_PUBLISH_DLQ] " +
            "DLQ disposition for {CorrelationId}: {Disposition}",
            dlqEvent.CorrelationId, disposition);

        return Task.CompletedTask;
    }
}
