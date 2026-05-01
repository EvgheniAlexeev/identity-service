using FluentValidation;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Shared.Validators;

/// <summary>
/// BLOCK_VALIDATE RoleAssignDto validation rules.
/// Enforces: non-empty UserId and RoleId.
/// </summary>
public class RoleAssignValidator : AbstractValidator<RoleAssignDto>
{
    private readonly ILogger<RoleAssignValidator> _logger;

    public RoleAssignValidator(ILogger<RoleAssignValidator> logger)
    {
        _logger = logger;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .Length(1, 100).WithMessage("UserId must be between 1 and 100 characters");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required")
            .Length(1, 100).WithMessage("RoleId must be between 1 and 100 characters");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("RoleName is required")
            .MaximumLength(100).WithMessage("RoleName must not exceed 100 characters");
    }

    /// <summary>
    /// Validate with semantic log markers.
    /// </summary>
    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        RoleAssignDto instance, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.Shared][RoleAssignValidator][BLOCK_VALIDATE] Validating role assignment {UserId} {RoleId}",
            instance.UserId, instance.RoleId);

        var result = await base.ValidateAsync(instance, ct);

        if (result.IsValid)
        {
            _logger.LogInformation(
                "[IdentityService.Shared][RoleAssignValidator][BLOCK_VALIDATE] Role assignment validation passed {UserId} {RoleId}",
                instance.UserId, instance.RoleId);
        }
        else
        {
            _logger.LogWarning(
                "[IdentityService.Shared][RoleAssignValidator][BLOCK_VALIDATE] Role assignment validation failed {UserId} {RoleId} {@Errors}",
                instance.UserId, instance.RoleId, result.Errors);
        }

        return result;
    }
}
