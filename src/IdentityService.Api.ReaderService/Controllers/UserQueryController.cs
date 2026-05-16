// FILE: UserQueryController.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: HTTP API controller (M-IDENTITY-READER)
// SEMANTIC_TAG: [HTTP_CONTROLLER, API]
// START_MODULE M_IDENTITY_READER

using IdentityService.Api.ReaderService.Handlers;
using IdentityService.Api.ReaderService.Models;
using IdentityService.Api.ReaderService.Validators;
using IdentityService.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ReaderService.Controllers;

/// <summary>
/// BLOCK_READER_QUERY controller for user query endpoints.
/// Cache-only reads — no Keycloak fallback.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-READER</para>
/// <para><strong>@purpose:</strong> Provides HTTP query endpoints for user retrieval from cache with fallback validation</para>
/// <para><strong>@module-type:</strong> ENTRY_POINT</para>
/// <para><strong>@depends:</strong> M-IDENTITY-CACHE, M-IDENTITY-SHARED</para>
/// <para><strong>@domain-concept:</strong> UsersController</para>
/// <para><strong>@invariant:</strong> Cache hit ≥ 95%</para>
/// <para><strong>@invariant:</strong> Response latency p99 ≤ 50ms (cache hit)</para>
/// <para><strong>@invariant:</strong> 404 on cache miss (no Keycloak fallback)</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-READER-ID</para>
/// </remarks>
[ApiController]
[Route("api/users")]
public class UserQueryController : ControllerBase
{
    private readonly IGetUserHandler _handler;
    private readonly GetUserRequestValidator _validator;
    private readonly ILogger<UserQueryController> _logger;

    public UserQueryController(
        IGetUserHandler handler,
        GetUserRequestValidator validator,
        ILogger<UserQueryController> logger)
    {
        _handler = handler;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users/{userId} — retrieve a single user from cache.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetUser</para>
    /// <para><strong>@param userId:</strong> User identifier</para>
    /// <para><strong>@return:</strong> UserDocumentDto with user details (200 OK)</para>
    /// <para><strong>@throws:</strong> NotFoundException — user not in cache; ValidationException — userId invalid</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-user-start {userId}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-user-cache-hit {userId}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-user-cache-miss {userId}</para>
    /// <para><strong>@trace-span:</strong> reader.get-user</para>
    /// <para><strong>@pre-condition:</strong> userId != null && userId.Length > 0</para>
    /// <para><strong>@post-condition:</strong> result != null</para>
    /// <para><strong>@complexity:</strong> O(1) (cache lookup)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: cache)</para>
    /// </remarks>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct)
    {
        // [IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY]
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY] " +
            "Querying user {UserId}", userId);

        var request = new GetUserRequest { UserId = userId };
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { error = "Invalid request", details = validationResult.Errors });
        }

        var result = await _handler.HandleAsync(request, ct);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
                return NotFound(new { error = result.Error });

            return StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// GET /api/users — retrieve all active users from cache.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetAllUsers</para>
    /// <para><strong>@return:</strong> List&lt;UserDocumentDto&gt; with all users (200 OK)</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-all-users-start</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-all-users-success {count}</para>
    /// <para><strong>@trace-span:</strong> reader.get-all-users</para>
    /// <para><strong>@complexity:</strong> O(n) (full cache scan)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: cache)</para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY_ALL] " +
            "Querying all users");

        var results = await _handler.HandleGetAllAsync(ct);

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY_ALL] " +
            "Returning {Count} users", results.Count);

        return Ok(results);
    }

    /// <summary>
    /// GET /api/users/by-status/{status} — retrieve users filtered by status.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetUsersByStatus</para>
    /// <para><strong>@param status:</strong> User status filter (e.g., ACTIVE, SUSPENDED)</para>
    /// <para><strong>@return:</strong> List&lt;UserDocumentDto&gt; filtered by status (200 OK)</para>
    /// <para><strong>@throws:</strong> BadRequestException — status parameter required or invalid</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-users-by-status-start {status}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-users-by-status-result {count}</para>
    /// <para><strong>@trace-span:</strong> reader.get-users-by-status</para>
    /// <para><strong>@complexity:</strong> O(n) (filtered cache scan)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersByStatus(string status, CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY_BY_STATUS] " +
            "Querying users by status {Status}", status);

        var request = new GetUsersByStatusRequest { Status = status };

        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { error = "Status parameter is required" });

        var results = await _handler.HandleGetByStatusAsync(request, ct);

        return Ok(results);
    }

    /// <summary>
    /// GET /api/users/by-role/{roleName} — retrieve users filtered by role.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetUsersByRole</para>
    /// <para><strong>@param roleName:</strong> Role name filter (e.g., Admin, User, Auditor)</para>
    /// <para><strong>@return:</strong> List&lt;UserDocumentDto&gt; filtered by role (200 OK)</para>
    /// <para><strong>@throws:</strong> BadRequestException — roleName parameter required</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-users-by-role-start {roleName}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-users-by-role-result {count}</para>
    /// <para><strong>@trace-span:</strong> reader.get-users-by-role</para>
    /// <para><strong>@complexity:</strong> O(n) (filtered cache scan)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    [HttpGet("by-role/{roleName}")]
    [ProducesResponseType(typeof(List<UserDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersByRole(string roleName, CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][UserQueryController][BLOCK_READER_QUERY_BY_ROLE] " +
            "Querying users by role {RoleName}", roleName);

        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new { error = "RoleName parameter is required" });

        var request = new GetUsersByRoleRequest { RoleName = roleName };
        var results = await _handler.HandleGetByRoleAsync(request, ct);

        return Ok(results);
    }
}
