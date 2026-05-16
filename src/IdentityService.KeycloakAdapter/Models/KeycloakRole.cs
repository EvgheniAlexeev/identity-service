// FILE: KeycloakRole.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-KEYCLOAK
// PURPOSE: Keycloak integration (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_KEYCLOAK

namespace IdentityService.KeycloakAdapter.Models;

/// <summary>
/// Represents a Keycloak role.
/// </summary>
public record KeycloakRole
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Composite { get; init; }
    public bool ClientRole { get; init; }
}
