// FILE: SagaStepCommands.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Domain command (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Saga orchestrator for distributed transaction processing in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga orchestrator, manages distributed transaction lifecycle)</para>
/// <para><strong>@purpose:</strong> Saga orchestrator for distributed transaction processing in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Saga state transitions are deterministic; idempotency ensures exactly-once processing</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public record CreateUserInKeycloakCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public UserCreatedDto User { get; init; } = new();
    public int Attempt { get; init; } = 1;
}

/// <summary>
/// Command to update the user cache in MongoDB as part of the ProvisionUserSaga.
/// </summary>
public record UpdateUserCacheCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string>? Roles { get; init; }
}

/// <summary>
/// Command to notify administrators about provisioning completion.
/// </summary>
public record NotifyCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

// ──────────────────────────────────────────────────────────────
// SyncRoleSaga Step Commands
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Command to assign a role to a user in Keycloak as part of SyncRoleSaga.
/// </summary>
public record AssignRoleInKeycloakCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public int Attempt { get; init; } = 1;
}

/// <summary>
/// Command to update the role cache in MongoDB as part of SyncRoleSaga.
/// </summary>
public record UpdateRoleCacheStepCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
}

/// <summary>
/// Command to invalidate the user cache entry after role changes as part of SyncRoleSaga.
/// </summary>
public record InvalidateUserCacheStepCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
