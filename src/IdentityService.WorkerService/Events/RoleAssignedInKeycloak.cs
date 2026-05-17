// FILE: RoleAssignedInKeycloak.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Internal saga event emitted when role assignment in Keycloak succeeds

namespace IdentityService.WorkerService.Events;

/// <summary>
/// Internal saga event emitted when a role has been successfully assigned in Keycloak.
/// Triggers the next SyncRoleSaga step: role cache update.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (internal saga event)</para>
/// <para><strong>@purpose:</strong> Internal event confirming role assignment in Keycloak during SyncRoleSaga</para>
/// <para><strong>@invariant:</strong> All properties are immutable after construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public record RoleAssignedInKeycloak
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime AssignedAt { get; init; }
}
