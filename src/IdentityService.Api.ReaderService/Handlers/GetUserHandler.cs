using IdentityService.Api.ReaderService.Models;
using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.ReaderService.Handlers;

/// <summary>
/// BLOCK_HANDLER_GET handler for user query operations.
/// Reads from cache only — no Keycloak fallback.
/// </summary>
public class GetUserHandler : IGetUserHandler
{
    private readonly IUserCacheRepository _cache;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(IUserCacheRepository cache, ILogger<GetUserHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<UserDocumentDto>> HandleAsync(GetUserRequest request, CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET]
        try
        {
            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET] " +
                "Fetching user {UserId}", request.UserId);

            var entry = await _cache.GetByUserIdAsync(request.UserId, ct);

            if (entry == null)
            {
                _logger.LogWarning(
                    "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET] " +
                    "User not found in cache {UserId}", request.UserId);
                return Result<UserDocumentDto>.NotFound($"User not found: {request.UserId}");
            }

            var dto = MapToDto(entry);

            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET] " +
                "User retrieved from cache {UserId}", request.UserId);

            return Result<UserDocumentDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET] " +
                "Error fetching user {UserId}", request.UserId);
            return Result<UserDocumentDto>.Failure("Internal server error");
        }
    }

    public async Task<List<UserDocumentDto>> HandleGetAllAsync(CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_ALL]
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_ALL] " +
            "Fetching all active users");

        var entries = await _cache.GetAllActiveAsync(ct);

        var results = entries.Select(MapToDto).ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_ALL] " +
            "Retrieved {Count} active users", results.Count);

        return results;
    }

    public async Task<List<UserDocumentDto>> HandleGetByStatusAsync(
        GetUsersByStatusRequest request, CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_STATUS]
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_STATUS] " +
            "Fetching users by status {Status}", request.Status);

        var allEntries = await _cache.GetAllActiveAsync(ct);

        // Filter by status in-memory (cache doesn't store status, so we filter DTOs)
        var results = allEntries
            .Select(MapToDto)
            .Where(dto => dto.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_STATUS] " +
            "Found {Count} users with status {Status}", results.Count, request.Status);

        return results;
    }

    public async Task<List<UserDocumentDto>> HandleGetByRoleAsync(
        GetUsersByRoleRequest request, CancellationToken ct = default)
    {
        // [IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_ROLE]
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_ROLE] " +
            "Fetching users by role {RoleName}", request.RoleName);

        var allEntries = await _cache.GetAllActiveAsync(ct);

        var results = allEntries
            .Where(e => e.Roles.Any(r => r.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase)))
            .Select(MapToDto)
            .ToList();

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserHandler][BLOCK_HANDLER_GET_BY_ROLE] " +
            "Found {Count} users with role {RoleName}", results.Count, request.RoleName);

        return results;
    }

    private static UserDocumentDto MapToDto(CacheLayer.MongoDB.UserCacheEntry entry)
    {
        return new UserDocumentDto
        {
            UserId = entry.UserId,
            Email = entry.Email,
            FirstName = string.Empty,  // Cache entry doesn't store name
            LastName = string.Empty,   // Cache entry doesn't store name
            Roles = entry.Roles,
            Status = "Provisioned",
            CreatedAt = entry.CreatedAt,
            LastModifiedAt = null
        };
    }
}
