using FluentValidation;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Shared.Validators;

/// <summary>
/// BLOCK_VALIDATE UserCreatedDto validation rules.
/// Enforces: non-empty UserId, valid email format, name length limits.
/// </summary>
public class UserCreatedValidator : AbstractValidator<UserCreatedDto>
{
    private readonly ILogger<UserCreatedValidator> _logger;

    public UserCreatedValidator(ILogger<UserCreatedValidator> logger)
    {
        _logger = logger;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .Length(1, 100).WithMessage("UserId must be between 1 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required")
            .MaximumLength(100).WithMessage("FirstName must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required")
            .MaximumLength(100).WithMessage("LastName must not exceed 100 characters");

        RuleForEach(x => x.InitialRoles)
            .NotEmpty().WithMessage("Role names must not be empty")
            .MaximumLength(100).WithMessage("Role name must not exceed 100 characters")
            .When(x => x.InitialRoles != null);
    }

    /// <summary>
    /// Validate with semantic log markers.
    /// </summary>
    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        UserCreatedDto instance, CancellationToken ct = default)
    {
        // [IdentityService.Shared][UserCreatedValidator][BLOCK_VALIDATE] Validating user {correlationId}
        _logger.LogInformation(
            "[IdentityService.Shared][UserCreatedValidator][BLOCK_VALIDATE] Validating user {UserId}",
            instance.UserId);

        var result = await base.ValidateAsync(instance, ct);

        if (result.IsValid)
        {
            _logger.LogInformation(
                "[IdentityService.Shared][UserCreatedValidator][BLOCK_VALIDATE] User validation passed {UserId}",
                instance.UserId);
        }
        else
        {
            _logger.LogWarning(
                "[IdentityService.Shared][UserCreatedValidator][BLOCK_VALIDATE] User validation failed {UserId} {@Errors}",
                instance.UserId, result.Errors);
        }

        return result;
    }
}
