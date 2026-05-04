namespace IdentityService.Api.WriterService.Models;

/// <summary>
/// BLOCK_WRITER_COMMAND request model for creating a user.
/// </summary>
public record CreateUserRequest
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

/// <summary>
/// BLOCK_WRITER_COMMAND response model for user creation.
/// </summary>
public record CreateUserResponse
{
    public string UserId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// BLOCK_WRITER_COMMAND request model for assigning a role.
/// </summary>
public record AssignRoleRequest
{
    /// <summary>User identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Role identifier.</summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>Role display name.</summary>
    public string RoleName { get; init; } = string.Empty;
}

/// <summary>
/// BLOCK_WRITER_COMMAND response model for role assignment.
/// </summary>
public record AssignRoleResponse
{
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
