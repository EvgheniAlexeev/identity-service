// FILE: AssignRoleHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WRITER
// PURPOSE: Business logic handler (M-IDENTITY-WRITER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WRITER

using FluentValidation;
using IdentityService.Api.WriterService.Models;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Dtos;
using IdentityService.Shared.Events;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.WriterService.Handlers;

/// <summary>
/// BLOCK_HANDLER_ASSIGN_ROLE handler for role assignment commands.
/// Validates, publishes SyncRoleCommand to MQ.
/// </summary>
public class AssignRoleHandler : IAssignRoleHandler
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<AssignRoleHandler> _logger;

    public AssignRoleHandler(
        IMessagePublisher publisher,
        ILogger<AssignRoleHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<AssignRoleResponse>> HandleAsync(AssignRoleRequest request, CancellationToken ct = default)
    {
        // [IdentityService.Api.WriterService][AssignRoleHandler][BLOCK_HANDLER_ASSIGN_ROLE]
        try
        {
            _logger.LogInformation(
                "[IdentityService.Api.WriterService][AssignRoleHandler][BLOCK_HANDLER_ASSIGN_ROLE] " +
                "Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

            // Validate
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<AssignRoleResponse>.Failure("UserId is required");
            }

            if (string.IsNullOrWhiteSpace(request.RoleId))
            {
                return Result<AssignRoleResponse>.Failure("RoleId is required");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return Result<AssignRoleResponse>.Failure("RoleName is required");
            }

            var correlationId = Guid.NewGuid().ToString("N");

            // Publish SyncRoleCommand to MQ
            var command = new SyncRoleCommand
            {
                IdempotencyKey = correlationId,
                Role = new RoleAssignDto
                {
                    UserId = request.UserId,
                    RoleId = request.RoleId,
                    RoleName = request.RoleName
                }
            };

            await _publisher.PublishAsync(command, ct);

            _logger.LogInformation(
                "[IdentityService.Api.WriterService][AssignRoleHandler][BLOCK_HANDLER_ASSIGN_ROLE] " +
                "Role assignment command published {UserId} {RoleId} {CorrelationId}",
                request.UserId, request.RoleId, correlationId);

            return Result<AssignRoleResponse>.Success(new AssignRoleResponse
            {
                UserId = request.UserId,
                RoleId = request.RoleId,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.Api.WriterService][AssignRoleHandler][BLOCK_HANDLER_ASSIGN_ROLE] " +
                "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

            // Publish FailedIdentityEvent to DLQ
            try
            {
                var failedEvent = new FailedIdentityEvent
                {
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    UserId = request.UserId,
                    OriginalRequest = new UserCreatedDto
                    {
                        UserId = request.UserId,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        InitialRoles = new List<string> { request.RoleId }
                    },
                    FailedStep = "AssignRoleHandler",
                    ErrorMessage = ex.Message,
                    ErrorCode = "INTERNAL_ERROR",
                    RetryCount = 0,
                    FailedAt = DateTime.UtcNow,
                    Cause = IdentityFailureCause.Unknown
                };
                await _publisher.PublishAsync(failedEvent, ct);
            }
            catch
            {
                _logger.LogCritical(
                    "[IdentityService.Api.WriterService][AssignRoleHandler][BLOCK_HANDLER_ASSIGN_ROLE] " +
                    "Failed to publish failure event to DLQ for user {UserId}", request.UserId);
            }

            return Result<AssignRoleResponse>.Failure("Internal server error");
        }
    }
}
