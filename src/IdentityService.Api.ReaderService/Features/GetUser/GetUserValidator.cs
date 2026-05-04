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
