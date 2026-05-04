using FluentAssertions;
using IdentityService.WorkerService.Configuration;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Configuration;

/// <summary>
/// BLOCK_TEST Unit tests for configuration classes.
/// </summary>
public class ConfigurationTests
{
    [Fact]
    public void SagaTimeoutConfiguration_Should_Have_Sensible_Defaults()
    {
        var config = new SagaTimeoutConfiguration();

        config.Development.Should().Be(TimeSpan.FromMinutes(5));
        config.Staging.Should().Be(TimeSpan.FromMinutes(10));
        config.Production.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void SagaTimeoutConfiguration_Development_Less_Than_Production()
    {
        var config = new SagaTimeoutConfiguration();

        config.Development.Should().BeLessThan(config.Production);
    }

    [Fact]
    public void SagaTimeoutConfiguration_Staging_Between_Dev_And_Prod()
    {
        var config = new SagaTimeoutConfiguration();

        config.Staging.Should().BeGreaterThan(config.Development);
        config.Staging.Should().BeLessThan(config.Production);
    }

    [Fact]
    public void KeycloakRetryConfiguration_Defaults_As_Specified()
    {
        var config = new KeycloakRetryConfiguration();

        config.MaxRetries.Should().Be(3);
        config.InitialDelayMs.Should().Be(100);
        config.MaxDelayMs.Should().Be(2000);
        config.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void RetryPolicyConfiguration_Record_Defaults_Match_Class()
    {
        var config = new RetryPolicyConfiguration();

        config.MaxRetries.Should().Be(3);
        config.InitialDelayMs.Should().Be(100);
        config.MaxDelayMs.Should().Be(2000);
        config.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void PrometheusConfiguration_Defaults()
    {
        var config = new PrometheusConfiguration();

        config.Enabled.Should().BeTrue();
        config.Port.Should().Be(9090);
        config.Url.Should().Be("/metrics");
    }

    [Fact]
    public void PrometheusConfiguration_Can_Be_Disabled()
    {
        var config = new PrometheusConfiguration { Enabled = false };

        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void KeycloakRetryConfiguration_Can_Be_Customized()
    {
        var config = new KeycloakRetryConfiguration
        {
            MaxRetries = 5,
            InitialDelayMs = 200,
            MaxDelayMs = 5000,
            BackoffMultiplier = 1.5
        };

        config.MaxRetries.Should().Be(5);
        config.InitialDelayMs.Should().Be(200);
        config.MaxDelayMs.Should().Be(5000);
        config.BackoffMultiplier.Should().Be(1.5);
    }
}
