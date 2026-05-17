// FILE: ProvisionUserSagaState.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: M-IDENTITY-WORKER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Sagas;

/// <summary>
/// Saga state document persisted in MongoDB for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga state document, persists saga lifecycle to MongoDB)</para>
/// <para><strong>@purpose:</strong> Saga state document persisted in MongoDB for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Saga state captures all lifecycle data; TTL index auto-expires after configured duration</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public class ProvisionUserSagaState
{
    /// <summary>
    /// Unique saga identifier — set to Command.IdempotencyKey for deduplication.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Correlation ID tracked across all services.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>User ID being provisioned.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Current saga status (Provisioning, Provisioned, Failed).</summary>
    public string Status { get; set; } = "Provisioning";

    /// <summary>Current saga step identifier.</summary>
    public string CurrentStep { get; set; } = "CreateInKeycloak";

    /// <summary>User creation request payload.</summary>
    public UserCreatedDto User { get; set; } = new();

    /// <summary>Keycloak user ID assigned after creation (null until created).</summary>
    public string? KeycloakUserId { get; set; }

    /// <summary>Number of retry attempts on the current step.</summary>
    public int RetryCount { get; set; }

    /// <summary>Maximum number of retries allowed per step (from Polly config).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Error reason if saga failed.</summary>
    public string? ErrorReason { get; set; }

    /// <summary>Error code from the failed step.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Whether the saga was completed without errors.</summary>
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

/// <summary>
/// Immutable audit trail entry for a saga step execution.
/// </summary>
public record SagaStepAudit
{
    public string StepName { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
    public int DurationMs { get; init; }
}
