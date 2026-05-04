// FILE: src/IdentityService.Api.ReaderService/Features/QueryUsers/QueryUsersEndpoint.cs
// VERSION: 1.0.0

using IdentityService.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ReaderService.Features.QueryUsers;

/// <summary>
/// BLOCK_QUERY_USERS_ENDPOINT — Query users with filters (all, by-status, by-role).
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
[ApiController]
[Route("api/users")]
public class QueryUsersEndpoint : ControllerBase
{
    private readonly QueryUsersHandler _handler;
    private readonly ILogger<QueryUsersEndpoint> _logger;

    public QueryUsersEndpoint(QueryUsersHandler handler, ILogger<QueryUsersEndpoint> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users — retrieve all active users from cache.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        // START_BLOCK_QUERY_USERS_ALL
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersEndpoint] " +
            "Querying all users");

        var results = await _handler.HandleGetAllAsync(ct);

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersEndpoint] " +
            "Returning {Count} users", results.Count);

        return Ok(results);
        // END_BLOCK_QUERY_USERS_ALL
    }

    /// <summary>
    /// GET /api/users/by-status/{status} — retrieve users filtered by status.
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken ct)
    {
        // START_BLOCK_QUERY_USERS_BY_STATUS
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersEndpoint] " +
            "Querying users by status {Status}", status);

        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { error = "Status parameter is required" });

        var request = new QueryUsersByStatusRequest { Status = status };
        var results = await _handler.HandleGetByStatusAsync(request, ct);

        return Ok(results);
        // END_BLOCK_QUERY_USERS_BY_STATUS
    }

    /// <summary>
    /// GET /api/users/by-role/{roleName} — retrieve users filtered by role.
    /// </summary>
    [HttpGet("by-role/{roleName}")]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRole(string roleName, CancellationToken ct)
    {
        // START_BLOCK_QUERY_USERS_BY_ROLE
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.QueryUsers][QueryUsersEndpoint] " +
            "Querying users by role {RoleName}", roleName);

        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new { error = "RoleName parameter is required" });

        var request = new QueryUsersByRoleRequest { RoleName = roleName };
        var results = await _handler.HandleGetByRoleAsync(request, ct);

        return Ok(results);
        // END_BLOCK_QUERY_USERS_BY_ROLE
    }
}
