using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Shared.Dtos;
using IdentityService.KeycloakAdapter.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.KeycloakAdapter.Admin;

/// <summary>
/// BLOCK_KEYCLOAK_API_CALL Keycloak Admin REST API client.
/// Handles user CRUD and role assignment.
/// </summary>
public class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfig _config;
    private readonly ILogger<KeycloakAdminClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KeycloakAdminClient(
        HttpClient httpClient,
        KeycloakConfig config,
        ILogger<KeycloakAdminClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_API_CALL Creates a user in Keycloak.
    /// </summary>
    public async Task<string> CreateUserAsync(UserCreatedDto user, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Creating Keycloak user {UserId}",
            user.UserId);

        var keycloakUser = new
        {
            username = user.Email,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            enabled = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.AdminUrl}/users",
            keycloakUser,
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Failed to create user {UserId}. Status: {StatusCode}",
                user.UserId, response.StatusCode);

            throw new KeycloakException(
                $"Failed to create user: {response.StatusCode}",
                (int)response.StatusCode);
        }

        // Keycloak returns the user ID in the Location header
        var locationHeader = response.Headers.Location?.ToString();
        var keycloakUserId = locationHeader?.Split('/').Last() ?? string.Empty;

        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] User created {UserId} -> KeycloakId {KeycloakUserId}",
            user.UserId, keycloakUserId);

        return keycloakUserId;
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_API_CALL Assigns a realm role to a user.
    /// </summary>
    public async Task AssignRoleAsync(string userId, string roleId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Assigning role {RoleId} to user {UserId}",
            roleId, userId);

        // Keycloak expects a list of role representations
        var rolePayload = new[]
        {
            new { id = roleId, name = "" }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.AdminUrl}/users/{userId}/role-mappings/realm",
            rolePayload,
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Failed to assign role {RoleId} to user {UserId}. Status: {StatusCode}",
                roleId, userId, response.StatusCode);

            throw new KeycloakException(
                $"Failed to assign role: {response.StatusCode}",
                (int)response.StatusCode);
        }
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_API_CALL Gets a user from Keycloak by ID.
    /// </summary>
    public async Task<KeycloakUser?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Getting Keycloak user {UserId}",
            userId);

        var response = await _httpClient.GetAsync(
            $"{_config.AdminUrl}/users/{userId}",
            ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] User not found {UserId}",
                userId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakException(
                $"Failed to get user: {response.StatusCode}",
                (int)response.StatusCode);
        }

        var user = await response.Content.ReadFromJsonAsync<KeycloakUser>(JsonOptions, ct);
        return user;
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_API_CALL Deletes a user from Keycloak.
    /// </summary>
    public async Task DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][KeycloakAdminClient][BLOCK_KEYCLOAK_API_CALL] Deleting Keycloak user {UserId}",
            userId);

        var response = await _httpClient.DeleteAsync(
            $"{_config.AdminUrl}/users/{userId}",
            ct);

        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw new KeycloakException(
                $"Failed to delete user: {response.StatusCode}",
                (int)response.StatusCode);
        }
    }
}
