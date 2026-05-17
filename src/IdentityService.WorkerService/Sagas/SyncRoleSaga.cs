// FILE: SyncRoleSaga.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Wolverine saga orchestrator for bidirectional role sync

using IdentityService.Shared.Commands;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// BLOCK_SYNC_ROLE_SAGA Wolverine saga orchestrator for bidirectional role synchronization.
/// Flow: AssignInKeycloak -> UpdateRoleCache -> InvalidateUserCache -> RoleSynced
/// On ANY step failure: publish RoleSyncFailedEvent to DLQ with full audit context.
/// Manual compensation via DLQ only (no automatic compensation).
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-WORKER</para>
/// <para><strong>@purpose:</strong> Saga orchestrator for role sync with Keycloak: Assign -> Cache -> Invalidate -> Complete</para>
/// <para><strong>@invariant:</strong> State: SyncPending -> AssigningInKeycloak -> UpdatingRoleCache -> InvalidatingUserCache -> Completed/Failed</para>
/// <para><strong>@invariant:</strong> Role cache populated with TTL on saga completion</para>
/// <para><strong>@invariant:</strong> User cache invalidated after role change to force fresh fetch</para>
/// <para><strong>@invariant:</strong> Failed operations published to DLQ with original request</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public class SyncRoleSaga
{
    private readonly ILogger<SyncRoleSaga> _logger;
    private readonly SagaMetrics _metrics;
    private readonly AssignRoleInKeycloakHandler _assignRoleKeycloak;
    private readonly UpdateRoleCacheHandler _updateRoleCache;
    private readonly InvalidateUserCacheHandler _invalidateUserCache;

    public SyncRoleSagaState Data { get; set; } = new();

    public SyncRoleSaga(
        ILogger<SyncRoleSaga> logger,
        SagaMetrics metrics,
        AssignRoleInKeycloakHandler assignRoleKeycloak,
        UpdateRoleCacheHandler updateRoleCache,
        InvalidateUserCacheHandler invalidateUserCache)
    {
        _logger = logger;
        _metrics = metrics;
        _assignRoleKeycloak = assignRoleKeycloak;
        _updateRoleCache = updateRoleCache;
        _invalidateUserCache = invalidateUserCache;
    }

    // START_BLOCK_SYNC_SAGA_START
    /// <summary>
    /// Saga entry point: receives SyncRoleCommand and starts the role sync orchestration.
    /// Sets up initial saga state and dispatches the Keycloak assign step.
    /// </summary>
    public Task<object> HandleAsync(
        SyncRoleCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_SAGA_START] " +
            "Starting SyncRoleSaga for user {UserId}, role {RoleId} (correlationId={CorrelationId})",
            command.Role.UserId, command.Role.RoleId, command.IdempotencyKey);

        _metrics.IncrementSagaStarted("SyncRoleSaga");

        // Initialize saga state
        Data.Id = command.IdempotencyKey;
        Data.CorrelationId = command.IdempotencyKey;
        Data.UserId = command.Role.UserId;
        Data.Role = command.Role;
        Data.Status = "SyncPending";
        Data.CurrentStep = "AssignInKeycloak";
        Data.CreatedAt = DateTime.UtcNow;
        Data.RetryCount = 0;
        Data.MaxRetries = 3;

        // START_BLOCK_SYNC_IDEMPOTENCY_CHECK
        if (!string.IsNullOrEmpty(Data.Id) &&
            Data.AuditHistory.Any(a => a.Outcome == "Success" && a.StepName == "Completed"))
        {
            _logger.LogWarning(
                "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_IDEMPOTENCY_CHECK] " +
                "Duplicate command detected for {CorrelationId} — saga already completed",
                Data.CorrelationId);

            return Task.FromResult<object>(new RoleSynced
            {
                CorrelationId = Data.CorrelationId,
                RoleId = Data.Role.RoleId,
                RoleName = Data.Role.RoleName,
                SyncedAt = DateTime.UtcNow
            });
        }
        // END_BLOCK_SYNC_IDEMPOTENCY_CHECK

        var stepCommand = new AssignRoleInKeycloakCommand
        {
            CorrelationId = command.IdempotencyKey,
            UserId = command.Role.UserId,
            KeycloakUserId = string.Empty, // resolved from saga state when known
            RoleId = command.Role.RoleId,
            RoleName = command.Role.RoleName,
            Attempt = 1
        };

        return Task.FromResult<object>(stepCommand);
    }
    // END_BLOCK_SYNC_SAGA_START

    // START_BLOCK_SYNC_KEYCLOAK_RESPONSE
    /// <summary>
    /// Handles the Keycloak role assignment response.
    /// On success: proceeds to UpdateRoleCache step.
    /// On failure: handled by HandleFailureAsync.
    /// </summary>
    public Task<object> HandleAsync(
        RoleAssignedInKeycloak @event,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_KEYCLOAK_RESPONSE] " +
            "Role {RoleId} assigned in Keycloak for user {UserId}, proceeding to cache update",
            @event.RoleId, @event.UserId);

        // Only update state; actual cache write happens in UpdateRoleCacheHandler
        Data.CurrentStep = "UpdateRoleCache";
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "AssignInKeycloak",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        var cacheCommand = new UpdateRoleCacheStepCommand
        {
            CorrelationId = @event.CorrelationId,
            RoleId = @event.RoleId,
            RoleName = @event.RoleName,
            Permissions = @event.Permissions
        };

        return Task.FromResult<object>(cacheCommand);
    }
    // END_BLOCK_SYNC_KEYCLOAK_RESPONSE

    // START_BLOCK_SYNC_CACHE_RESPONSE
    /// <summary>
    /// Handles the role cache update response.
    /// On success: proceeds to InvalidateUserCache step.
    /// On failure: handled by HandleFailureAsync.
    /// </summary>
    public Task<object> HandleAsync(
        RoleCacheUpdated @event,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_CACHE_RESPONSE] " +
            "Role cache updated for {RoleId}, proceeding to invalidate user cache for {UserId}",
            @event.RoleId, @event.UserId);

        Data.CurrentStep = "InvalidateUserCache";
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "UpdateRoleCache",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        var invalidateCommand = new InvalidateUserCacheStepCommand
        {
            CorrelationId = @event.CorrelationId,
            UserId = Data.UserId
        };

        return Task.FromResult<object>(invalidateCommand);
    }
    // END_BLOCK_SYNC_CACHE_RESPONSE

    // START_BLOCK_SYNC_COMPLETE
    /// <summary>
    /// Saga completion handler: marks saga as Completed, emits RoleSynced event.
    /// </summary>
    public Task<object> HandleAsync(
        InvalidateUserCacheStepCommand completedCommand,
        CancellationToken ct)
    {
        Data.Status = "Completed";
        Data.CompletedAt = DateTime.UtcNow;
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation(
            "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_COMPLETE] " +
            "SyncRoleSaga completed for user {UserId}, role {RoleId}",
            Data.UserId, Data.Role.RoleId);

        _metrics.IncrementSagaCompleted("SyncRoleSaga");

        var syncedEvent = new RoleSynced
        {
            CorrelationId = Data.CorrelationId,
            RoleId = Data.Role.RoleId,
            RoleName = Data.Role.RoleName,
            SyncedAt = DateTime.UtcNow
        };

        return Task.FromResult<object>(syncedEvent);
    }
    // END_BLOCK_SYNC_COMPLETE

    // START_BLOCK_SYNC_FAILURE
    /// <summary>
    /// BLOCK_SYNC_FAILURE Generic failure handler for any SyncRoleSaga step failure.
    /// Publishes RoleSyncFailedEvent to DLQ with full context:
    /// - Original request preserved for retry
    /// - Failed step name
    /// - Error details
    /// - Full audit history
    /// No automatic compensation — manual DLQ review by operators.
    /// </summary>
    public Task<object> HandleFailureAsync(
        Exception exception,
        CancellationToken ct)
    {
        Data.Status = "Failed";
        Data.CompletedAt = DateTime.UtcNow;
        Data.ErrorReason = exception.Message;

        var failureCause = exception switch
        {
            KeycloakAdapter.Admin.KeycloakException => IdentityFailureCause.KeycloakApiError,
            TimeoutException => IdentityFailureCause.NetworkTimeout,
            OperationCanceledException => IdentityFailureCause.NetworkTimeout,
            _ => IdentityFailureCause.Unknown
        };

        Data.ErrorCode = failureCause.ToString();
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = Data.CurrentStep,
            Outcome = "Failed",
            ErrorMessage = exception.Message,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogError(
            "[IdentityService.WorkerService][SyncRoleSaga][BLOCK_SYNC_FAILURE] " +
            "SyncRoleSaga failed for user {UserId}, role {RoleId}. " +
            "Step: {Step}, Cause: {Cause}, Error: {ErrorMessage}, " +
            "AuditSteps: {AuditCount}",
            Data.UserId, Data.Role.RoleId,
            Data.CurrentStep, failureCause, exception.Message,
            Data.AuditHistory.Count);

        _metrics.IncrementSagaFailed("SyncRoleSaga", failureCause.ToString());
        _metrics.IncrementDlqPublished(Data.CurrentStep);

        var failedEvent = new RoleSyncFailedEvent
        {
            CorrelationId = Data.CorrelationId,
            UserId = Data.UserId,
            OriginalRequest = Data.Role,
            FailedStep = Data.CurrentStep,
            ErrorMessage = exception.Message,
            ErrorCode = failureCause.ToString(),
            RetryCount = Data.RetryCount,
            FailedAt = DateTime.UtcNow,
            Cause = failureCause
        };

        return Task.FromResult<object>(failedEvent);
    }
    // END_BLOCK_SYNC_FAILURE
}
