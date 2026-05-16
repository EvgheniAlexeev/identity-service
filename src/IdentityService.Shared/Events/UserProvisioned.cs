// FILE: UserProvisioned.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Events;

/// <summary>
/// BLOCK_USER_PROVISIONED event — emitted when a user is successfully
/// provisioned in Keycloak and cache.
/// </summary>
public record UserProvisioned : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> AssignedRoles { get; init; } = new();
    public string KeycloakUserId { get; init; } = string.Empty;
    public DateTime ProvisionedAt { get; init; }
}
