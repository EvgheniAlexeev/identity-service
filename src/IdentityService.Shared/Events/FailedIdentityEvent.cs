using IdentityService.Shared.Dtos;

namespace IdentityService.Shared.Events;

/// <summary>
/// BLOCK_FAILED_IDENTITY event — emitted when an identity operation fails.
/// Routed to DLQ for retry/disposition. Preserves full original request.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-IDENTITY-SHARED</para>
/// <para><strong>@purpose:</strong> DLQ pattern event preserving original request for manual intervention and replay</para>
/// <para><strong>@module-type:</strong> UTILITY</para>
/// <para><strong>@domain-concept:</strong> FailedIdentityEvent (event value object)</para>
/// <para><strong>@invariant:</strong> OriginalRequest must be non-null for replay capability</para>
/// <para><strong>@invariant:</strong> ErrorReason must be descriptive</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED-ID</para>
/// </remarks>
public record FailedIdentityEvent : IEvent
{
    /// <summary>Correlation ID of the failed operation.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>User ID that was being processed.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Full original request preserved for retry.</summary>
    public UserCreatedDto OriginalRequest { get; init; } = new();

    /// <summary>The step that failed (e.g., "CreateInKeycloak", "UpdateCache").</summary>
    public string FailedStep { get; init; } = string.Empty;

    /// <summary>Human-readable error description.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Machine-readable error code.</summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Number of retry attempts so far.</summary>
    public int RetryCount { get; init; }

    /// <summary>Timestamp of the failure.</summary>
    public DateTime FailedAt { get; init; }

    /// <summary>Error classification for disposition routing.</summary>
    public IdentityFailureCause Cause { get; init; }
}
