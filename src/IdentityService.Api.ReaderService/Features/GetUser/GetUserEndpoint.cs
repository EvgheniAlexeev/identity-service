// FILE: GetUserEndpoint.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: M-IDENTITY-READER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/GetUser/GetUserEndpoint.cs
// VERSION: 1.0.0

using IdentityService.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ReaderService.Features.GetUser;

/// <summary>
/// BLOCK_GET_USER_ENDPOINT — Get single user by ID from cache.
/// VSA feature: GetUser (ReaderService)
/// </summary>
[ApiController]
[Route("api/users")]
public class GetUserEndpoint : ControllerBase
{
    private readonly GetUserHandler _handler;
    private readonly GetUserValidator _validator;
    private readonly ILogger<GetUserEndpoint> _logger;

    public GetUserEndpoint(
        GetUserHandler handler,
        GetUserValidator validator,
        ILogger<GetUserEndpoint> logger)
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
    public async Task<IActionResult> Get(string userId, CancellationToken ct)
    {
        // START_BLOCK_GET_USER_ENDPOINT
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][Features.GetUser][GetUserEndpoint] " +
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
        // END_BLOCK_GET_USER_ENDPOINT
    }
}
