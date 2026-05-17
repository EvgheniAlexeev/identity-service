// FILE: KeycloakRetryPolicy.cs
// VERSION: 2.0.0
// MODULE: M-IDENTITY-WORKER
// PURPOSE: Keycloak integration (M-IDENTITY-WORKER)
// SEMANTIC_TAG: [SERVICE, BUSINESS_LOGIC]
// START_MODULE M_IDENTITY_WORKER

using IdentityService.WorkerService.Configuration;
using IdentityService.WorkerService.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace IdentityService.WorkerService.Policies;

/// <summary>
/// Retry/resilience policy for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (resilience policy)</para>
/// <para><strong>@purpose:</strong> Retry/resilience policy for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Retry policies respect exponential backoff with jitter</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
static class KeycloakRetryPolicy
{
    /// <summary>
    /// Configures Polly retry policies on Keycloak HttpClients.
    /// Must be called after AddKeycloakAdapter for the policy to wrap the correct client.
    /// </summary>
    public static IServiceCollection AddKeycloakRetryPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var retryConfig = configuration
            .GetSection("KeycloakRetry")
            .Get<RetryPolicyConfiguration>() ?? new RetryPolicyConfiguration();

        services.AddHttpClient(KeycloakRetryConstants.ClientName)
            .AddPolicyHandler((sp, _) =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KeycloakRetryPolicy");
                var metrics = sp.GetRequiredService<SagaMetrics>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutException>()
                    .Or<OperationCanceledException>()
                    .WaitAndRetryAsync(
                        retryCount: retryConfig.MaxRetries,
                        sleepDurationProvider: retryAttempt =>
                        {
                            var baseDelay = retryConfig.InitialDelayMs *
                                Math.Pow(retryConfig.BackoffMultiplier, retryAttempt - 1);
                            var cappedDelay = Math.Min(baseDelay, retryConfig.MaxDelayMs);
                            var jitter = Random.Shared.Next(0, 100);
                            var totalDelay = TimeSpan.FromMilliseconds(cappedDelay + jitter);

                            return totalDelay;
                        },
                        onRetryAsync: async (outcome, timespan, retryCount, context) =>
                        {
                            metrics.IncrementKeycloakRetry();

                            logger.LogWarning(
                                "[IdentityService.WorkerService][KeycloakRetryPolicy][BLOCK_RETRY_POLICY] " +
                                "Keycloak HTTP retry {RetryAttempt}/{MaxRetries} after {DelayMs}ms. " +
                                "StatusCode: {StatusCode}, Error: {ErrorMessage}",
                                retryCount,
                                retryConfig.MaxRetries,
                                timespan.TotalMilliseconds,
                                outcome.Result?.StatusCode,
                                outcome.Exception?.Message ?? "none");

                            if (retryCount >= retryConfig.MaxRetries)
                            {
                                metrics.IncrementKeycloakRetryExhausted();
                                logger.LogError(
                                    "[IdentityService.WorkerService][KeycloakRetryPolicy][BLOCK_RETRY_EXHAUSTED] " +
                                    "All {MaxRetries} retries exhausted for Keycloak call. " +
                                    "Final error: {ErrorMessage}",
                                    retryConfig.MaxRetries,
                                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                            }

                            await Task.CompletedTask;
                        });
            });

        return services;
    }
}

/// <summary>
/// Constants for named HTTP client registration in the retry policy.
/// </summary>
public static class KeycloakRetryConstants
{
    public const string ClientName = "KeycloakRetryClient";
}
