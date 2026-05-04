using IdentityService.CacheLayer.MongoDB;

namespace IdentityService.CacheLayer.Repositories;

/// <summary>
/// Repository for user cache entries with TTL-based invalidation.
/// </summary>
public interface IUserCacheRepository
{
    /// <summary>
    /// Get a user cache entry by user ID.
    /// Returns null if not found or expired.
    /// </summary>
    Task<UserCacheEntry?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Upsert a user cache entry (insert or replace).
    /// </summary>
    Task UpsertAsync(UserCacheEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Delete a user cache entry.
    /// </summary>
    Task DeleteAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get all non-expired user cache entries.
    /// </summary>
    Task<List<UserCacheEntry>> GetAllActiveAsync(CancellationToken ct = default);
}
