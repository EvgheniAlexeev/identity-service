// FILE: DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-KEYCLOAK
// PURPOSE: M-IDENTITY-KEYCLOAK component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_KEYCLOAK

using IdentityService.KeycloakAdapter.Admin;
using IdentityService.KeycloakAdapter.Auth;
using IdentityService.KeycloakAdapter.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.KeycloakAdapter;

/// <summary>
/// BLOCK_DI dependency injection registration for the KeycloakAdapter module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Keycloak adapter services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">Keycloak configuration.</param>
    public static IServiceCollection AddKeycloakAdapter(
        this IServiceCollection services,
        KeycloakConfig config)
    {
        services.AddSingleton(config);

        // Register typed HttpClient for admin client
        services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>(client =>
        {
            client.BaseAddress = new Uri(config.AdminUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register HttpClient for token validation (separate for different lifetime)
        services.AddHttpClient<TokenValidator>(client =>
        {
            client.BaseAddress = new Uri(config.Url);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // JWKS cache is singleton — shared across validators
        services.AddSingleton<JwksCache>();
        services.AddScoped<ITokenValidator, TokenValidator>();

        return services;
    }
}
