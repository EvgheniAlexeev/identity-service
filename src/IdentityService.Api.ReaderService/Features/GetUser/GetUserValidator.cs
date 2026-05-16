// FILE: GetUserValidator.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Input validation (M-IDENTITY-READER)
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/GetUser/GetUserValidator.cs
// VERSION: 1.0.0

using FluentValidation;

namespace IdentityService.Api.ReaderService.Features.GetUser;

/// <summary>
/// Validator for GetUser feature requests.
/// VSA feature: GetUser (ReaderService)
/// </summary>
public class GetUserValidator : AbstractValidator<GetUserRequest>
{
    public GetUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(128)
            .WithMessage("UserId must be between 1 and 128 characters");
    }
}
