// FILE: QueryUsersHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Business logic handler (M-IDENTITY-READER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/QueryUsers/QueryUsersHandler.cs
// VERSION: 1.0.0

using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.ReaderService.Features.QueryUsers;

/// <summary>
/// BLOCK_QUERY_USERS_HANDLER — Handler for querying users with filters.
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
public class QueryUsersHandler
{
    private readonly IUserCacheRepository _cache;
    private readonly ILogger<QueryUsersHandler> _logger;

    public QueryUsersHandler(IUserCacheRepository cache, ILogger<QueryUsersHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<UserDocumentDto>> HandleGetAllAsync(CancellationToken ct = default)
    {
        // START_BLOCK_QUERY_USERS_HANDLER_ALL
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Fetching all active users");

        var entries = await _cache.GetAllActiveAsync(ct);

        var results = entries.Select(MapToDto).ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Retrieved {Count} active users", results.Count);

        return results;
        // END_BLOCK_QUERY_USERS_HANDLER_ALL
    }

    public async Task<List<UserDocumentDto>> HandleGetByStatusAsync(
        QueryUsersByStatusRequest request, CancellationToken ct = default)
    {
        // START_BLOCK_QUERY_USERS_HANDLER_BY_STATUS
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Fetching users by status {Status}", request.Status);

        var allEntries = await _cache.GetAllActiveAsync(ct);

        var results = allEntries
            .Select(MapToDto)
            .Where(dto => dto.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Found {Count} users with status {Status}", results.Count, request.Status);

        return results;
        // END_BLOCK_QUERY_USERS_HANDLER_BY_STATUS
    }

    public async Task<List<UserDocumentDto>> HandleGetByRoleAsync(
        QueryUsersByRoleRequest request, CancellationToken ct = default)
    {
        // START_BLOCK_QUERY_USERS_HANDLER_BY_ROLE
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Fetching users by role {RoleName}", request.RoleName);

        var allEntries = await _cache.GetAllActiveAsync(ct);

        var results = allEntries
            .Where(e => e.Roles.Any(r => r.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase)))
            .Select(MapToDto)
            .ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersHandler] " +
            "Found {Count} users with role {RoleName}", results.Count, request.RoleName);

        return results;
        // END_BLOCK_QUERY_USERS_HANDLER_BY_ROLE
    }

    private static UserDocumentDto MapToDto(CacheLayer.MongoDB.UserCacheEntry entry)
    {
        return new UserDocumentDto
        {
            UserId = entry.UserId,
            Email = entry.Email,
            FirstName = string.Empty,
            LastName = string.Empty,
            Roles = entry.Roles,
            Status = "Provisioned",
            CreatedAt = entry.CreatedAt,
            LastModifiedAt = null
        };
    }
}
