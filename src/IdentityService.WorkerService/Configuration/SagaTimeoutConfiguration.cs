// FILE: SagaTimeoutConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Configuration and setup (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Saga orchestrator for distributed transaction processing in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga orchestrator, manages distributed transaction lifecycle)</para>
/// <para><strong>@purpose:</strong> Saga orchestrator for distributed transaction processing in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Saga state transitions are deterministic; idempotency ensures exactly-once processing</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public class SagaTimeoutConfiguration
{
    /// <summary>Development timeout (default: 5 minutes).</summary>
    public TimeSpan Development { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Staging timeout (default: 10 minutes).</summary>
    public TimeSpan Staging { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Production timeout (default: 15 minutes).</summary>
    public TimeSpan Production { get; set; } = TimeSpan.FromMinutes(15);
}
