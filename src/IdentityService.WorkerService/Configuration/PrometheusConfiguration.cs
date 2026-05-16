// FILE: PrometheusConfiguration.cs
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

public class PrometheusConfiguration
{
    /// <summary>Whether the Prometheus metrics endpoint is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Port for the metrics endpoint (default: 9090).</summary>
    public int Port { get; set; } = 9090;

    /// <summary>URL path for the metrics endpoint (default: /metrics).</summary>
    public string Url { get; set; } = "/metrics";
}
