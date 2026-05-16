// FILE: SyncRoleCommand.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Domain command (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Commands;

/// <summary>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@purpose:</strong> Wolverine command to sync a role from Keycloak into cache</para>
/// <para><strong>@invariant:</strong> IdempotencyKey must be non-empty</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_SYNC_ROLE
public record SyncRoleCommand : ICommand
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public RoleAssignDto Role { get; init; } = new();
}
