// FILE: UserCreatedDto.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Data transfer object (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Dtos;

/// <summary>
/// BLOCK_CREATE_USER DTO for user provisioning requests.
/// Contains PII (Email) — must be redacted in logs.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> DTO carrying user creation request with PII redaction requirements</para>
/// <para><strong>@invariant:</strong> Email is valid RFC 5322 format</para>
/// <para><strong>@invariant:</strong> FirstName and LastName non-empty</para>
/// <para><strong>@invariant:</strong> Email must be redacted in log output</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </remarks>
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
