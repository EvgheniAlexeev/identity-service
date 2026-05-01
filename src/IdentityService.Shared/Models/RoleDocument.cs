namespace IdentityService.Shared.Models;

/// <summary>
/// MongoDB document for role persistence.
/// </summary>
public record RoleDocument
{
    /// <summary>MongoDB _id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Business key — unique role identifier.</summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>Human-readable role name.</summary>
    public string RoleName { get; init; } = string.Empty;

    /// <summary>Permission strings associated with this role.</summary>
    public List<string> Permissions { get; init; } = new();

    /// <summary>Document creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; init; }
}
