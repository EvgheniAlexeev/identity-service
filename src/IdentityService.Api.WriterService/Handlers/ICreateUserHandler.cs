// FILE: ICreateUserHandler.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WRITER
// PURPOSE: Business logic handler (M-IDENTITY-WRITER)
// SEMANTIC_TAG: [HANDLER, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WRITER

using IdentityService.Api.WriterService.Models;

namespace IdentityService.Api.WriterService.Handlers;

/// <summary>
/// Handler interface for user command operations.
/// </summary>
public interface ICreateUserHandler
{
    /// <summary>
    /// Handle a user creation command: validate, publish to MQ, return 202.
    /// </summary>
    Task<Result<CreateUserResponse>> HandleAsync(CreateUserRequest request, CancellationToken ct = default);
}

/// <summary>
/// Handler interface for role assignment operations.
/// </summary>
public interface IAssignRoleHandler
{
    /// <summary>
    /// Handle a role assignment command: validate, publish to MQ, return 202.
    /// </summary>
    Task<Result<AssignRoleResponse>> HandleAsync(AssignRoleRequest request, CancellationToken ct = default);
}
