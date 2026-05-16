/**
 * @contract M-KEYCLOAK-ID
 * @purpose Adapter for Keycloak admin API + JWT token validation with JWKS caching
 * @module-type INTEGRATION
 * @depends M-SHARED-ID
 * @verification-ref V-M-KEYCLOAK-ID
 * @semantic-domain Keycloak, Authentication, Token Validation
 * @invariant JWT validation issuer: configured Keycloak instance
 * @invariant JWKS cache TTL: 1 hour, stampede prevention enabled
 * @invariant Admin operations: CreateUser, AssignRole, DeleteUser, GetUser
 * @error-strategy HttpException, TokenValidationException, KeycloakAdminException
 * @stability STABLE
 */

namespace IdentityService.KeycloakAdapter
{
    /**
     * @domain-concept IKeycloakAdminClient
     * @purpose Interface for Keycloak admin operations
     */
    public interface IKeycloakAdminClient
    {
        /**
         * @contract-action CreateUserAsync
         * @param userCreated UserCreatedDto with details
         * @param ct Cancellation token
         * @return User ID assigned by Keycloak
         * @throws HttpException — API call failed
         * @throws ValidationException — user details invalid
         * @log-event keycloak.admin.create-user-start {email}
         * @log-event keycloak.admin.create-user-success {userId}
         * @log-event keycloak.admin.create-user-error {email} {error}
         * @trace-span keycloak.admin.create-user
         * @pre-condition userCreated != null && userCreated.Email != null
         * @post-condition result != null && result.Length > 0
         * @complexity O(1) (HTTP call)
         * @idempotent NO
         */
        Task<string> CreateUserAsync(UserCreatedDto userCreated, CancellationToken ct);

        /**
         * @contract-action AssignRoleAsync
         * @param userId User identifier
         * @param roleName Role name to assign
         * @param ct Cancellation token
         * @throws HttpException — API call failed
         * @log-event keycloak.admin.assign-role-start {userId} {roleName}
         * @log-event keycloak.admin.assign-role-success {userId} {roleName}
         * @trace-span keycloak.admin.assign-role
         * @idempotent NO
         */
        Task AssignRoleAsync(string userId, string roleName, CancellationToken ct);

        /**
         * @contract-action GetUserAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @return User details from Keycloak
         * @throws NotFoundException — user not found
         * @log-event keycloak.admin.get-user {userId}
         * @trace-span keycloak.admin.get-user
         * @idempotent YES
         */
        Task<UserDetails> GetUserAsync(string userId, CancellationToken ct);

        /**
         * @contract-action DeleteUserAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @throws HttpException — deletion failed
         * @log-event keycloak.admin.delete-user {userId}
         * @trace-span keycloak.admin.delete-user
         * @idempotent NO
         */
        Task DeleteUserAsync(string userId, CancellationToken ct);
    };

    /**
     * @domain-concept ITokenValidator
     * @purpose JWT token validation with JWKS
     */
    public interface ITokenValidator
    {
        /**
         * @contract-action ValidateAsync
         * @param token JWT access token
         * @param ct Cancellation token
         * @return TokenValidationResult with claims if valid
         * @throws TokenExpiredException — token has expired
         * @throws InvalidSignatureException — JWT signature invalid
         * @log-event keycloak.validator.validate-token-start
         * @log-event keycloak.validator.validate-token-success
         * @log-event keycloak.validator.validate-token-error {error}
         * @log-event keycloak.validator.validate-token-jwks-cache-hit
         * @log-event keycloak.validator.validate-token-jwks-cache-miss
         * @trace-span keycloak.validator.validate-token
         * @pre-condition token != null && token.Length > 0
         * @post-condition result != null
         * @complexity O(1) (cache-backed)
         * @idempotent YES
         * @pure NO (I/O: JWKS cache)
         */
        Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct);
    };

    /**
     * @domain-concept JwksCache
     * @purpose In-memory JWKS cache with TTL and stampede prevention
     * @invariant Cache TTL: 1 hour
     * @invariant Stampede prevention: single concurrent refresh
     */
    public class JwksCache
    {
        /**
         * @contract-action GetJwksAsync
         * @param httpClient HttpClient for JWKS fetch
         * @param jwksUrl Keycloak JWKS endpoint URL
         * @param ct Cancellation token
         * @return JsonWebKeySet from cache or refreshed
         * @log-event keycloak.cache.get-jwks-cache-hit
         * @log-event keycloak.cache.get-jwks-cache-miss
         * @log-event keycloak.cache.get-jwks-refresh-start
         * @log-event keycloak.cache.get-jwks-refresh-complete
         * @trace-span keycloak.cache.get-jwks
         * @pre-condition jwksUrl != null && httpClient != null
         * @post-condition result != null
         * @complexity O(1) (cache) or O(network) on expiry
         * @idempotent YES
         * @pure NO (I/O: HTTP + cache mutation)
         */
        public Task<JsonWebKeySet> GetJwksAsync(HttpClient httpClient, string jwksUrl, CancellationToken ct);
    };

    /**
     * @domain-concept KeycloakConfig
     * @value-object YES
     * @purpose Configuration record for Keycloak connection
     */
    public record KeycloakConfig
    {
        public string Url { get; init; }
        public string Realm { get; init; }
        public string ClientId { get; init; }
        public string ClientSecret { get; init; }

        /**
         * @contract-action DeriveIssuer
         * @return Derived issuer URL
         * @pure YES
         */
        public string DeriveIssuer() => $"{Url}/realms/{Realm}";

        /**
         * @contract-action DeriveJwksUrl
         * @return Derived JWKS endpoint URL
         * @pure YES
         */
        public string DeriveJwksUrl() => $"{DeriveIssuer()}/.well-known/jwks.json";
    };
}
