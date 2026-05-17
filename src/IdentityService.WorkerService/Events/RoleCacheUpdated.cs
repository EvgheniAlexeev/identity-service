// FILE: RoleCacheUpdated.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Internal saga event emitted when role cache has been updated

namespace IdentityService.WorkerService.Events;

/// <summary>
/// Internal saga event emitted when the role cache has been updated in MongoDB.
/// Part of SyncRoleSaga flow: Keycloak assign -> Cache update -> Invalidate user cache -> RoleSynced.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (internal saga event)</para>
/// <para><strong>@purpose:</strong> Internal event confirming role cache update during SyncRoleSaga</para>
/// <para><strong>@invariant:</strong> All properties are immutable after construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public record RoleCacheUpdated
{
    public string CorrelationId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
