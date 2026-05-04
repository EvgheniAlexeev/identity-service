using IdentityService.Api.ReaderService.Handlers;
using IdentityService.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ReaderService.Controllers;

/// <summary>
/// BLOCK_READER_QUERY controller for role query endpoints.
/// Cache-only reads.
/// </summary>
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
