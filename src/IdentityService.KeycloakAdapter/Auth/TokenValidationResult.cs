// FILE: TokenValidationResult.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-KEYCLOAK
// PURPOSE: Token validation and caching (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [AUTH, SECURITY]
// START_MODULE M_IDENTITY_KEYCLOAK

using System.Security.Claims;

namespace IdentityService.KeycloakAdapter.Auth;

/// <summary>
/// Result of a token validation operation.
/// </summary>
public record TokenValidationResult
{
    /// <summary>Whether the token is valid.</summary>
    public bool IsValid { get; init; }

    /// <summary>Claims extracted from the validated token.</summary>
    public IEnumerable<Claim> Claims { get; init; } = Enumerable.Empty<Claim>();

    /// <summary>Error message when validation fails.</summary>
    public string? Error { get; init; }

    /// <summary>Token expiration time if valid.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Token issuer if valid.</summary>
    public string? Issuer { get; init; }

    /// <summary>Subject claim (user ID) if valid.</summary>
    public string? Subject { get; init; }
}
