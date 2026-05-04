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

    /// <summary>
    /// Returns true if the Keycloak API returned a 404 (user not found).
    /// Used by saga failure handler to classify UserAlreadyExists vs retryable errors.
    /// </summary>
    public bool IsNotFound() => StatusCode == 404;
}
