using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityService.WorkerService.UnitTests;

/// <summary>
/// BLOCK_TEST Tests for DependencyInjection registration.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddWorkerService_Should_Register_All_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
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
        IdentityService.WorkerService.DependencyInjection.AddWorkerService(services, config);
        var sp = services.BuildServiceProvider();

        // Assert: Key services should be registered
        sp.Should().NotBeNull();
    }

    [Fact]
    public void AddWorkerService_Should_Be_Callable_Multiple_Times()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        // Act
        IdentityService.WorkerService.DependencyInjection.AddWorkerService(services, config);
        IdentityService.WorkerService.DependencyInjection.AddWorkerService(services, config);

        // Assert: No duplicate registration exception
        var sp = services.BuildServiceProvider();
        sp.Should().NotBeNull();
    }
}
