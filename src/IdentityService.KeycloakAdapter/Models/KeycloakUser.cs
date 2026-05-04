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
