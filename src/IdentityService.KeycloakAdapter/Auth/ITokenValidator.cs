// FILE: ITokenValidator.cs
// VERSION: 2.0.0
// MODULE: M-KEYCLOAK
// PURPOSE: Token validation and caching (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_IDENTITY_KEYCLOAK

namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// BLOCK_TOKEN_VALIDATE token validation interface.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-KEYCLOAK</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> JWT token validation with cached JWKS (JSON Web Key Set)</para>
/// <para><strong>@invariant:</strong> JWKS cache TTL: 1 hour, stampede prevention enabled</para>
/// <para><strong>@invariant:</strong> Validates JWT issuer matches configured Keycloak instance</para>
/// <para><strong>@verification-ref:</strong> V-M-KEYCLOAK</para>
/// </remarks>
public interface ITokenValidator
{
    /// <summary>
    /// Validate a JWT token and return claims.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> ValidateAsync</para>
    /// <para><strong>@param token:</strong> JWT access token to validate</para>
    /// <para><strong>@return:</strong> TokenValidationResult with claims if valid</para>
    /// <para><strong>@throws:</strong> TokenExpiredException — token has expired; InvalidSignatureException — JWT signature invalid</para>
    /// <para><strong>@log-event:</strong> keycloak.validator.validate-token-start</para>
    /// <para><strong>@log-event:</strong> keycloak.validator.validate-token-success</para>
    /// <para><strong>@log-event:</strong> keycloak.validator.validate-token-error {error}</para>
    /// <para><strong>@log-event:</strong> keycloak.validator.validate-token-jwks-cache-hit</para>
    /// <para><strong>@log-event:</strong> keycloak.validator.validate-token-jwks-cache-miss</para>
    /// <para><strong>@trace-span:</strong> keycloak.validator.validate-token</para>
    /// <para><strong>@pre-condition:</strong> token != null && token.Length > 0</para>
    /// <para><strong>@post-condition:</strong> result != null</para>
    /// <para><strong>@complexity:</strong> O(1) (cache-backed)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: JWKS cache)</para>
    /// </remarks>
    Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct = default);
}
