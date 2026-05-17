// FILE: CacheUpdated.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Caching layer (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_WORKER

namespace IdentityService.WorkerService.Events;

/// <summary>
/// Domain event published during saga execution in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (domain event, immutable value object)</para>
/// <para><strong>@purpose:</strong> Domain event published during saga execution in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Immutable domain event; all properties set at construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public record CacheUpdated
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
