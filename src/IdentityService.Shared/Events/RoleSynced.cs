// FILE: RoleSynced.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@purpose:</strong> Event emitted when a role is synced to the cache</para>
/// <para><strong>@invariant:</strong> CorrelationId must be non-empty</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_ROLE_SYNCED
public record RoleSynced : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime SyncedAt { get; init; }
}
