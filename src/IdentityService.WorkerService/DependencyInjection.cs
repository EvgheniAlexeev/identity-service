// FILE: DependencyInjection.cs
// VERSION: 2.1.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: M-IDENTITY-WORKER component
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.WorkerService.Configuration;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Policies;
using IdentityService.WorkerService.Sagas;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace IdentityService.WorkerService;

/// <summary>
/// BLOCK_DI dependency injection registration for the WorkerService module.
/// Registers Wolverine saga, step handlers, Polly retry, Prometheus metrics, and DLQ publishing.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkerService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- CONFIGURATION ----
        services.Configure<RetryPolicyConfiguration>(
            configuration.GetSection("KeycloakRetry"));

        // ---- PROMETHEUS METRICS ----
        services.AddSingleton<SagaMetrics>();

        // ---- POLLY RETRY POLICY FOR KEYCLOAK ----
        services.AddKeycloakRetryPolicy(configuration);

        // ---- WOLVERINE SAGA STEPS ----
        services.AddScoped<CreateUserInKeycloakHandler>();
        services.AddScoped<UpdateUserCacheHandler>();
        services.AddScoped<NotifyAdminsHandler>();

        // ---- SAGA ORCHESTRATOR ----
        services.AddScoped<ProvisionUserSaga>();

        // ---- WOLVERINE (in-memory for unit tests, replace with Dapr for production) ----
        // Guard: AddWolverine can only be called once per service collection (Wolverine constraint).
        if (!services.Any(sd => sd.ServiceType == typeof(WolverineOptions)))
        {
            services.AddWolverine(opts =>
        {
            // Register saga and handlers
            opts.Discovery.IncludeType<ProvisionUserSaga>();
            opts.Discovery.IncludeType<CreateUserInKeycloakHandler>();
            opts.Discovery.IncludeType<UpdateUserCacheHandler>();
            opts.Discovery.IncludeType<NotifyAdminsHandler>();
        });
        }

        return services;
    }
}
