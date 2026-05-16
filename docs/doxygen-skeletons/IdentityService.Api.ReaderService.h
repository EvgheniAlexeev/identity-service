/**
 * @contract M-READER-ID
 * @purpose Provides HTTP query endpoints for user retrieval from cache with fallback validation
 * @module-type ENTRY_POINT
 * @depends M-SHARED-ID, M-CACHE-ID
 * @verification-ref V-M-READER-ID
 * @semantic-domain Identity, Query, User Lookup
 * @invariant Cache hit ≥ 95%
 * @invariant Response latency p99 ≤ 50ms (cache hit)
 * @invariant 404 on cache miss (no Keycloak fallback)
 * @error-strategy NotFoundException, ValidationException, TimeoutException
 * @stability STABLE
 */

namespace IdentityService.Api.ReaderService
{
    /**
     * @domain-concept UsersController
     * @purpose Handles HTTP GET requests for user queries
     */
    public class UsersController
    {
        /**
         * @contract-action GetUser
         * @param userId User identifier
         * @return UserDocumentDto with user details
         * @throws NotFoundException — user not in cache
         * @throws ValidationException — userId invalid
         * @log-event reader.controller.get-user-start {userId}
         * @log-event reader.controller.get-user-cache-hit {userId}
         * @log-event reader.controller.get-user-cache-miss {userId}
         * @trace-span reader.get-user
         * @pre-condition userId != null && userId.Length > 0
         * @post-condition result != null
         * @complexity O(1) (cache lookup)
         * @idempotent YES
         * @http GET /api/users/{userId}
         * @http-response 200 UserDocumentDto
         * @http-response 404 User not found in cache
         */
        public Task<IActionResult> GetUser(string userId, CancellationToken ct);

        /**
         * @contract-action QueryUsers
         * @param request QueryUsersRequest with filters
         * @return Paginated QueryUsersResponse
         * @throws ValidationException — request invalid
         * @log-event reader.controller.query-users-start {skip} {take}
         * @log-event reader.controller.query-users-success {count}
         * @trace-span reader.query-users
         * @pre-condition request != null && request.Take <= 1000
         * @post-condition result.Items.Count <= request.Take
         * @complexity O(log n + k)
         * @idempotent YES
         * @http POST /api/users/query
         * @http-response 200 QueryUsersResponse
         */
        public Task<IActionResult> QueryUsers([FromBody] QueryUsersRequest request, CancellationToken ct);

        /**
         * @contract-action IntrospectToken
         * @param token JWT access token
         * @return TokenIntrospectionResult with claims
         * @throws UnauthorizedException — token invalid
         * @throws TokenExpiredException — token expired
         * @log-event reader.controller.introspect-token-start
         * @log-event reader.controller.introspect-token-success
         * @log-event reader.controller.introspect-token-error {error}
         * @trace-span reader.introspect-token
         * @pre-condition token != null && token.Length > 0
         * @post-condition result != null
         * @idempotent YES (validation only)
         * @http POST /api/introspect
         * @http-response 200 TokenIntrospectionResult
         */
        public Task<IActionResult> IntrospectToken([FromBody] TokenIntrospectionRequest request, CancellationToken ct);
    };

    /**
     * @domain-concept UserQueryService
     * @purpose Business logic for user queries
     */
    public class UserQueryService
    {
        /**
         * @contract-action GetUserAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @return UserDocumentDto from cache
         * @throws NotFoundException — user not in cache (no Keycloak fallback)
         * @log-event reader.service.get-user-cache-lookup {userId}
         * @log-event reader.service.get-user-cache-hit {userId}
         * @log-event reader.service.get-user-cache-miss {userId}
         * @trace-span reader.service.get-user
         * @pre-condition userId != null
         * @post-condition result != null
         * @complexity O(1)
         * @idempotent YES
         * @pure NO (I/O: cache)
         */
        public Task<UserDocumentDto> GetUserAsync(string userId, CancellationToken ct);

        /**
         * @contract-action QueryUsersAsync
         * @param request QueryUsersRequest with pagination
         * @param ct Cancellation token
         * @return QueryUsersResponse with results
         * @log-event reader.service.query-users-cache-scan {skip} {take}
         * @log-event reader.service.query-users-result {count}
         * @trace-span reader.service.query-users
         * @idempotent YES
         * @pure NO (I/O)
         */
        public Task<QueryUsersResponse> QueryUsersAsync(QueryUsersRequest request, CancellationToken ct);
    };
}
