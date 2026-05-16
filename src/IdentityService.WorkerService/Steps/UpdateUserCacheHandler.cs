// FILE: UpdateUserCacheHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Business logic handler (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// Step handler processing Wolverine commands for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, processes Wolverine commands)</para>
/// <para><strong>@purpose:</strong> Step handler processing Wolverine commands for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public class UpdateUserCacheHandler
{
    private readonly IUserCacheRepository _cacheRepository;
    private readonly ILogger<UpdateUserCacheHandler> _logger;
    private readonly SagaMetrics _metrics;

    public UpdateUserCacheHandler(
        IUserCacheRepository cacheRepository,
        ILogger<UpdateUserCacheHandler> logger,
        SagaMetrics metrics)
    {
        _cacheRepository = cacheRepository;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// BLOCK_CACHE_UPDATE Handles the UpdateUserCache saga step.
    /// Populates the user cache with the provisioned user data.
    /// </summary>
    public async Task<object> HandleAsync(
        UpdateUserCacheCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.WorkerService][UpdateUserCacheHandler][BLOCK_UPDATE_CACHE] " +
            "Updating user cache for {UserId}",
            command.UserId);

        using var timer = _metrics.RecordStepDuration("UpdateCache");

        try
        {
            var entry = new UserCacheEntry
            {
                Id = command.UserId,
                UserId = command.UserId,
                Email = command.Email,
                Roles = command.Roles ?? new(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            await _cacheRepository.UpsertAsync(entry, ct);

            _logger.LogInformation(
                "[IdentityService.WorkerService][UpdateUserCacheHandler][BLOCK_UPDATE_CACHE] " +
                "User cache updated for {UserId}",
                command.UserId);

            _metrics.IncrementStepSuccess("UpdateCache");

            return new CacheUpdated
            {
                CorrelationId = command.CorrelationId,
                UserId = command.UserId,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.WorkerService][UpdateUserCacheHandler][BLOCK_UPDATE_CACHE_ERROR] " +
                "Failed to update cache for {UserId}",
                command.UserId);

            _metrics.IncrementStepFailure("UpdateCache", ex.GetType().Name);

            throw;
        }
    }
}
