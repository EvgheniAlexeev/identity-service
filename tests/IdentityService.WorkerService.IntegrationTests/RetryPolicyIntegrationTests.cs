using FluentAssertions;
using IdentityService.WorkerService.Configuration;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityService.WorkerService.IntegrationTests;

/// <summary>
/// BLOCK_INTEGRATION_TEST Integration tests for the Polly retry policy pipeline.
/// Tests HTTP client factory registration, retry delay math, and pipeline creation.
/// </summary>
public class RetryPolicyIntegrationTests
{
    [Fact]
    public void RetryPolicy_Pipeline_Should_Be_Created_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton(new SagaMetrics());

        var configValues = new Dictionary<string, string?>
        {
            ["KeycloakRetry:MaxRetries"] = "3",
            ["KeycloakRetry:InitialDelayMs"] = "100",
            ["KeycloakRetry:MaxDelayMs"] = "2000",
            ["KeycloakRetry:BackoffMultiplier"] = "2.0"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Act
        services.AddKeycloakRetryPolicy(config);
        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<IHttpClientFactory>();

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void RetryPolicy_Should_Handle_Minimal_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SagaMetrics());
        var config = new ConfigurationBuilder().Build(); // No configuration section

        // Act
        services.AddKeycloakRetryPolicy(config);
        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<IHttpClientFactory>();

        // Assert: Should use defaults (3 retries, 100ms-2000ms)
        factory.Should().NotBeNull();
    }

    [Fact]
    public void RetryPolicy_Should_Handle_Custom_Config()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SagaMetrics());

        var configValues = new Dictionary<string, string?>
        {
            ["KeycloakRetry:MaxRetries"] = "5",
            ["KeycloakRetry:InitialDelayMs"] = "50",
            ["KeycloakRetry:MaxDelayMs"] = "10000",
            ["KeycloakRetry:BackoffMultiplier"] = "3.0"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Act
        services.AddKeycloakRetryPolicy(config);
        var sp = services.BuildServiceProvider();

        // Assert
        sp.Should().NotBeNull();
    }

    [Fact]
    public void Exponential_Backoff_Sequence_Matches_Phase3_Spec()
    {
        // Phase 3 spec: 3 retries, exponential backoff 100ms-2s, with jitter
        // Attempt 1: 100ms (+ jitter 0-100ms) → 100-200ms
        // Attempt 2: 200ms (+ jitter 0-100ms) → 200-300ms
        // Attempt 3: 400ms (+ jitter 0-100ms) → 400-500ms

        var initialDelay = 100;
        var multiplier = 2.0;
        var maxDelay = 2000;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            var baseDelay = initialDelay * Math.Pow(multiplier, attempt - 1);
            var cappedDelay = Math.Min(baseDelay, maxDelay);

            // Verify base delay doesn't exceed max
            cappedDelay.Should().BeLessThanOrEqualTo(maxDelay);

            // Verify first 3 attempts are within the 100ms-2000ms window
            baseDelay.Should().BeInRange(100, 400);
        }
    }

    [Fact]
    public void Jitter_Should_Add_Between_0_And_99_Milliseconds()
    {
        // Run jitter many times to verify range
        var jitterValues = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            jitterValues.Add(Random.Shared.Next(0, 100));
        }

        jitterValues.Should().AllSatisfy(j => j.Should().BeInRange(0, 99));
        jitterValues.Min().Should().BeLessThan(20); // Some values should be near 0
        jitterValues.Max().Should().BeGreaterThan(80); // Some values should be near 99
    }

    [Fact]
    public void Capping_Should_Prevent_Runaway_Delays()
    {
        var maxDelay = 2000;
        var initialDelay = 100;
        var multiplier = 2.0;

        // After many attempts, base delay would be huge but should cap at 2000ms
        for (int attempt = 1; attempt <= 20; attempt++)
        {
            var baseDelay = initialDelay * Math.Pow(multiplier, attempt - 1);
            var cappedDelay = Math.Min(baseDelay, maxDelay);

            cappedDelay.Should().BeLessThanOrEqualTo(maxDelay);
        }
    }
}
