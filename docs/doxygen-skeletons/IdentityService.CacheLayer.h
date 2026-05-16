/**
 * @contract M-CACHE-ID
 * @purpose Provides MongoDB cache layer for identity data with TTL-based expiration
 * @module-type DATA_LAYER
 * @depends M-SHARED-ID
 * @verification-ref V-M-CACHE-ID
 * @semantic-domain Caching, Identity Storage
 * @invariant Cache populated by ProvisionUserSaga on completion
 * @invariant TTL range: 60-300 seconds from saga completion
 * @invariant Auto-cleanup of expired entries via MongoDB TTL index
 * @error-strategy PersistenceException, TimeoutException
 * @stability STABLE
 */

namespace IdentityService.CacheLayer
{
    /**
     * @domain-concept IUserCacheRepository
     * @purpose Cache access for user documents
     */
    public interface IUserCacheRepository
    {
        /**
         * @contract-action GetUserAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @return UserDocument from cache or null
         * @log-event cache.repo.get-user {userId}
         * @log-event cache.repo.get-user-hit {userId}
         * @log-event cache.repo.get-user-miss {userId}
         * @trace-span cache.repo.get-user
         * @pre-condition userId != null
         * @post-condition result == null || result.UserId == userId
         * @complexity O(1)
         * @idempotent YES
         * @pure NO (I/O)
         */
        Task<UserDocument?> GetUserAsync(string userId, CancellationToken ct);

        /**
         * @contract-action UpsertUserAsync
         * @param user UserDocument to cache
         * @param ttlSeconds TTL in seconds (60-300)
         * @param session MongoDB session
         * @param ct Cancellation token
         * @log-event cache.repo.upsert-user {userId} ttl={ttlSeconds}s
         * @trace-span cache.repo.upsert-user
         * @pre-condition user != null && ttlSeconds >= 60 && ttlSeconds <= 300
         * @idempotent NO (updates state)
         */
        Task UpsertUserAsync(UserDocument user, int ttlSeconds, IClientSessionHandle session, CancellationToken ct);

        /**
         * @contract-action QueryUsersAsync
         * @param skip Skip count
         * @param take Take count
         * @param ct Cancellation token
         * @return List of UserDocuments
         * @log-event cache.repo.query-users {skip} {take}
         * @trace-span cache.repo.query-users
         * @complexity O(log n + k)
         * @idempotent YES
         */
        Task<List<UserDocument>> QueryUsersAsync(int skip, int take, CancellationToken ct);
    };

    /**
     * @domain-concept CacheIndexConfiguration
     * @contract-action EnsureIndexesAsync
     * @param database IMongoDatabase
     * @param ct Cancellation token
     * @log-event cache.index.ensure-start
     * @log-event cache.index.ensure-complete
     * @trace-span cache.index.ensure
     * @invariant Creates TTL index on ExpiresAt (auto-cleanup)
     * @invariant Creates unique index on UserId
     * @idempotent YES
     */
    public static class CacheIndexConfiguration
    {
        public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken ct);
    };
}
