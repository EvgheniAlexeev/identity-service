using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// BLOCK_JWKS_CACHE In-memory JWKS (JSON Web Key Set) cache.
/// Caches Keycloak's public keys for 1 hour by default.
/// </summary>
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
