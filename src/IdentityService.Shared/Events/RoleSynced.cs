namespace IdentityService.Shared.Events;

/// <summary>
/// BLOCK_ROLE_SYNCED event — emitted when a role is synced to the cache.
/// </summary>
public record RoleSynced : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime SyncedAt { get; init; }
}
