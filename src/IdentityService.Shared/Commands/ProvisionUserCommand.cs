// FILE: ProvisionUserCommand.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Domain command (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Commands;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Wolverine command to provision a user in Keycloak and populate cache</para>
/// <para><strong>@invariant:</strong> IdempotencyKey must be non-empty</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_PROVISION_USER
public record ProvisionUserCommand : ICommand
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public UserCreatedDto User { get; init; } = new();
}

/// <summary>
/// Marker interface for Wolverine commands in the identity domain.
/// </summary>
public interface ICommand { }
// END_BLOCK_PROVISION_USER
