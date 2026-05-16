// FILE: UserDocument.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-SHARED
// PURPOSE: M-IDENTITY-SHARED component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_SHARED

namespace IdentityService.Shared.Models;

/// <summary>
/// MongoDB document for user persistence.
/// </summary>
public record UserDocument
{
    /// <summary>MongoDB _id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Business key — unique user identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>User email (PII — redact in logs).</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>User first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>User last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Keycloak user identifier (populated after provisioning).</summary>
    public string? KeycloakUserId { get; init; }

    /// <summary>Assigned role names.</summary>
    public List<string> Roles { get; init; } = new();

    /// <summary>Provisioning status.</summary>
    public string Status { get; init; } = "Pending";

    /// <summary>Current saga step.</summary>
    public string SagaState { get; init; } = "Provisioning";

    /// <summary>Document creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; init; }
}
