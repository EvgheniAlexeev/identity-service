// FILE: RoleQueryController.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: HTTP API controller (M-IDENTITY-READER)
// SEMANTIC_TAG: [HTTP_CONTROLLER, API]
// START_MODULE M_IDENTITY_READER

using IdentityService.Api.ReaderService.Handlers;
using IdentityService.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ReaderService.Controllers;

/// <summary>
/// BLOCK_READER_QUERY controller for role query endpoints.
/// Cache-only reads.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-READER</para>
/// <para><strong>@purpose:</strong> Provides HTTP query endpoints for role retrieval from cache with fallback validation</para>
/// <para><strong>@invariant:</strong> Cache hit ≥ 95%</para>
/// <para><strong>@invariant:</strong> Response latency p99 ≤ 50ms (cache hit)</para>
/// <para><strong>@verification-ref:</strong> V-M-READER</para>
/// </remarks>
[ApiController]
[Route("api/roles")]
public class RoleQueryController : ControllerBase
{
    private readonly IGetRoleHandler _handler;
    private readonly ILogger<RoleQueryController> _logger;

    public RoleQueryController(
        IGetRoleHandler handler,
        ILogger<RoleQueryController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/roles/{roleId} — retrieve a single role from cache.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetRole</para>
    /// <para><strong>@param roleId:</strong> Role identifier</para>
    /// <para><strong>@return:</strong> RoleDocumentDto with role details (200 OK)</para>
    /// <para><strong>@throws:</strong> NotFoundException — role not in cache; BadRequestException — roleId required</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-role-start {roleId}</para>
    /// <para><strong>@trace-span:</strong> reader.get-role</para>
    /// <para><strong>@pre-condition:</strong> roleId != null && roleId.Length > 0</para>
    /// <para><strong>@post-condition:</strong> result != null</para>
    /// <para><strong>@complexity:</strong> O(1) (cache lookup)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: cache)</para>
    /// </remarks>
    [HttpGet("{roleId}")]
    [ProducesResponseType(typeof(RoleDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(string roleId, CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][RoleQueryController][BLOCK_READER_QUERY] " +
            "Querying role {RoleId}", roleId);

        if (string.IsNullOrWhiteSpace(roleId))
            return BadRequest(new { error = "RoleId parameter is required" });

        var result = await _handler.HandleAsync(roleId, ct);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
                return NotFound(new { error = result.Error });

            return StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// GET /api/roles — retrieve all active roles from cache.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetAllRoles</para>
    /// <para><strong>@return:</strong> List&lt;RoleDocumentDto&gt; with all roles (200 OK)</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-all-roles-start</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-all-roles-success {count}</para>
    /// <para><strong>@trace-span:</strong> reader.get-all-roles</para>
    /// <para><strong>@complexity:</strong> O(n) (full cache scan)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: cache)</para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles(CancellationToken ct)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][RoleQueryController][BLOCK_READER_QUERY_ALL] " +
            "Querying all roles");

        var results = await _handler.HandleGetAllAsync(ct);

        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][RoleQueryController][BLOCK_READER_QUERY_ALL] " +
            "Returning {Count} roles", results.Count);

        return Ok(results);
    }
}
