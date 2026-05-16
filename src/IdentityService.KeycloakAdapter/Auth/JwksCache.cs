// FILE: JwksCache.cs
// VERSION: 2.0.0
// MODULE: M-KEYCLOAK
// PURPOSE: Caching layer (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [AUTH, SECURITY]
// START_MODULE M_IDENTITY_KEYCLOAK

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// BLOCK_JWKS_CACHE In-memory JWKS (JSON Web Key Set) cache.
/// Caches Keycloak's public keys for 1 hour by default.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-KEYCLOAK</para>
/// <para><strong>@purpose:</strong> In-memory JWKS cache with TTL and stampede prevention for JWT validation</para>
/// <para><strong>@invariant:</strong> Cache TTL: 1 hour (configurable)</para>
/// <para><strong>@invariant:</strong> Stampede prevention: single concurrent refresh via SemaphoreSlim</para>
/// </remarks>
public class JwksCache
{
    private JsonWebKeySet? _jwks;
    private DateTime _expiresAt = DateTime.MinValue;
    private readonly int _cacheTtlSeconds;
    private readonly ILogger<JwksCache> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JwksCache(ILogger<JwksCache> logger, int cacheTtlSeconds = 3600)
    {
        _logger = logger;
        _cacheTtlSeconds = cacheTtlSeconds;
    }

    /// <summary>
    /// BLOCK_JWKS_CACHE_CHECK Returns cached JWKS if still valid.
    /// BLOCK_JWKS_CACHE_REFRESH Fetches from Keycloak if expired.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetJwksAsync</para>
    /// <para><strong>@param httpClient:</strong> HttpClient for JWKS fetch</para>
    /// <para><strong>@param jwksUrl:</strong> Keycloak JWKS endpoint URL</para>
    /// <para><strong>@return:</strong> JsonWebKeySet from cache or refreshed from Keycloak</para>
    /// <para><strong>@log-event:</strong> keycloak.cache.get-jwks-cache-hit</para>
    /// <para><strong>@log-event:</strong> keycloak.cache.get-jwks-cache-hit-after-lock</para>
    /// <para><strong>@log-event:</strong> keycloak.cache.get-jwks-refresh-start {jwksUrl}</para>
    /// <para><strong>@log-event:</strong> keycloak.cache.get-jwks-refresh-complete {keyCount} {expiresAt}</para>
    /// <para><strong>@trace-span:</strong> keycloak.cache.get-jwks</para>
    /// <para><strong>@pre-condition:</strong> jwksUrl != null && httpClient != null</para>
    /// <para><strong>@post-condition:</strong> result != null && result.Keys.Count > 0</para>
    /// <para><strong>@complexity:</strong> O(1) (cache) or O(network) on expiry</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: HTTP + cache mutation)</para>
    /// </remarks>
    public async Task<JsonWebKeySet> GetJwksAsync(
        HttpClient httpClient,
        string jwksUrl,
        CancellationToken ct = default)
    {
        // Fast path: check cache without lock
        if (_jwks != null && DateTime.UtcNow < _expiresAt)
        {
            _logger.LogDebug(
                "[IdentityService.KeycloakAdapter][JwksCache][BLOCK_JWKS_CACHE_CHECK] JWKS cache hit");
            return _jwks;
        }

        // Slow path: refresh under lock to prevent stampede
        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_jwks != null && DateTime.UtcNow < _expiresAt)
            {
                _logger.LogDebug(
                    "[IdentityService.KeycloakAdapter][JwksCache][BLOCK_JWKS_CACHE_CHECK] JWKS cache hit (after lock)");
                return _jwks;
            }

            _logger.LogInformation(
                "[IdentityService.KeycloakAdapter][JwksCache][BLOCK_JWKS_CACHE_REFRESH] Refreshing JWKS from {JwksUrl}",
                jwksUrl);

            var response = await httpClient.GetAsync(jwksUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            _jwks = new JsonWebKeySet(json);
            _expiresAt = DateTime.UtcNow.AddSeconds(_cacheTtlSeconds);

            _logger.LogInformation(
                "[IdentityService.KeycloakAdapter][JwksCache][BLOCK_JWKS_CACHE_REFRESH] JWKS refreshed. Keys: {KeyCount}. Expires: {ExpiresAt}",
                _jwks.Keys.Count, _expiresAt);

            return _jwks;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Force invalidation of the cache. Useful after key rotation.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> Invalidate</para>
    /// <para><strong>@log-event:</strong> keycloak.cache.jwks-invalidate</para>
    /// <para><strong>@trace-span:</strong> keycloak.cache.invalidate</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    public void Invalidate()
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][JwksCache][BLOCK_JWKS_CACHE_INVALIDATE] JWKS cache invalidated");
        _expiresAt = DateTime.MinValue;
        _jwks = null;
    }

    /// <summary>
    /// Returns the approximate expiration time of the cache for testing.
    /// </summary>
    public DateTime CacheExpiresAt => _expiresAt;
}
