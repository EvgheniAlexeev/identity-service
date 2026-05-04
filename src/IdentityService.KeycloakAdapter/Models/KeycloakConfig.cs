namespace IdentityService.KeycloakAdapter.Models;

/// <summary>
/// Configuration for Keycloak connection.
/// </summary>
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
    public string Issuer => $"{Url}/realms/{Realm}";

    /// <summary>Expected audience claim value.</summary>
    public string Audience => ClientId;

    /// <summary>JWKS endpoint URL (derived).</summary>
    public string JwksUrl => $"{Issuer}/protocol/openid-connect/certs";
}
