namespace IdentityService.Shared.Dtos;

/// <summary>
/// Role document DTO for cache population and API responses.
/// </summary>
public record RoleDocumentDto
{
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
}
