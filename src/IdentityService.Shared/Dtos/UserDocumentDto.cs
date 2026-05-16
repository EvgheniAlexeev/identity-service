// FILE: UserDocumentDto.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Data transfer object (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Dtos;

/// <summary>
/// User document DTO for API responses and cache population.
/// PII fields must be redacted in logs.
/// </summary>
public record UserDocumentDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public string Status { get; init; } = "Provisioned";
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
}
