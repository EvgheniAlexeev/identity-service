using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using IdentityService.KeycloakAdapter.Models;

namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// BLOCK_TOKEN_VALIDATE JWT token validator using Keycloak JWKS.
/// Validates signature, lifetime, issuer, and audience.
/// </summary>
public class TokenValidator : ITokenValidator
{
    private readonly JwksCache _jwksCache;
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfig _config;
    private readonly ILogger<TokenValidator> _logger;

    public TokenValidator(
        JwksCache jwksCache,
        HttpClient httpClient,
        KeycloakConfig config,
        ILogger<TokenValidator> logger)
    {
        _jwksCache = jwksCache;
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// BLOCK_TOKEN_VALIDATE Validates a JWT token against Keycloak's public keys.
    /// </summary>
    public async Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Validating token");

        try
        {
            var jwks = await _jwksCache.GetJwksAsync(
                _httpClient,
                $"{_config.Issuer}/protocol/openid-connect/certs",
                ct);

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = _config.Issuer,
                ValidAudiences = new[] { _config.Audience },
                IssuerSigningKeys = jwks.Keys,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            var principal = handler.ValidateToken(token, parameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Token is not a valid JWT"
                };
            }

            _logger.LogInformation(
                "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Token validated successfully. Subject: {Subject}",
                principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                principal.FindFirst("sub")?.Value);

            // Try multiple claim types for subject
            var subjectClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                ?? principal.FindFirst("sub");

            return new TokenValidationResult
            {
                IsValid = true,
                Claims = principal.Claims,
                ExpiresAt = jwtToken.ValidTo,
                Issuer = jwtToken.Issuer,
                Subject = subjectClaim?.Value
            };
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(
                "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Token expired: {Message}",
                ex.Message);
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Token has expired"
            };
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(
                "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Invalid signature: {Message}",
                ex.Message);
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Invalid token signature"
            };
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(
                "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Invalid issuer: {Message}",
                ex.Message);
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Invalid token issuer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "[IdentityService.KeycloakAdapter][TokenValidator][BLOCK_TOKEN_VALIDATE] Token validation error: {Message}",
                ex.Message);
            return new TokenValidationResult
            {
                IsValid = false,
                Error = $"Token validation failed: {ex.Message}"
            };
        }
    }
}
