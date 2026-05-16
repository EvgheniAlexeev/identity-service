// FILE: QueryUsersRequest.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/QueryUsers/QueryUsersRequest.cs
// VERSION: 1.0.0

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.ReaderService.Features.QueryUsers;

/// <summary>
/// Request model for querying users by status.
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
public class QueryUsersByStatusRequest
{
    /// <summary>
    /// Status filter value.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request model for querying users by role.
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
public class QueryUsersByRoleRequest
{
    /// <summary>
    /// Role name filter.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string RoleName { get; set; } = string.Empty;
}
