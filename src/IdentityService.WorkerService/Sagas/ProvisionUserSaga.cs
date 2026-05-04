using IdentityService.Shared.Commands;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// BLOCK_PROVISION_USER_SAGA Wolverine saga orchestrator for user provisioning.
/// Flow: CreateInKeycloak (auto-retry) → UpdateCache → Notify → UserProvisioned
/// On ANY step failure: publish FailedIdentityEvent to DLQ with full audit context.
/// Manual compensation via DLQ only (no automatic compensation).
/// Role-based DLQ access: operators review failed saga state + original request.
/// </summary>
public class ProvisionUserSaga
{
    private readonly ILogger<ProvisionUserSaga> _logger;
    private readonly SagaMetrics _metrics;
    private readonly CreateUserInKeycloakHandler _createKeycloak;
    private readonly UpdateUserCacheHandler _updateCache;
    private readonly NotifyAdminsHandler _notify;

    public ProvisionUserSagaState Data { get; set; } = new();

    public ProvisionUserSaga(
        ILogger<ProvisionUserSaga> logger,
        SagaMetrics metrics,
        CreateUserInKeycloakHandler createKeycloak,
        UpdateUserCacheHandler updateCache,
        NotifyAdminsHandler notify)
    {
        _logger = logger;
        _metrics = metrics;
        _createKeycloak = createKeycloak;
        _updateCache = updateCache;
        _notify = notify;
    }

