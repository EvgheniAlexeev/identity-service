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
