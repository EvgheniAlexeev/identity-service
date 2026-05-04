using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Events;

/// <summary>
/// BLOCK_USER_CREATED_IN_KEYCLOAK event — internal saga event emitted after Keycloak user creation.
/// </summary>
public record UserCreatedInKeycloak
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string>? Roles { get; init; }
    public DateTime CreatedAt { get; init; }
}
