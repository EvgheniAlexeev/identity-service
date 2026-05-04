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
