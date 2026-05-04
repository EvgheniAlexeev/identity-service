using IdentityService.Api.WriterService.Handlers;
using IdentityService.Api.WriterService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.WriterService.Controllers;

/// <summary>
/// BLOCK_WRITER_COMMAND controller for user command endpoints.
/// Async processing — returns 202 Accepted immediately.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserCommandController : ControllerBase
{
    private readonly ICreateUserHandler _handler;
    private readonly IAssignRoleHandler _roleHandler;
    private readonly ILogger<UserCommandController> _logger;

    public UserCommandController(
        ICreateUserHandler handler,
        IAssignRoleHandler roleHandler,
        ILogger<UserCommandController> logger)
    {
        _handler = handler;
        _roleHandler = roleHandler;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/users — create a new user (async).
    /// Returns 202 Accepted with correlation ID.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        // [IdentityService.Api.WriterService][UserCommandController][BLOCK_WRITER_COMMAND]
        _logger.LogInformation(
            "[IdentityService.Api.WriterService][UserCommandController][BLOCK_WRITER_COMMAND] " +
            "Creating user {UserId}", request.UserId);

        var result = await _handler.HandleAsync(request, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        // Return 202 Accepted (async processing)
        return Accepted(new
        {
            userId = result.Data!.UserId,
            correlationId = result.Data.CorrelationId,
            message = "User provisioning initiated"
        });
    }

    /// <summary>
    /// POST /api/users/{userId}/roles — assign a role to a user (async).
    /// Returns 202 Accepted with correlation ID.
    /// </summary>
    [HttpPost("{userId}/roles")]
    [ProducesResponseType(typeof(AssignRoleResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        string userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken ct)
    {
        // [IdentityService.Api.WriterService][UserCommandController][BLOCK_WRITER_COMMAND]
        _logger.LogInformation(
            "[IdentityService.Api.WriterService][UserCommandController][BLOCK_WRITER_COMMAND] " +
            "Assigning role {RoleId} to user {UserId}", request.RoleId, userId);

        // Override UserId from URL
        request = request with { UserId = userId };

        var result = await _roleHandler.HandleAsync(request, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Accepted(new
        {
            userId = result.Data!.UserId,
            roleId = result.Data.RoleId,
            correlationId = result.Data.CorrelationId,
            message = "Role assignment initiated"
        });
    }
}
