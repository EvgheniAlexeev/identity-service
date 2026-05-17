// FILE: UserDocumentDto.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Data transfer object (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Dtos;

/// <summary>
/// User document DTO for API responses and cache population.
/// PII fields must be redacted in logs.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Data transfer object carrying user information with PII redaction requirements</para>
/// <para><strong>@invariant:</strong> UserId and Email must be non-empty</para>
/// <para><strong>@invariant:</strong> Email and PII fields must be redacted in logs</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </remarks>
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
