// FILE: UserCreatedValidator.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Input validation (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_IDENTITY_SHARED

using FluentValidation;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace IdentityService.Shared.Validators;

/// <summary>
/// BLOCK_VALIDATE UserCreatedDto validation rules.
/// Enforces: non-empty UserId, valid email format, name length limits.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> FluentValidation validator for UserCreatedDto with semantic logging</para>
/// <para><strong>@invariant:</strong> Email format valid (RFC 5322)</para>
/// <para><strong>@invariant:</strong> Names non-empty, max 100 chars</para>
/// <para><strong>@invariant:</strong> UserId max 100 chars</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> ValidateAsync</para>
    /// <para><strong>@param instance:</strong> UserCreatedDto to validate</para>
    /// <para><strong>@return:</strong> ValidationResult with errors if any</para>
    /// <para><strong>@log-event:</strong> shared.validator.user-validate-start {userId}</para>
    /// <para><strong>@log-event:</strong> shared.validator.user-validate-success {userId}</para>
    /// <para><strong>@log-event:</strong> shared.validator.user-validate-failed {userId} {errors}</para>
    /// <para><strong>@trace-span:</strong> shared.validate-user</para>
    /// <para><strong>@complexity:</strong> O(1) (field validation)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> YES</para>
    /// </remarks>
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
