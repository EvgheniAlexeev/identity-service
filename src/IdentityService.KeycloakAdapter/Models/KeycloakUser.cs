// FILE: KeycloakUser.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-KEYCLOAK
// PURPOSE: Keycloak integration (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_KEYCLOAK

namespace IdentityService.KeycloakAdapter.Models;

/// <summary>
/// Represents a Keycloak user.
/// </summary>
public record KeycloakUser
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public long CreatedTimestamp { get; init; }
}
