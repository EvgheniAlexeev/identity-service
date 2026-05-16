// FILE: RoleSynced.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Events;

/// <summary>
/// BLOCK_ROLE_SYNCED event — emitted when a role is synced to the cache.
/// </summary>
public record RoleSynced : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime SyncedAt { get; init; }
}
