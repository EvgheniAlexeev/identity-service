// FILE: UserCreatedInKeycloak.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Keycloak integration (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.Shared.Dtos;

namespace IdentityService.WorkerService.Events;

/// <summary>
/// Component of the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (component)</para>
/// <para><strong>@purpose:</strong> Component of the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All properties are immutable after construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public record UserCreatedInKeycloak
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string>? Roles { get; init; }
    public DateTime CreatedAt { get; init; }
}
