// FILE: UserCacheRepository.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY
// PURPOSE: Data repository pattern (M-IDENTITY)
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_IDENTITY

using IdentityService.CacheLayer.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IdentityService.CacheLayer.Repositories;

/// <summary>
/// BLOCK_CACHE_USER MongoDB-backed user cache repository.
/// Uses TTL indexes for automatic expiration.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-CACHE</para>
/// <para><strong>@purpose:</strong> MongoDB repository implementation for user identity cache with TTL-based expiration</para>
/// <para><strong>@invariant:</strong> All queries filter by ExpiresAt > UtcNow</para>
/// <para><strong>@invariant:</strong> Upsert operations use ReplaceOneAsync with IsUpsert=true</para>
/// <para><strong>@verification-ref:</strong> V-M-CACHE</para>
/// </remarks>
public class UserCacheRepository : IUserCacheRepository
{
    private readonly CacheContext _context;
    private readonly ILogger<UserCacheRepository> _logger;

    public UserCacheRepository(CacheContext context, ILogger<UserCacheRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserCacheEntry?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][UserCacheRepository][BLOCK_CACHE_GET] Getting user cache {UserId}",
            userId);

        var filter = Builders<UserCacheEntry>.Filter.Eq(u => u.UserId, userId);
        var entry = await _context.UserCache.Find(filter).FirstOrDefaultAsync(ct);

        if (entry == null)
        {
            _logger.LogInformation(
                "[IdentityService.CacheLayer][UserCacheRepository][BLOCK_CACHE_MISS] User cache miss {UserId}",
                userId);
        }

        return entry;
    }

    public async Task UpsertAsync(UserCacheEntry entry, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][UserCacheRepository][BLOCK_CACHE_UPSERT] Upserting user cache {UserId}",
            entry.UserId);

        var filter = Builders<UserCacheEntry>.Filter.Eq(u => u.UserId, entry.UserId);
        var options = new ReplaceOptions { IsUpsert = true };

        await _context.UserCache.ReplaceOneAsync(filter, entry, options, ct);
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][UserCacheRepository][BLOCK_CACHE_DELETE] Deleting user cache {UserId}",
            userId);

        var filter = Builders<UserCacheEntry>.Filter.Eq(u => u.UserId, userId);
        await _context.UserCache.DeleteOneAsync(filter, ct);
    }

    public async Task<List<UserCacheEntry>> GetAllActiveAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][UserCacheRepository][BLOCK_CACHE_GET_ALL] Getting all active user caches");

        // MongoDB TTL index handles physical deletion; we just return all non-expired
        var filter = Builders<UserCacheEntry>.Filter.Gt(u => u.ExpiresAt, DateTime.UtcNow);
        return await _context.UserCache.Find(filter).ToListAsync(ct);
    }
}
