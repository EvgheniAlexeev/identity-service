// FILE: GetUserHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Business logic handler (M-IDENTITY-READER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/GetUser/GetUserHandler.cs
// VERSION: 1.0.0

using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.ReaderService.Features.GetUser;

/// <summary>
/// BLOCK_GET_USER_HANDLER — Handler for fetching a single user from cache.
/// VSA feature: GetUser (ReaderService)
/// </summary>
public class GetUserHandler
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
        // START_BLOCK_GET_USER_HANDLER
        try
        {
            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][Features.GetUser][GetUserHandler] " +
                "Fetching user {UserId}", request.UserId);

            var entry = await _cache.GetByUserIdAsync(request.UserId, ct);

            if (entry == null)
            {
                _logger.LogWarning(
                    "[IdentityService.Api.ReaderService][Features.GetUser][GetUserHandler] " +
                    "User not found in cache {UserId}", request.UserId);
                return Result<UserDocumentDto>.NotFound($"User not found: {request.UserId}");
            }

            var dto = MapToDto(entry);

            _logger.LogInformation(
                "[IdentityService.Api.ReaderService][Features.GetUser][GetUserHandler] " +
                "User retrieved from cache {UserId}", request.UserId);

            return Result<UserDocumentDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.Api.ReaderService][Features.GetUser][GetUserHandler] " +
                "Error fetching user {UserId}", request.UserId);
            return Result<UserDocumentDto>.Failure("Internal server error");
        }
        // END_BLOCK_GET_USER_HANDLER
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