    // START_BLOCK_SAGA_START
    /// <summary>
    /// Saga entry point: receives ProvisionUserCommand and starts the orchestration.
    /// Sets up initial saga state and sends the first step command.
    /// </summary>
    public async Task<object> HandleAsync(
        ProvisionUserCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_SAGA_START] " +
            "Starting ProvisionUserSaga for {UserId} (correlationId={CorrelationId})",
            command.User.UserId, command.IdempotencyKey);

        _metrics.IncrementSagaStarted();

        // Initialize saga state
        Data.Id = command.IdempotencyKey;
        Data.CorrelationId = command.IdempotencyKey;
        Data.UserId = command.User.UserId;
        Data.User = command.User;
        Data.Status = "Provisioning";
        Data.CurrentStep = "CreateInKeycloak";
        Data.CreatedAt = DateTime.UtcNow;
        Data.RetryCount = 0;
        Data.MaxRetries = 3;

        // START_BLOCK_IDEMPOTENCY_CHECK
        if (!string.IsNullOrEmpty(Data.Id) && Data.AuditHistory.Any(a => a.Outcome == "Success" && a.StepName == "Completed"))
        {
            _logger.LogWarning(
                "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_IDEMPOTENCY_CHECK] " +
                "Duplicate command detected for {CorrelationId} — saga already completed",
                Data.CorrelationId);

            return new UserProvisioned
            {
                CorrelationId = Data.CorrelationId,
                UserId = Data.UserId,
                Email = Data.User.Email,
                AssignedRoles = Data.User.InitialRoles ?? new(),
                KeycloakUserId = Data.KeycloakUserId ?? string.Empty,
                ProvisionedAt = DateTime.UtcNow
            };
        }
        // END_BLOCK_IDEMPOTENCY_CHECK

        var stepCommand = new CreateUserInKeycloakCommand
        {
            CorrelationId = command.IdempotencyKey,
            User = command.User,
            Attempt = 1
        };

        return stepCommand;
    }
    // END_BLOCK_SAGA_START

    // START_BLOCK_SAGA_KEYCLOAK_RESPONSE
    /// <summary>
    /// Handles the Keycloak user creation response.
    /// On success: proceeds to UpdateCache.
    /// On failure: publishes to DLQ with full context.
    /// </summary>
    public async Task<object> HandleAsync(
        UserCreatedInKeycloak @event,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_SAGA_KEYCLOAK_RESPONSE] " +
            "User {UserId} created in Keycloak (KeycloakId={KeycloakUserId}), proceeding to cache update",
            @event.UserId, @event.KeycloakUserId);

        // Record audit
        Data.KeycloakUserId = @event.KeycloakUserId;
        Data.CurrentStep = "UpdateCache";
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "CreateInKeycloak",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        var cacheCommand = new UpdateUserCacheCommand
        {
            CorrelationId = @event.CorrelationId,
            UserId = @event.UserId,
            KeycloakUserId = @event.KeycloakUserId,
            Email = @event.Email,
            FirstName = @event.FirstName,
            LastName = @event.LastName,
            Roles = @event.Roles
        };

        return cacheCommand;
    }
    // END_BLOCK_SAGA_KEYCLOAK_RESPONSE

    // START_BLOCK_SAGA_CACHE_RESPONSE
    /// <summary>
    /// Handles the cache update response.
    /// On success: proceeds to Notify step.
    /// On failure: publishes to DLQ.
    /// </summary>
    public async Task<object> HandleAsync(
        CacheUpdated @event,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_SAGA_CACHE_RESPONSE] " +
            "Cache updated for {UserId}, proceeding to notification step",
            @event.UserId);

        Data.CurrentStep = "Notify";
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "UpdateCache",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        var notifyCommand = new NotifyCommand
        {
            CorrelationId = @event.CorrelationId,
            UserId = @event.UserId,
            Email = Data.User.Email,
            Status = "Provisioned"
        };

        return notifyCommand;
    }
    // END_BLOCK_SAGA_CACHE_RESPONSE

    // START_BLOCK_SAGA_COMPLETE
    /// <summary>
    /// Saga completion handler: marks saga as Provisioned, records final audit,
    /// emits UserProvisioned event, and increments completion metrics.
    /// </summary>
    public async Task<object> HandleAsync(
        NotifyCommand completedCommand,
        CancellationToken ct)
    {
        Data.Status = "Provisioned";
        Data.CompletedAt = DateTime.UtcNow;
        Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation(
            "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_SAGA_COMPLETE] " +
            "ProvisionUserSaga completed for {UserId}",
            Data.UserId);

        _metrics.IncrementSagaCompleted();

        var provisionedEvent = new UserProvisioned
        {
            CorrelationId = Data.CorrelationId,
            UserId = Data.UserId,
            Email = Data.User.Email,
            AssignedRoles = Data.User.InitialRoles ?? new(),
            KeycloakUserId = Data.KeycloakUserId ?? string.Empty,
            ProvisionedAt = DateTime.UtcNow
        };

        return provisionedEvent;
    }
    // END_BLOCK_SAGA_COMPLETE

    // START_BLOCK_SAGA_FAILURE
    /// <summary>
    /// BLOCK_SAGA_FAILURE Generic failure handler for any saga step failure.
    /// Publishes FailedIdentityEvent to DLQ with full context:
    /// - Original request preserved for retry
    /// - Failed step name
    /// - Error details
    /// - Full audit history
    /// No automatic compensation — manual DLQ review by operators.
    /// </summary>
    public async Task<object> HandleFailureAsync(
        Exception exception,
        CancellationToken ct)
    {
        Data.Status = "Failed";
        Data.CompletedAt = DateTime.UtcNow;
        Data.ErrorReason = exception.Message;

        var failureCause = exception switch
        {
            KeycloakAdapter.Admin.KeycloakException kc => kc.IsNotFound()
                ? IdentityFailureCause.UserAlreadyExists
                : IdentityFailureCause.KeycloakApiError,
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

        _logger.LogError(exception,
            "[IdentityService.WorkerService][ProvisionUserSaga][BLOCK_SAGA_FAILURE] " +
            "ProvisionUserSaga FAILED for {UserId} at step {CurrentStep}. Cause: {FailureCause}. " +
            "Publishing to DLQ for manual review.",
            Data.UserId, Data.CurrentStep, failureCause);

        _metrics.IncrementSagaFailed("ProvisionUserSaga", failureCause.ToString());
        _metrics.IncrementDlqPublished(Data.CurrentStep);

        // START_BLOCK_PUBLISH_DLQ
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = Data.CorrelationId,
            UserId = Data.UserId,
            OriginalRequest = Data.User,
            FailedStep = Data.CurrentStep,
            ErrorMessage = exception.Message,
            ErrorCode = Data.ErrorCode ?? "Unknown",
            RetryCount = Data.RetryCount,
            FailedAt = DateTime.UtcNow,
            Cause = failureCause
        };
        // END_BLOCK_PUBLISH_DLQ

        return dlqEvent;
    }
    // END_BLOCK_SAGA_FAILURE
}
