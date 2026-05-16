// FILE: GetUserRequest.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [DTO, DATA_TRANSFER]
// START_MODULE M_IDENTITY_READER

namespace IdentityService.Api.ReaderService.Models;

/// <summary>
/// BLOCK_READER_QUERY request model for fetching a user by ID.
/// </summary>
public record GetUserRequest
{
    /// <summary>Unique user identifier.</summary>
    public string UserId { get; init; } = string.Empty;
}

/// <summary>
/// BLOCK_READER_QUERY request model for fetching users by status.
/// </summary>
public record GetUsersByStatusRequest
{
    /// <summary>Status filter (e.g., "Provisioned", "Pending").</summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// BLOCK_READER_QUERY request model for fetching users by role.
/// </summary>
public record GetUsersByRoleRequest
{
    /// <summary>Role name to filter by.</summary>
    public string RoleName { get; init; } = string.Empty;
}
