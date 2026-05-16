// FILE: CacheUpdated.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Caching layer (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_WORKER

namespace IdentityService.WorkerService.Events;

/// <summary>
/// BLOCK_CACHE_UPDATED event — internal saga event emitted after cache update completes.
/// </summary>
public record CacheUpdated
{
    public string CorrelationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
