using IdentityService.Shared.Dtos;
using IdentityService.KeycloakAdapter.Models;

namespace IdentityService.KeycloakAdapter.Admin;

/// <summary>
/// BLOCK_KEYCLOAK_ADMIN client interface for Keycloak admin operations.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-KEYCLOAK</para>
/// <para><strong>@purpose:</strong> Adapter interface for Keycloak admin API operations (CreateUser, AssignRole, GetUser, DeleteUser)</para>
/// <para><strong>@module-type:</strong> INTEGRATION</para>
/// <para><strong>@depends:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@domain-concept:</strong> IKeycloakAdminClient</para>
/// <para><strong>@invariant:</strong> Admin operations use authenticated Keycloak client</para>
/// <para><strong>@invariant:</strong> All operations throw HttpException on API failure</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-KEYCLOAK-ID</para>
/// </remarks>
public interface IKeycloakAdminClient
{
    /// <summary>
    /// Create a user in Keycloak. Returns the Keycloak-generated user ID.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> CreateUserAsync</para>
    /// <para><strong>@param user:</strong> UserCreatedDto with email, name, credentials</para>
    /// <para><strong>@return:</strong> User ID assigned by Keycloak</para>
    /// <para><strong>@throws:</strong> HttpException — API call failed; ValidationException — user details invalid</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.create-user-start {email}</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.create-user-success {userId}</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.create-user-error {email} {error}</para>
    /// <para><strong>@trace-span:</strong> keycloak.admin.create-user</para>
    /// <para><strong>@pre-condition:</strong> user != null && user.Email != null</para>
    /// <para><strong>@post-condition:</strong> result != null && result.Length > 0</para>
    /// <para><strong>@complexity:</strong> O(1) (HTTP call)</para>
    /// <para><strong>@idempotent:</strong> NO</para>
    /// </remarks>
    Task<string> CreateUserAsync(UserCreatedDto user, CancellationToken ct = default);

    /// <summary>
    /// Assign a realm role to a user.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> AssignRoleAsync</para>
    /// <para><strong>@param userId:</strong> Keycloak user identifier</para>
    /// <para><strong>@param roleId:</strong> Keycloak role identifier to assign</para>
    /// <para><strong>@throws:</strong> HttpException — API call failed</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.assign-role-start {userId} {roleId}</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.assign-role-success {userId} {roleId}</para>
    /// <para><strong>@trace-span:</strong> keycloak.admin.assign-role</para>
    /// <para><strong>@complexity:</strong> O(1) (HTTP call)</para>
    /// <para><strong>@idempotent:</strong> NO</para>
    /// </remarks>
    Task AssignRoleAsync(string userId, string roleId, CancellationToken ct = default);

    /// <summary>
    /// Get a user by Keycloak user ID.
    /// Returns null if the user is not found.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetUserAsync</para>
    /// <para><strong>@param userId:</strong> Keycloak user identifier</para>
    /// <para><strong>@return:</strong> KeycloakUser details from Keycloak or null</para>
    /// <para><strong>@throws:</strong> NotFoundException — user not found; HttpException — API call failed</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.get-user-start {userId}</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.get-user-success {userId}</para>
    /// <para><strong>@trace-span:</strong> keycloak.admin.get-user</para>
    /// <para><strong>@complexity:</strong> O(1) (HTTP call)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task<KeycloakUser?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a user from Keycloak.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> DeleteUserAsync</para>
    /// <para><strong>@param userId:</strong> Keycloak user identifier to delete</para>
    /// <para><strong>@throws:</strong> HttpException — deletion failed</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.delete-user-start {userId}</para>
    /// <para><strong>@log-event:</strong> keycloak.admin.delete-user-success {userId}</para>
    /// <para><strong>@trace-span:</strong> keycloak.admin.delete-user</para>
    /// <para><strong>@complexity:</strong> O(1) (HTTP call)</para>
    /// <para><strong>@idempotent:</strong> NO</para>
    /// </remarks>
    Task DeleteUserAsync(string userId, CancellationToken ct = default);
}
