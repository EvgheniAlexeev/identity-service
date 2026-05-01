namespace IdentityService.Shared.Dtos;

/// <summary>
/// BLOCK_ASSIGN_ROLE DTO for role assignment operations.
/// </summary>
public record RoleAssignDto
{
    /// <summary>Target user identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Role identifier to assign.</summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>Role name (for cache population).</summary>
    public string RoleName { get; init; } = string.Empty;
}
