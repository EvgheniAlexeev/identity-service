namespace IdentityService.Shared.Dtos;

/// <summary>
/// BLOCK_CREATE_USER DTO for user provisioning requests.
/// Contains PII (Email) — must be redacted in logs.
/// </summary>
public record UserCreatedDto
{
    /// <summary>Unique user identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>User email (PII — redact in logs).</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>User first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>User last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Roles to assign at creation time.</summary>
    public List<string>? InitialRoles { get; init; }
}
