namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Keycloak retry policy configuration for Polly HTTP resilience.
/// </summary>
public class KeycloakRetryConfiguration
{
    /// <summary>Maximum number of retry attempts (default: 3).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Initial delay in milliseconds (default: 100ms).</summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>Maximum delay in milliseconds (default: 2000ms / 2s).</summary>
    public int MaxDelayMs { get; set; } = 2000;

    /// <summary>Backoff multiplier (default: 2.0 for exponential).</summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}
