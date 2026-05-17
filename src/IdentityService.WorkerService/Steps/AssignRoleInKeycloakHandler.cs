// FILE: AssignRoleInKeycloakHandler.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Step handler for SyncRoleSaga — assigns role in Keycloak

using IdentityService.KeycloakAdapter.Admin;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Step handler for SyncRoleSaga: assigns a realm role to a user in Keycloak.
/// Retries are handled by the Polly policy configured on IKeycloakAdminClient's HttpClient.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, processes role assignment in Keycloak)</para>
/// <para><strong>@purpose:</strong> Handles the Keycloak role assignment step of SyncRoleSaga</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public class AssignRoleInKeycloakHandler
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly ILogger<AssignRoleInKeycloakHandler> _logger;
    private readonly SagaMetrics _metrics;

    public AssignRoleInKeycloakHandler(
        IKeycloakAdminClient keycloakClient,
        ILogger<AssignRoleInKeycloakHandler> logger,
        SagaMetrics metrics)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_KEYCLOAK_ROLE_ASSIGN Handles the AssignRoleInKeycloak saga step.
    /// Assigns a realm role to the user and returns an event for the next step.
    /// </summary>
    public async Task<object> HandleAsync(
        AssignRoleInKeycloakCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][AssignRoleInKeycloakHandler][BLOCK_KEYCLOAK_ROLE_ASSIGN] " +
            "Assigning role {RoleId} to Keycloak user {KeycloakUserId} (attempt {Attempt})",
            command.RoleId, command.KeycloakUserId, command.Attempt);

        using var timer = _metrics.RecordStepDuration("AssignRoleInKeycloak");

        try
        {
            await _keycloakClient.AssignRoleAsync(
                command.KeycloakUserId, command.RoleId, ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][AssignRoleInKeycloakHandler][BLOCK_KEYCLOAK_ROLE_ASSIGN] " +
                "Role {RoleId} assigned to Keycloak user {KeycloakUserId}",
                command.RoleId, command.KeycloakUserId);

            _metrics.IncrementStepSuccess("AssignRoleInKeycloak");

            return new RoleAssignedInKeycloak
            {
                CorrelationId = command.CorrelationId,
                UserId = command.UserId,
                RoleId = command.RoleId,
                RoleName = command.RoleName,
                AssignedAt = DateTime.UtcNow
            };
        }
        catch (KeycloakException ex)
        {
            _logger.LogError(ex,
                "[IdentityService.WorkerService][AssignRoleInKeycloakHandler][BLOCK_KEYCLOAK_ROLE_ASSIGN_ERROR] " +
                "Failed to assign role {RoleId} to Keycloak user {KeycloakUserId}. Status: {StatusCode}",
                command.RoleId, command.KeycloakUserId, ex.StatusCode);

            _metrics.IncrementStepFailure("AssignRoleInKeycloak", "KeycloakApiError");
            throw;
        }
    }
}
