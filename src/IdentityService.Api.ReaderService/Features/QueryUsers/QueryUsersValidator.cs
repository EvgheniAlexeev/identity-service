// FILE: QueryUsersValidator.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-READER
// PURPOSE: Input validation (M-IDENTITY-READER)
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_IDENTITY_READER

// FILE: src/IdentityService.Api.ReaderService/Features/QueryUsers/QueryUsersValidator.cs
// VERSION: 1.0.0

using FluentValidation;

namespace IdentityService.Api.ReaderService.Features.QueryUsers;

/// <summary>
/// Validator for QueryUsers feature requests.
/// VSA feature: QueryUsers (ReaderService)
/// </summary>
public class QueryUsersValidator : AbstractValidator<QueryUsersByStatusRequest>
{
    public QueryUsersValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(64)
            .WithMessage("Status must be between 1 and 64 characters");
    }
}
