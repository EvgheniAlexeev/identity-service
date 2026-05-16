// FILE: CacheIndexConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: Configuration and setup (M-IDENTITY)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY

using MongoDB.Driver;

namespace IdentityService.CacheLayer.MongoDB;

/// <summary>
/// BLOCK_CACHE_INDEX configuration for cache collections.
/// Creates TTL indexes for auto-expiry and query indexes for lookups.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-CACHE</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Configures MongoDB indexes for user and role cache with TTL-based auto-expiration</para>
/// <para><strong>@invariant:</strong> Creates TTL index on ExpiresAt (auto-cleanup)</para>
/// <para><strong>@invariant:</strong> Creates unique index on UserId and RoleId</para>
/// <para><strong>@verification-ref:</strong> V-M-CACHE</para>
/// </remarks>
public static class CacheIndexConfiguration
{
    /// <summary>
    /// Ensures all needed indexes exist on the cache collections.
    /// Idempotent — safe to call on every startup.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> EnsureIndexesAsync</para>
    /// <para><strong>@param database:</strong> IMongoDatabase instance</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-start</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-user-ttl</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-user-unique</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-role-ttl</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-role-unique</para>
    /// <para><strong>@log-event:</strong> cache.index.ensure-complete</para>
    /// <para><strong>@trace-span:</strong> cache.index.ensure</para>
    /// <para><strong>@complexity:</strong> O(1) (per-collection index creation)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    public static async Task EnsureIndexesAsync(IMongoDatabase database)
    {
        var userCacheCollection = database.GetCollection<UserCacheEntry>("user_cache");

        // TTL index: auto-delete documents after ExpiresAt
        await userCacheCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<UserCacheEntry>(
                Builders<UserCacheEntry>.IndexKeys.Ascending(u => u.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromSeconds(0) }));

        // Query index: UserId unique lookup
        await userCacheCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<UserCacheEntry>(
                Builders<UserCacheEntry>.IndexKeys.Ascending(u => u.UserId),
                new CreateIndexOptions { Unique = true }));

        var roleCacheCollection = database.GetCollection<RoleCacheEntry>("role_cache");

        // TTL index: auto-delete documents after ExpiresAt
        await roleCacheCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<RoleCacheEntry>(
                Builders<RoleCacheEntry>.IndexKeys.Ascending(r => r.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromSeconds(0) }));

        // Query index: RoleId unique lookup
        await roleCacheCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<RoleCacheEntry>(
                Builders<RoleCacheEntry>.IndexKeys.Ascending(r => r.RoleId),
                new CreateIndexOptions { Unique = true }));
    }
}
