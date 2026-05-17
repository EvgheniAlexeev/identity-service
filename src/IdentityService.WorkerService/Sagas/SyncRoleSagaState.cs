// FILE: SyncRoleSagaState.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Saga state document for role sync operations

using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// Saga state document persisted in MongoDB for the SyncRoleSaga orchestrator.
/// Tracks role assignment lifecycle: AssigningInKeycloak -> UpdatingRoleCache -> InvalidatingUserCache -> Completed/Failed.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga state document, persists role sync lifecycle)</para>
/// <para><strong>@purpose:</strong> Saga state document for bidirectional role sync between Keycloak and MongoDB cache</para>
/// <para><strong>@invariant:</strong> Status transitions: AssigningInKeycloak -> UpdatingRoleCache -> InvalidatingUserCache -> Completed/Failed</para>
/// <para><strong>@invariant:</strong> TTL index auto-expires after configured duration</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public class SyncRoleSagaState
{
    /// <summary>Unique saga identifier — set to Command.IdempotencyKey for deduplication.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Correlation ID tracked across all services.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>User ID whose roles are being synced.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Current saga status.</summary>
    public string Status { get; set; } = "SyncPending";

    /// <summary>Current saga step identifier.</summary>
    public string CurrentStep { get; set; } = "AssignInKeycloak";

    /// <summary>Role assignment request payload.</summary>
    public RoleAssignDto Role { get; set; } = new();

    /// <summary>Keycloak user ID (populated during ProvisionUserSaga).</summary>
    public string? KeycloakUserId { get; set; }

    /// <summary>Number of retry attempts on the current step.</summary>
    public int RetryCount { get; set; }

    /// <summary>Maximum number of retries allowed per step.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Error reason if saga failed.</summary>
    public string? ErrorReason { get; set; }

    /// <summary>Error code from the failed step.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Whether the saga was compensated on failure.</summary>
    public bool WasCompensated { get; set; }

    /// <summary>Saga creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Saga completion timestamp (success or failure).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>TTL expiry timestamp for MongoDB auto-deletion (7 days).</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>Full audit history of saga step transitions.</summary>
    public List<SagaStepAudit> AuditHistory { get; set; } = new();
}
