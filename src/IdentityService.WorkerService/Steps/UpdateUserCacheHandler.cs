using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Logging;

namespace IdentityService.WorkerService.Steps;

/// <summary>
/// BLOCK_UPDATE_CACHE Step handler for updating the MongoDB user cache.
/// Populates the cache layer with provisioned user data, TTL-based auto-expiry.
/// </summary>
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
