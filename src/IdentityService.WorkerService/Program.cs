// FILE: Program.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: M-IDENTITY-WORKER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

// START_MODULE_CONTRACT
//   PURPOSE: Saga processor service — ProvisionUserSaga (Keycloak create→cache→notify+DLQ),
//            Polly retry policy for Keycloak HTTP calls, Prometheus metrics, DLQ integration.
//   SCOPE: Wolverine saga orchestration, Polly HTTP retry, Prometheus metrics endpoints,
//          DLQ publishing for failed saga steps, role-based DLQ access audit history.
//   DEPENDS: M-SHARED (IdentityService.Shared), M-KEYCLOAK (IdentityService.KeycloakAdapter),
//            M-CACHE (IdentityService.CacheLayer)
// END_MODULE_CONTRACT

using IdentityService.WorkerService;
using IdentityService.WorkerService.Configuration;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ---- SERILOG ----
builder.Services.AddSerilog((sp, lc) =>
{
    lc.ReadFrom.Configuration(builder.Configuration)
      .Enrich.FromLogContext()
      .WriteTo.Console();
});

// ---- CONFIGURATION ----
builder.Services.Configure<SagaTimeoutConfiguration>(
    builder.Configuration.GetSection("Wolverine:SagaTimeout"));
builder.Services.Configure<KeycloakRetryConfiguration>(
    builder.Configuration.GetSection("KeycloakRetry"));
builder.Services.Configure<PrometheusConfiguration>(
    builder.Configuration.GetSection("Prometheus"));

// ---- MODULE DI ----
builder.Services.AddWorkerService(
    builder.Configuration);

// ---- PROMETHEUS ----
var promConfig = builder.Configuration
    .GetSection("Prometheus")
    .Get<PrometheusConfiguration>() ?? new PrometheusConfiguration();

if (promConfig.Enabled)
{
    var metricServer = new KestrelMetricServer(
        port: promConfig.Port,
        url: promConfig.Url);
    metricServer.Start();
}

var app = builder.Build();

Log.Information(
    "[IdentityService.WorkerService][Program][STARTUP] " +
    "Worker service starting. Prometheus metrics on port {Port}",
    promConfig.Port);

await app.RunAsync();
