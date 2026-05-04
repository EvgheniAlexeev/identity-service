using IdentityService.Shared.Dtos;
using IdentityService.KeycloakAdapter.Models;

namespace IdentityService.KeycloakAdapter.Admin;

/// <summary>
/// BLOCK_KEYCLOAK_ADMIN client interface for Keycloak admin operations.
/// </summary>
public interface IKeycloakAdminClient
{
    /// <summary>
    /// Create a user in Keycloak. Returns the Keycloak-generated user ID.
    /// </summary>
    Task<string> CreateUserAsync(UserCreatedDto user, CancellationToken ct = default);

    /// <summary>
    /// Assign a realm role to a user.
    /// </summary>
    Task AssignRoleAsync(string userId, string roleId, CancellationToken ct = default);

    /// <summary>
    /// Get a user by Keycloak user ID.
    /// Returns null if the user is not found.
    /// </summary>
    Task<KeycloakUser?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a user from Keycloak.
    /// </summary>
    Task DeleteUserAsync(string userId, CancellationToken ct = default);
}
