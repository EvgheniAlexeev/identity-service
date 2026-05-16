// FILE: RoleDocumentDto.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Data transfer object (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_SHARED

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
