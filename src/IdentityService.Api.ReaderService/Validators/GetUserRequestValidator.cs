// FILE: GetUserRequestValidator.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Input validation (M-IDENTITY-READER)
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_IDENTITY_READER

using FluentValidation;
using IdentityService.Api.ReaderService.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.ReaderService.Validators;

/// <summary>
/// BLOCK_VALIDATE_QUERY validator for GetUserRequest.
/// </summary>
public class GetUserRequestValidator : AbstractValidator<GetUserRequest>
{
    private readonly ILogger<GetUserRequestValidator> _logger;

    public GetUserRequestValidator(ILogger<GetUserRequestValidator> logger)
    {
        _logger = logger;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .Length(1, 100).WithMessage("UserId must be between 1 and 100 characters");
    }

    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        GetUserRequest instance, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[IdentityService.Api.ReaderService][GetUserRequestValidator][BLOCK_VALIDATE_QUERY] Validating user query {UserId}",
            instance.UserId);

        var result = await base.ValidateAsync(instance, ct);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "[IdentityService.Api.ReaderService][GetUserRequestValidator][BLOCK_VALIDATE_QUERY] Query validation failed {UserId} {@Errors}",
                instance.UserId, result.Errors);
        }

        return result;
    }
}
