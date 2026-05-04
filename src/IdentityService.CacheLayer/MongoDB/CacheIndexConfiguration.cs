using MongoDB.Driver;

namespace IdentityService.CacheLayer.MongoDB;

/// <summary>
/// BLOCK_CACHE_INDEX configuration for cache collections.
/// Creates TTL indexes for auto-expiry and query indexes for lookups.
/// </summary>
public static class CacheIndexConfiguration
{
    /// <summary>
    /// Ensures all needed indexes exist on the cache collections.
    /// Idempotent — safe to call on every startup.
    /// </summary>
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
