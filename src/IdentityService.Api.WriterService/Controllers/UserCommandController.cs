using IdentityService.Api.WriterService.Handlers;
using IdentityService.Api.WriterService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.WriterService.Controllers;

/// <summary>
/// BLOCK_WRITER_COMMAND controller for user command endpoints.
/// Async processing — returns 202 Accepted immediately.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-WRITER</para>
/// <para><strong>@purpose:</strong> Accept and queue async user provisioning commands</para>
/// <para><strong>@module-type:</strong> ENTRY_POINT</para>
/// <para><strong>@depends:</strong> M-IDENTITY-KEYCLOAK, M-IDENTITY-SHARED</para>
/// <para><strong>@domain-concept:</strong> UserCommandController</para>
/// <para><strong>@invariant:</strong> All commands return 202 Accepted (async)</para>
/// <para><strong>@invariant:</strong> Idempotency via correlationId</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-WRITER-ID</para>
/// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> CreateUser</para>
    /// <para><strong>@param request:</strong> CreateUserRequest with user data</para>
    /// <para><strong>@return:</strong> CreateUserResponse with correlationId (202 Accepted)</para>
    /// <para><strong>@throws:</strong> BadRequestException — invalid request; InternalServerException — handler error</para>
    /// <para><strong>@log-event:</strong> writer.controller.create-user-start {userId}</para>
    /// <para><strong>@log-event:</strong> writer.controller.create-user-queued {correlationId}</para>
    /// <para><strong>@trace-span:</strong> writer.create-user</para>
    /// <para><strong>@pre-condition:</strong> request != null && request.UserId != null</para>
    /// <para><strong>@post-condition:</strong> response.correlationId != null</para>
    /// <para><strong>@complexity:</strong> O(1)</para>
    /// <para><strong>@idempotent:</strong> YES (idempotency key via correlationId)</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> AssignRole</para>
    /// <para><strong>@param userId:</strong> User ID from URL path</para>
    /// <para><strong>@param request:</strong> AssignRoleRequest with role data</para>
    /// <para><strong>@return:</strong> AssignRoleResponse with correlationId (202 Accepted)</para>
    /// <para><strong>@throws:</strong> BadRequestException — invalid request; InternalServerException — handler error</para>
    /// <para><strong>@log-event:</strong> writer.controller.assign-role-start {userId} {roleId}</para>
    /// <para><strong>@log-event:</strong> writer.controller.assign-role-queued {correlationId}</para>
    /// <para><strong>@trace-span:</strong> writer.assign-role</para>
    /// <para><strong>@pre-condition:</strong> userId != null && request != null && request.RoleId != null</para>
    /// <para><strong>@post-condition:</strong> response.correlationId != null</para>
    /// <para><strong>@complexity:</strong> O(1)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
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
