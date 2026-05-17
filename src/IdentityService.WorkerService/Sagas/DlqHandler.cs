// FILE: DlqHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Business logic handler (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.Shared.Events;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// Step handler processing Wolverine commands for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, processes Wolverine commands)</para>
/// <para><strong>@purpose:</strong> Step handler processing Wolverine commands for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

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
