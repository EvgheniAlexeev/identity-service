namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Saga timeout configuration per environment.
/// </summary>
public class SagaTimeoutConfiguration
{
    /// <summary>Development timeout (default: 5 minutes).</summary>
    public TimeSpan Development { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Staging timeout (default: 10 minutes).</summary>
    public TimeSpan Staging { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Production timeout (default: 15 minutes).</summary>
    public TimeSpan Production { get; set; } = TimeSpan.FromMinutes(15);
}
