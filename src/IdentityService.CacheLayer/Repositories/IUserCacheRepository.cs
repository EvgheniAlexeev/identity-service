// FILE: IUserCacheRepository.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: Data repository pattern (M-IDENTITY)
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_IDENTITY

using IdentityService.CacheLayer.MongoDB;

namespace IdentityService.CacheLayer.Repositories;

/// <summary>
/// Repository for user cache entries with TTL-based invalidation.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-CACHE</para>
/// <para><strong>@purpose:</strong> Provides MongoDB cache repository interface for user identity data with TTL-based expiration</para>
/// <para><strong>@module-type:</strong> DATA_LAYER</para>
/// <para><strong>@depends:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@domain-concept:</strong> IUserCacheRepository</para>
/// <para><strong>@invariant:</strong> Cache populated by ProvisionUserSaga on completion</para>
/// <para><strong>@invariant:</strong> TTL range: 60-300 seconds from saga completion</para>
/// <para><strong>@invariant:</strong> Auto-cleanup of expired entries via MongoDB TTL index</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-CACHE-ID</para>
/// </remarks>
public interface IUserCacheRepository
{
    /// <summary>
    /// Get a user cache entry by user ID.
    /// Returns null if not found or expired.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetUserAsync</para>
    /// <para><strong>@param userId:</strong> User identifier</para>
    /// <para><strong>@return:</strong> UserCacheEntry from cache or null</para>
    /// <para><strong>@log-event:</strong> cache.repo.get-user-start {userId}</para>
    /// <para><strong>@log-event:</strong> cache.repo.get-user-hit {userId}</para>
    /// <para><strong>@log-event:</strong> cache.repo.get-user-miss {userId}</para>
    /// <para><strong>@trace-span:</strong> cache.repo.get-user</para>
    /// <para><strong>@pre-condition:</strong> userId != null</para>
    /// <para><strong>@post-condition:</strong> result == null || result.UserId == userId</para>
    /// <para><strong>@complexity:</strong> O(1) (index lookup)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: MongoDB)</para>
    /// </remarks>
    Task<UserCacheEntry?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Upsert a user cache entry (insert or replace).
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> UpsertAsync</para>
    /// <para><strong>@param entry:</strong> UserCacheEntry to cache</para>
    /// <para><strong>@log-event:</strong> cache.repo.upsert-start {userId}</para>
    /// <para><strong>@log-event:</strong> cache.repo.upsert-success {userId}</para>
    /// <para><strong>@trace-span:</strong> cache.repo.upsert</para>
    /// <para><strong>@pre-condition:</strong> entry != null && entry.UserId != null</para>
    /// <para><strong>@complexity:</strong> O(1) (index update)</para>
    /// <para><strong>@idempotent:</strong> NO (updates state)</para>
    /// </remarks>
    Task UpsertAsync(UserCacheEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Delete a user cache entry.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> DeleteAsync</para>
    /// <para><strong>@param userId:</strong> User ID to delete</para>
    /// <para><strong>@log-event:</strong> cache.repo.delete-start {userId}</para>
    /// <para><strong>@log-event:</strong> cache.repo.delete-success {userId}</para>
    /// <para><strong>@trace-span:</strong> cache.repo.delete</para>
    /// <para><strong>@complexity:</strong> O(1)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task DeleteAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get all non-expired user cache entries.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetAllActiveAsync</para>
    /// <para><strong>@return:</strong> List of UserCacheEntry, excluding expired entries</para>
    /// <para><strong>@log-event:</strong> cache.repo.get-all-active-start</para>
    /// <para><strong>@log-event:</strong> cache.repo.get-all-active-result {count}</para>
    /// <para><strong>@trace-span:</strong> cache.repo.get-all-active</para>
    /// <para><strong>@complexity:</strong> O(n) (full collection scan)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task<List<UserCacheEntry>> GetAllActiveAsync(CancellationToken ct = default);
}
