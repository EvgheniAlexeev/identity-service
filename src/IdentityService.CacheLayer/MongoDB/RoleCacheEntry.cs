namespace IdentityService.CacheLayer.MongoDB;

/// <summary>
/// MongoDB cache entry for a role. Stored in the "role_cache" collection.
/// TTL index on ExpiresAt auto-deletes expired entries.
/// </summary>
public record RoleCacheEntry
{
    /// <summary>MongoDB _id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Role identifier.</summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>Human-readable role name.</summary>
    public string RoleName { get; init; } = string.Empty;

    /// <summary>Permission strings for this role.</summary>
    public List<string> Permissions { get; init; } = new();

    /// <summary>Cache creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>TTL field — MongoDB auto-deletes after this time.</summary>
    public DateTime ExpiresAt { get; init; }
}
