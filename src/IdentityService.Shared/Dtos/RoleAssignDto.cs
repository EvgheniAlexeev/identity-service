// FILE: RoleAssignDto.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: Data transfer object (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Dtos;

/// <summary>
/// BLOCK_ASSIGN_ROLE DTO for role assignment operations.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@purpose:</strong> DTO carrying role assignment request</para>
/// <para><strong>@module-type:</strong> UTILITY</para>
/// <para><strong>@domain-concept:</strong> RoleAssignDto (value object)</para>
/// <para><strong>@invariant:</strong> RoleName valid for organization</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED-ID</para>
/// </remarks>
public record RoleAssignDto
{
    /// <summary>Target user identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Role identifier to assign.</summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>Role name (for cache population).</summary>
    public string RoleName { get; init; } = string.Empty;
}
