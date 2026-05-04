namespace IdentityService.KeycloakAdapter.Admin;

/// <summary>
/// Exception thrown by the Keycloak admin client.
/// </summary>
public class KeycloakException : Exception
{
    /// <summary>HTTP status code from the Keycloak API response.</summary>
    public int StatusCode { get; }

    public KeycloakException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public KeycloakException(string message, int statusCode, Exception inner)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
