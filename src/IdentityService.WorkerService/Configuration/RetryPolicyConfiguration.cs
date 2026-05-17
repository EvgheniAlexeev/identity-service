// FILE: RetryPolicyConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Configuration and setup (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Configuration model for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (configuration model)</para>
/// <para><strong>@purpose:</strong> Configuration model for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Configuration values have sensible defaults; validated at startup</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
/// <remarks>
/// <para><strong>@note:</strong> This configuration record is shared across ProvisionUserSaga and SyncRoleSaga.</para>
/// </remarks>
public record RetryPolicyConfiguration
{
    public int MaxRetries { get; init; } = 3;
    public int InitialDelayMs { get; init; } = 100;
    public int MaxDelayMs { get; init; } = 2000;
    public double BackoffMultiplier { get; init; } = 2.0;
}
