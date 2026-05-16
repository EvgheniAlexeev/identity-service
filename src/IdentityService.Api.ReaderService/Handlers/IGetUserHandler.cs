// FILE: IGetUserHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Business logic handler (M-IDENTITY-READER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

using IdentityService.Api.ReaderService.Models;
using IdentityService.Shared.Dtos;

namespace IdentityService.Api.ReaderService.Handlers;

/// <summary>
/// Handler interface for user query operations.
/// </summary>
public interface IGetUserHandler
{
    /// <summary>
    /// Fetch a single user by ID from the cache.
    /// </summary>
    Task<Result<UserDocumentDto>> HandleAsync(GetUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Fetch all active users from the cache.
    /// </summary>
    Task<List<UserDocumentDto>> HandleGetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetch users by status filter.
    /// </summary>
    Task<List<UserDocumentDto>> HandleGetByStatusAsync(GetUsersByStatusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Fetch users by role name.
    /// </summary>
    Task<List<UserDocumentDto>> HandleGetByRoleAsync(GetUsersByRoleRequest request, CancellationToken ct = default);
}

/// <summary>
/// Handler interface for role query operations.
/// </summary>
public interface IGetRoleHandler
{
    /// <summary>
    /// Fetch a single role by ID from the cache.
    /// </summary>
    Task<Result<RoleDocumentDto>> HandleAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Fetch all active roles from the cache.
    /// </summary>
    Task<List<RoleDocumentDto>> HandleGetAllAsync(CancellationToken ct = default);
}
