// FILE: FailedIdentityEvent.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Domain event (M-IDENTITY-SHARED)
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@purpose:</strong> DLQ pattern event preserving original request for manual intervention and replay</para>
/// <para><strong>@invariant:</strong> OriginalRequest must be non-null for replay capability</para>
/// <para><strong>@invariant:</strong> ErrorReason must be descriptive</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_FAILED_IDENTITY
public record FailedIdentityEvent : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public UserCreatedDto OriginalRequest { get; init; } = new();

    public string FailedStep { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;

    public int RetryCount { get; init; }

    public DateTime FailedAt { get; init; }

    public IdentityFailureCause Cause { get; init; }
}
