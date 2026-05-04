using FluentValidation;
using IdentityService.Api.WriterService.Models;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Dtos;
using IdentityService.Shared.Events;
using IdentityService.Shared.Validators;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.WriterService.Handlers;

/// <summary>
/// BLOCK_HANDLER_CREATE handler for user creation commands.
/// Validates, persists initial document, publishes ProvisionUserCommand to MQ.
/// </summary>
public class CreateUserHandler : ICreateUserHandler
{
    private readonly IUserCacheRepository _cache;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserCacheRepository cache,
        IMessagePublisher publisher,
        ILogger<CreateUserHandler> logger)
    {
        _cache = cache;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<CreateUserResponse>> HandleAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        // [IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE]
        try
        {
            _logger.LogInformation(
                "[IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE] " +
                "Creating user {UserId}", request.UserId);

            // Map request to shared DTO for validation
            var dto = new UserCreatedDto
            {
                UserId = request.UserId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                InitialRoles = request.InitialRoles
            };

            // Validate using shared validator
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var validatorLogger = loggerFactory.CreateLogger<UserCreatedValidator>();
            var validator = new UserCreatedValidator(validatorLogger);
            var validationResult = await validator.ValidateAsync(dto, ct);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "[IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE] " +
                    "Validation failed for user {UserId}", request.UserId);

                return Result<CreateUserResponse>.Failure(
                    validationResult.Errors.First().ErrorMessage);
            }

            // Create initial cache entry (Pending state)
            var correlationId = Guid.NewGuid().ToString("N");
            var entry = new UserCacheEntry
            {
                Id = request.UserId,
                UserId = request.UserId,
                Email = request.Email,
                Roles = request.InitialRoles ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            await _cache.UpsertAsync(entry, ct);

            // Publish ProvisionUserCommand to MQ
            var command = new ProvisionUserCommand
            {
                IdempotencyKey = correlationId,
                User = dto
            };

            await _publisher.PublishAsync(command, ct);

            _logger.LogInformation(
                "[IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE] " +
                "User creation command published {UserId} {CorrelationId}",
                request.UserId, correlationId);

            return Result<CreateUserResponse>.Success(new CreateUserResponse
            {
                UserId = request.UserId,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE] " +
                "Error creating user {UserId}", request.UserId);

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
                        Email = request.Email,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        InitialRoles = request.InitialRoles
                    },
                    FailedStep = "CreateUserHandler",
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
                // DLQ publish failed — log and swallow
                _logger.LogCritical(
                    "[IdentityService.Api.WriterService][CreateUserHandler][BLOCK_HANDLER_CREATE] " +
                    "Failed to publish failure event to DLQ for user {UserId}", request.UserId);
            }

            return Result<CreateUserResponse>.Failure("Internal server error");
        }
    }
}
