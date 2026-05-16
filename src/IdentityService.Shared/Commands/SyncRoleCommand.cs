// FILE: SyncRoleCommand.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Domain command (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Commands;

/// <summary>
/// BLOCK_SYNC_ROLE command — Wolverine command to sync a role
/// from Keycloak into the cache.
/// </summary>
public record SyncRoleCommand : ICommand
{
    /// <summary>Idempotency key for deduplication.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>The role to sync.</summary>
    public RoleAssignDto Role { get; init; } = new();
}
