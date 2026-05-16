// FILE: IdentityFailureCause.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [EVENT, MESSAGE]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Events;

/// <summary>
/// BLOCK_FAILURE_CAUSE classification for DLQ routing decisions.
/// </summary>
public enum IdentityFailureCause
{
    /// <summary>Unknown or unclassified failure.</summary>
    Unknown = 0,

    /// <summary>Keycloak API returned an error (retryable).</summary>
    KeycloakApiError = 1,

    /// <summary>Keycloak user already exists (idempotent — skip).</summary>
    UserAlreadyExists = 2,

    /// <summary>Cache write failed (retryable after delay).</summary>
    CacheWriteFailure = 3,

    /// <summary>Validation failed — request is invalid (non-retryable).</summary>
    ValidationError = 4,

    /// <summary>Network timeout — transient (retryable).</summary>
    NetworkTimeout = 5,

    /// <summary>Max retries exceeded — dead letter.</summary>
    MaxRetriesExceeded = 6
}
