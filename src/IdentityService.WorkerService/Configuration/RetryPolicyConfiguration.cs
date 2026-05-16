// FILE: RetryPolicyConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Configuration and setup (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Retry policy configuration record used at the service level.
/// </summary>
public record RetryPolicyConfiguration
{
    public int MaxRetries { get; init; } = 3;
    public int InitialDelayMs { get; init; } = 100;
    public int MaxDelayMs { get; init; } = 2000;
    public double BackoffMultiplier { get; init; } = 2.0;
}
