// FILE: CacheContext.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: Caching layer (M-IDENTITY)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY

using MongoDB.Driver;

namespace IdentityService.CacheLayer.MongoDB;

/// <summary>
/// MongoDB context wrapper for cache databases.
/// Provides access to user_cache and role_cache collections.
/// </summary>
public class CacheContext
{
    private readonly IMongoDatabase _database;

    public CacheContext(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoCollection<UserCacheEntry> UserCache =>
        _database.GetCollection<UserCacheEntry>("user_cache");

    public IMongoCollection<RoleCacheEntry> RoleCache =>
        _database.GetCollection<RoleCacheEntry>("role_cache");

    public async Task InitializeAsync()
    {
        await CacheIndexConfiguration.EnsureIndexesAsync(_database);
    }
}
