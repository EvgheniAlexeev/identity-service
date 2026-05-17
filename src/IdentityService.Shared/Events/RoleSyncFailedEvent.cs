// FILE: RoleSyncFailedEvent.cs
// VERSION: 1.0.0
// MODULE: M-SHARED
// PURPOSE: DLQ event for role sync saga failures

using IdentityService.Shared.Dtos;
using IdentityService.Shared.Events;

namespace IdentityService.Shared.Events;

/// <summary>
/// DLQ pattern event for SyncRoleSaga failures. Preserves original role assignment request
/// for manual intervention and replay. Mirrors FailedIdentityEvent but for role operations.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> DLQ event preserving role sync failure context for operator review and replay</para>
/// <para><strong>@invariant:</strong> OriginalRequest must be non-null for replay capability</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </remarks>
public record RoleSyncFailedEvent : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public RoleAssignDto OriginalRequest { get; init; } = new();

    public string FailedStep { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;

    public int RetryCount { get; init; }

    public DateTime FailedAt { get; init; }

    public IdentityFailureCause Cause { get; init; }
}
