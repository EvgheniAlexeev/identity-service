namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// BLOCK_TOKEN_VALIDATE token validation interface.
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validate a JWT token and return claims.
    /// </summary>
    Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct = default);
}
