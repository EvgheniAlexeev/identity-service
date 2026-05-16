// FILE: KeycloakConfig.cs
// VERSION: 2.0.0
// MODULE: M-KEYCLOAK
// PURPOSE: Keycloak integration (M-IDENTITY-KEYCLOAK)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_KEYCLOAK

namespace IdentityService.KeycloakAdapter.Models;

/// <summary>
/// Configuration for Keycloak connection.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-KEYCLOAK</para>
/// <para><strong>@purpose:</strong> Configuration record for Keycloak connection with derived URLs</para>
/// </remarks>
public record KeycloakConfig
{
    /// <summary>Keycloak server base URL (e.g., https://auth.example.com).</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Keycloak realm name.</summary>
    public string Realm { get; init; } = string.Empty;

    /// <summary>Client ID for this service.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Client secret (keep secure).</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>Admin API base URL (derived).</summary>
    public string AdminUrl => $"{Url}/admin/realms/{Realm}";

    /// <summary>Token issuer URI.</summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> DeriveIssuer</para>
    /// <para><strong>@return:</strong> Derived issuer URL format: {Url}/realms/{Realm}</para>
    /// <para><strong>@pure:</strong> YES</para>
    /// </remarks>
    public string Issuer => $"{Url}/realms/{Realm}";

    /// <summary>Expected audience claim value.</summary>
    public string Audience => ClientId;

    /// <summary>JWKS endpoint URL (derived).</summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> DeriveJwksUrl</para>
    /// <para><strong>@return:</strong> Derived JWKS endpoint URL</para>
    /// <para><strong>@pure:</strong> YES</para>
    /// </remarks>
    public string JwksUrl => $"{Issuer}/protocol/openid-connect/certs";
}
