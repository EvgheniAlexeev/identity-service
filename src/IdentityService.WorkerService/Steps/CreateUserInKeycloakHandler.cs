// FILE: CreateUserInKeycloakHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Keycloak integration (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.KeycloakAdapter.Admin;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// BLOCK_CREATE_IN_KEYCLOAK Step handler for creating a user in Keycloak.
/// Uses Polly retry policy (3x, exponential backoff 100ms-2s).
/// Emits UserCreatedInKeycloak or FailedIdentityEvent on failure.
/// </summary>
public class CreateUserInKeycloakHandler
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly ILogger<CreateUserInKeycloakHandler> _logger;
    private readonly SagaMetrics _metrics;

    public CreateUserInKeycloakHandler(
        IKeycloakAdminClient keycloakClient,
        ILogger<CreateUserInKeycloakHandler> logger,
        SagaMetrics metrics)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_API_CALL Handles the CreateUserInKeycloak saga step.
    /// Retries are handled by the Polly policy configured on IKeycloakAdminClient's HttpClient.
    /// </summary>
    public async Task<object> HandleAsync(
        CreateUserInKeycloakCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][CreateUserInKeycloakHandler][BLOCK_CREATE_IN_KEYCLOAK] " +
            "Creating user in Keycloak {UserId} (attempt {Attempt})",
            command.User.UserId, command.Attempt);

        using var timer = _metrics.RecordStepDuration("CreateInKeycloak");

        try
        {
            var keycloakUserId = await _keycloakClient.CreateUserAsync(
                command.User, ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][CreateUserInKeycloakHandler][BLOCK_CREATE_IN_KEYCLOAK] " +
                "User {UserId} created in Keycloak with KeycloakId {KeycloakUserId}",
                command.User.UserId, keycloakUserId);

            _metrics.IncrementStepSuccess("CreateInKeycloak");

            return new UserCreatedInKeycloak
            {
                CorrelationId = command.CorrelationId,
                UserId = command.User.UserId,
                KeycloakUserId = keycloakUserId,
                Email = command.User.Email,
                FirstName = command.User.FirstName,
                LastName = command.User.LastName,
                Roles = command.User.InitialRoles?.ToList(),
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (KeycloakException ex)
        {
            _logger.LogError(ex,
                "[IdentityService.WorkerService][CreateUserInKeycloakHandler][BLOCK_CREATE_IN_KEYCLOAK_ERROR] " +
                "Failed to create user in Keycloak {UserId}. Status: {StatusCode}",
                command.User.UserId, ex.StatusCode);

            _metrics.IncrementStepFailure("CreateInKeycloak", "KeycloakApiError");

            throw;
        }
    }
}
