namespace IdentityService.CacheLayer.MongoDB;

/// <summary>
/// MongoDB cache entry for a user. Stored in the "user_cache" collection.
/// TTL index on ExpiresAt auto-deletes expired entries.
/// </summary>
public record UserCacheEntry
{
    /// <summary>MongoDB _id (equals userId for easy lookup).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>User identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>User email (PII — redact in logs).</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Assigned role names.</summary>
    public List<string> Roles { get; init; } = new();

    /// <summary>Cache creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>TTL field — MongoDB auto-deletes after this time.</summary>
    public DateTime ExpiresAt { get; init; }
}
