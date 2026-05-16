// FILE: SagaStepCommands.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Domain command (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [COMMAND, MESSAGE]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Command to create a user in Keycloak as part of the ProvisionUserSaga.
/// </summary>
public record CreateUserInKeycloakCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public UserCreatedDto User { get; init; } = new();
    public int Attempt { get; init; } = 1;
}

/// <summary>
/// Command to update the user cache in MongoDB as part of the ProvisionUserSaga.
/// </summary>
public record UpdateUserCacheCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string>? Roles { get; init; }
}

/// <summary>
/// Command to notify administrators about provisioning completion.
/// </summary>
public record NotifyCommand
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
