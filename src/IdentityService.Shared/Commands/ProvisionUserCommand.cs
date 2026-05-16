// FILE: ProvisionUserCommand.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Domain command (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Commands;

/// <summary>
/// BLOCK_PROVISION_USER command — Wolverine command to provision a user
/// in Keycloak and populate the cache.
/// </summary>
public record ProvisionUserCommand : ICommand
{
    /// <summary>Idempotency key for deduplication.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>The user creation request payload.</summary>
    public UserCreatedDto User { get; init; } = new();
}

/// <summary>
/// Marker interface for Wolverine commands in the identity domain.
/// </summary>
public interface ICommand { }
