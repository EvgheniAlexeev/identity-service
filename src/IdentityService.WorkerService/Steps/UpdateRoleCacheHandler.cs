// FILE: UpdateRoleCacheHandler.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Step handler for SyncRoleSaga — updates role cache in MongoDB

using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Step handler for SyncRoleSaga: updates the role cache entry in MongoDB after
/// successful role assignment in Keycloak.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, updates role cache in MongoDB)</para>
/// <para><strong>@purpose:</strong> Handles the role cache update step of SyncRoleSaga</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public class UpdateRoleCacheHandler
{
    private readonly IRoleCacheRepository _roleCacheRepository;
    private readonly ILogger<UpdateRoleCacheHandler> _logger;
    private readonly SagaMetrics _metrics;

    public UpdateRoleCacheHandler(
        IRoleCacheRepository roleCacheRepository,
        ILogger<UpdateRoleCacheHandler> logger,
        SagaMetrics metrics)
    {
        _roleCacheRepository = roleCacheRepository;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_UPDATE_ROLE_CACHE Handles the UpdateRoleCache saga step.
    /// Upserts the role cache entry with TTL-based expiry.
    /// </summary>
    public async Task<object> HandleAsync(
        UpdateRoleCacheStepCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][UpdateRoleCacheHandler][BLOCK_UPDATE_ROLE_CACHE] " +
            "Updating role cache for {RoleId}",
            command.RoleId);

        using var timer = _metrics.RecordStepDuration("UpdateRoleCache");

        try
        {
            var entry = new RoleCacheEntry
            {
                Id = command.RoleId,
                RoleId = command.RoleId,
                RoleName = command.RoleName,
                Permissions = command.Permissions,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            await _roleCacheRepository.UpsertAsync(entry, ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][UpdateRoleCacheHandler][BLOCK_UPDATE_ROLE_CACHE] " +
                "Role cache updated for {RoleId}",
                command.RoleId);

            _metrics.IncrementStepSuccess("UpdateRoleCache");

            return new RoleCacheUpdated
            {
                CorrelationId = command.CorrelationId,
                RoleId = command.RoleId,
                UserId = string.Empty, // populated from saga state
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.WorkerService][UpdateRoleCacheHandler][BLOCK_UPDATE_ROLE_CACHE_ERROR] " +
                "Failed to update role cache for {RoleId}",
                command.RoleId);

            _metrics.IncrementStepFailure("UpdateRoleCache", ex.GetType().Name);
            throw;
        }
    }
}
