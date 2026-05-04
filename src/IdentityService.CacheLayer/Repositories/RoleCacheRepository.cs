using IdentityService.CacheLayer.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IdentityService.CacheLayer.Repositories;

/// <summary>
/// BLOCK_CACHE_ROLE MongoDB-backed role cache repository.
/// Uses TTL indexes for automatic expiration.
/// </summary>
public class RoleCacheRepository : IRoleCacheRepository
{
    private readonly CacheContext _context;
    private readonly ILogger<RoleCacheRepository> _logger;

    public RoleCacheRepository(CacheContext context, ILogger<RoleCacheRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RoleCacheEntry?> GetByRoleIdAsync(string roleId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][RoleCacheRepository][BLOCK_CACHE_GET] Getting role cache {RoleId}",
            roleId);

        var filter = Builders<RoleCacheEntry>.Filter.Eq(r => r.RoleId, roleId);
        var entry = await _context.RoleCache.Find(filter).FirstOrDefaultAsync(ct);

        if (entry == null)
        {
            _logger.LogInformation(
                "[IdentityService.CacheLayer][RoleCacheRepository][BLOCK_CACHE_MISS] Role cache miss {RoleId}",
                roleId);
        }

        return entry;
    }

    public async Task UpsertAsync(RoleCacheEntry entry, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][RoleCacheRepository][BLOCK_CACHE_UPSERT] Upserting role cache {RoleId}",
            entry.RoleId);

        var filter = Builders<RoleCacheEntry>.Filter.Eq(r => r.RoleId, entry.RoleId);
        var options = new ReplaceOptions { IsUpsert = true };

        await _context.RoleCache.ReplaceOneAsync(filter, entry, options, ct);
    }

    public async Task DeleteAsync(string roleId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][RoleCacheRepository][BLOCK_CACHE_DELETE] Deleting role cache {RoleId}",
            roleId);

        var filter = Builders<RoleCacheEntry>.Filter.Eq(r => r.RoleId, roleId);
        await _context.RoleCache.DeleteOneAsync(filter, ct);
    }

    public async Task<List<RoleCacheEntry>> GetAllActiveAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.CacheLayer][RoleCacheRepository][BLOCK_CACHE_GET_ALL] Getting all active role caches");

        var filter = Builders<RoleCacheEntry>.Filter.Gt(r => r.ExpiresAt, DateTime.UtcNow);
        return await _context.RoleCache.Find(filter).ToListAsync(ct);
    }
}
