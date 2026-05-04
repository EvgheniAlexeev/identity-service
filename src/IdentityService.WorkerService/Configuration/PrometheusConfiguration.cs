namespace IdentityService.WorkerService.Configuration;

/// <summary>
/// Prometheus metrics endpoint configuration.
/// </summary>
public class PrometheusConfiguration
{
    /// <summary>Whether the Prometheus metrics endpoint is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Port for the metrics endpoint (default: 9090).</summary>
    public int Port { get; set; } = 9090;

    /// <summary>URL path for the metrics endpoint (default: /metrics).</summary>
    public string Url { get; set; } = "/metrics";
}
