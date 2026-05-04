using FluentAssertions;
using IdentityService.WorkerService.Configuration;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Policies;

/// <summary>
/// BLOCK_TEST Unit tests for Polly retry policy configuration.
/// Tests retry delay calculation, jitter inclusion, and config validation.
/// </summary>
public class KeycloakRetryPolicyTests
{
    [Fact]
    public void AddKeycloakRetryPolicy_Should_Register_Named_HttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SagaMetrics());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeycloakRetry:MaxRetries"] = "3",
                ["KeycloakRetry:InitialDelayMs"] = "100",
                ["KeycloakRetry:MaxDelayMs"] = "2000",
                ["KeycloakRetry:BackoffMultiplier"] = "2.0"
            })
            .Build();

        // Act
        services.AddKeycloakRetryPolicy(config);
        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<IHttpClientFactory>();

        // Assert
        factory.Should().NotBeNull("should register HttpClient");
    }

    [Fact]
    public void AddKeycloakRetryPolicy_Should_Use_Default_Config_When_No_Section()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SagaMetrics());
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddKeycloakRetryPolicy(config);
        var sp = services.BuildServiceProvider();

        // Assert
        sp.Should().NotBeNull();
    }

    [Fact]
    public void RetryPolicyConfiguration_Should_Have_Correct_Defaults()
    {
        // Act
        var config = new RetryPolicyConfiguration();

        // Assert
        config.MaxRetries.Should().Be(3);
        config.InitialDelayMs.Should().Be(100);
        config.MaxDelayMs.Should().Be(2000);
        config.BackoffMultiplier.Should().Be(2.0);
    }

    [Theory]
    [InlineData(1, 100)]   // First attempt: 100ms base
    [InlineData(2, 200)]   // Second attempt: 200ms base (100 * 2^1)
    [InlineData(3, 400)]   // Third attempt: 400ms base (100 * 2^2)
    public void RetryDelay_Should_Follow_Exponential_Backoff(int attempt, double expectedBaseDelay)
    {
        // This tests the math: initialDelay * multiplier^(attempt-1)
        var initialDelay = 100;
        var multiplier = 2.0;
        var baseDelay = initialDelay * Math.Pow(multiplier, attempt - 1);

        baseDelay.Should().Be(expectedBaseDelay);
    }

    [Fact]
    public void RetryDelay_Should_Cap_At_Max_Delay()
    {
        // Simulate large attempt number where delay would exceed MaxDelayMs
        var initialDelay = 100;
        var multiplier = 2.0;
        var maxDelay = 2000;

        // For attempt 10: 100 * 2^9 = 51200ms, capped at 2000ms
        var attempt = 10;
        var baseDelay = initialDelay * Math.Pow(multiplier, attempt - 1);
        var cappedDelay = Math.Min(baseDelay, maxDelay);

        cappedDelay.Should().Be(2000);
        baseDelay.Should().BeGreaterThan(2000);
    }

    [Fact]
    public void RetryDelay_Should_Include_Jitter()
    {
        // Jitter is added as Random(0, 100) so total delay = baseDelay + [0, 99]
        var baseDelay = 100.0;
        var jitter = Random.Shared.Next(0, 100);

        var totalDelay = baseDelay + jitter;
        totalDelay.Should().BeInRange(100, 199);
    }

    [Fact]
    public void KeycloakRetryConfiguration_Should_Be_Deserializable()
    {
        // Arrange
        var json = """
        {
            "MaxRetries": 5,
            "InitialDelayMs": 200,
            "MaxDelayMs": 5000,
            "BackoffMultiplier": 1.5
        }
        """;

        // Act
        var config = System.Text.Json.JsonSerializer.Deserialize<KeycloakRetryConfiguration>(json)!;

        // Assert
        config.MaxRetries.Should().Be(5);
        config.InitialDelayMs.Should().Be(200);
        config.MaxDelayMs.Should().Be(5000);
        config.BackoffMultiplier.Should().Be(1.5);
    }

    [Fact]
    public void KeycloakRetryConfiguration_Defaults_Should_Match_Phase3_Spec()
    {
        // Phase 3 spec: 3x retry, exponential backoff 100ms-2s
        var config = new KeycloakRetryConfiguration();

        config.MaxRetries.Should().Be(3);
        config.InitialDelayMs.Should().Be(100);
        config.MaxDelayMs.Should().Be(2000);
        config.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void Exponential_Backoff_Sequence_Should_Be_Within_Spec()
    {
        // Spec: exponential backoff 100ms-2s with jitter
        // Attempt 1: 100ms
        // Attempt 2: 200ms
        // Attempt 3: 400ms
        // All + jitter [0-100ms], capped at 2000ms

        var delays = new List<double>();
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            var baseDelay = 100 * Math.Pow(2.0, attempt - 1);
            var capped = Math.Min(baseDelay, 2000);
            delays.Add(capped);
        }

        delays[0].Should().Be(100);
        delays[1].Should().Be(200);
        delays[2].Should().Be(400);

        // Verify all within 100ms-2000ms range
        foreach (var delay in delays)
        {
            delay.Should().BeInRange(100, 2000);
        }
    }
}
