// FILE: GetRoleHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Business logic handler (M-IDENTITY-READER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

using IdentityService.Api.ReaderService.Models;
using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.ReaderService.Handlers;

/// <summary>
/// BLOCK_HANDLER_GET handler for role query operations.
/// Reads from role cache only.
/// </summary>
public class GetRoleHandler : IGetRoleHandler
{
    private readonly IRoleCacheRepository _cache;
    private readonly ILogger<GetRoleHandler> _logger;

    public GetRoleHandler(IRoleCacheRepository cache, ILogger<GetRoleHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<RoleDocumentDto>> HandleAsync(string roleId, CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ROLE]
        try
        {
            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ROLE] " +
                "Fetching role {RoleId}", roleId);

            var entry = await _cache.GetByRoleIdAsync(roleId, ct);

            if (entry == null)
            {
                _logger.LogWarning(
                    "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ROLE] " +
                    "Role not found in cache {RoleId}", roleId);
                return Result<RoleDocumentDto>.NotFound($"Role not found: {roleId}");
            }

            var dto = new RoleDocumentDto
            {
                RoleId = entry.RoleId,
                RoleName = entry.RoleName,
                Permissions = entry.Permissions,
                CreatedAt = entry.CreatedAt,
                LastModifiedAt = null
            };

            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ROLE] " +
                "Role retrieved from cache {RoleId}", roleId);

            return Result<RoleDocumentDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ROLE] " +
                "Error fetching role {RoleId}", roleId);
            return Result<RoleDocumentDto>.Failure("Internal server error");
        }
    }

    public async Task<List<RoleDocumentDto>> HandleGetAllAsync(CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ALL_ROLES]
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ALL_ROLES] " +
            "Fetching all active roles");

        var entries = await _cache.GetAllActiveAsync(ct);

        var results = entries.Select(e => new RoleDocumentDto
        {
            RoleId = e.RoleId,
            RoleName = e.RoleName,
            Permissions = e.Permissions,
            CreatedAt = e.CreatedAt,
            LastModifiedAt = null
        }).ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetRoleHandler][BLOCK_HANDLER_GET_ALL_ROLES] " +
            "Retrieved {Count} active roles", results.Count);

        return results;
    }
}
