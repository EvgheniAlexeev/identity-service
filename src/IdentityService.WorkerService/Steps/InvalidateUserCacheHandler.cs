// FILE: InvalidateUserCacheHandler.cs
// VERSION: 1.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Step handler for SyncRoleSaga — invalidates user cache after role change

using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Step handler for SyncRoleSaga: invalidates the user cache entry after role
/// assignment to force a fresh cache fetch on next query.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, invalidates user cache after role sync)</para>
/// <para><strong>@purpose:</strong> Invalidates user cache entry so reader picks up updated role assignments</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public class InvalidateUserCacheHandler
{
    private readonly IUserCacheRepository _userCacheRepository;
    private readonly ILogger<InvalidateUserCacheHandler> _logger;
    private readonly SagaMetrics _metrics;

    public InvalidateUserCacheHandler(
        IUserCacheRepository userCacheRepository,
        ILogger<InvalidateUserCacheHandler> logger,
        SagaMetrics metrics)
    {
        _userCacheRepository = userCacheRepository;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_INVALIDATE_USER_CACHE Handles the InvalidateUserCache saga step.
    /// Deletes the user cache entry so the Reader falls through to Keycloak.
    /// </summary>
    public async Task<object> HandleAsync(
        InvalidateUserCacheStepCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][InvalidateUserCacheHandler][BLOCK_INVALIDATE_USER_CACHE] " +
            "Invalidating user cache for {UserId} after role change",
            command.UserId);

        using var timer = _metrics.RecordStepDuration("InvalidateUserCache");

        try
        {
            await _userCacheRepository.DeleteAsync(command.UserId, ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][InvalidateUserCacheHandler][BLOCK_INVALIDATE_USER_CACHE] " +
                "User cache invalidated for {UserId}",
                command.UserId);

            _metrics.IncrementStepSuccess("InvalidateUserCache");

            // Return completion metadata back to saga
            return new
            {
                CorrelationId = command.CorrelationId,
                UserId = command.UserId,
                InvalidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.WorkerService][InvalidateUserCacheHandler][BLOCK_INVALIDATE_USER_CACHE_ERROR] " +
                "Failed to invalidate user cache for {UserId}",
                command.UserId);

            _metrics.IncrementStepFailure("InvalidateUserCache", ex.GetType().Name);
            throw;
        }
    }
}
