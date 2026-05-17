// FILE: UserProvisioned.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Event emitted when a user is successfully provisioned in Keycloak and cache</para>
/// <para><strong>@invariant:</strong> CorrelationId must be non-empty</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_USER_PROVISIONED
public record UserProvisioned : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> AssignedRoles { get; init; } = new();
    public string KeycloakUserId { get; init; } = string.Empty;
    public DateTime ProvisionedAt { get; init; }
}
// END_BLOCK_USER_PROVISIONED
