// FILE: IRoleCacheRepository.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: Data repository pattern (M-IDENTITY)
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_IDENTITY

using IdentityService.CacheLayer.MongoDB;

namespace IdentityService.CacheLayer.Repositories;

/// <summary>
/// Repository for role cache entries with TTL-based invalidation.
/// </summary>
public interface IRoleCacheRepository
{
    /// <summary>
    /// Get a role cache entry by role ID.
    /// Returns null if not found or expired.
    /// </summary>
    Task<RoleCacheEntry?> GetByRoleIdAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Upsert a role cache entry (insert or replace).
    /// </summary>
    Task UpsertAsync(RoleCacheEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Delete a role cache entry.
    /// </summary>
    Task DeleteAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Get all non-expired role cache entries.
    /// </summary>
    Task<List<RoleCacheEntry>> GetAllActiveAsync(CancellationToken ct = default);
}
