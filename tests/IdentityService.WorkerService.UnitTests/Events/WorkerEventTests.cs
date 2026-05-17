using FluentAssertions;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Steps;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Events;

/// <summary>
/// BLOCK_TEST Unit tests for worker service internal events.
/// </summary>
public class WorkerEventTests
{
    [Fact]
    public void UserCreatedInKeycloak_Should_Initialize_With_Defaults()
    {
        var evt = new UserCreatedInKeycloak();

        evt.CorrelationId.Should().BeEmpty();
        evt.UserId.Should().BeEmpty();
        evt.KeycloakUserId.Should().BeEmpty();
        evt.Email.Should().BeEmpty();
        evt.FirstName.Should().BeEmpty();
        evt.LastName.Should().BeEmpty();
        evt.Roles.Should().BeNull();
    }

    [Fact]
    public void UserCreatedInKeycloak_Should_Store_All_Properties()
    {
        var evt = new UserCreatedInKeycloak
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            KeycloakUserId = "kc-123",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "admin", "reader" },
            CreatedAt = DateTime.UtcNow
        };

        evt.CorrelationId.Should().Be("corr-1");
        evt.UserId.Should().Be("user-1");
        evt.KeycloakUserId.Should().Be("kc-123");
        evt.Email.Should().Be("test@example.com");
        evt.FirstName.Should().Be("Test");
        evt.LastName.Should().Be("User");
        evt.Roles.Should().HaveCount(2).And.Contain(new[] { "admin", "reader" });
    }

    [Fact]
    public void CacheUpdated_Should_Initialize_With_Defaults()
    {
        var evt = new CacheUpdated();

        evt.CorrelationId.Should().BeEmpty();
        evt.UserId.Should().BeEmpty();
    }

    [Fact]
    public void CacheUpdated_Should_Store_All_Properties()
    {
        var now = DateTime.UtcNow;
        var evt = new CacheUpdated
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            UpdatedAt = now
        };

        evt.CorrelationId.Should().Be("corr-1");
        evt.UserId.Should().Be("user-1");
        evt.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void SagaStepCommands_CreateUser_Should_Have_Sensible_Defaults()
    {
        var cmd = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-1",
            User = new IdentityService.Shared.Dtos.UserCreatedDto
            {
                UserId = "user-1",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            },
            Attempt = 1
        };

        cmd.CorrelationId.Should().Be("corr-1");
        cmd.Attempt.Should().Be(1);
        cmd.User.UserId.Should().Be("user-1");
    }

    [Fact]
    public void SagaStepCommands_UpdateCache_Should_Have_Sensible_Defaults()
    {
        var cmd = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            KeycloakUserId = "kc-1",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "admin" }
        };

        cmd.CorrelationId.Should().Be("corr-1");
        cmd.UserId.Should().Be("user-1");
        cmd.KeycloakUserId.Should().Be("kc-1");
        cmd.Roles.Should().ContainSingle("admin");
    }

    [Fact]
    public void SagaStepCommands_Notify_Should_Have_Sensible_Defaults()
    {
        var cmd = new NotifyCommand
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            Email = "test@example.com",
            Status = "Provisioned"
        };

        cmd.CorrelationId.Should().Be("corr-1");
        cmd.UserId.Should().Be("user-1");
        cmd.Status.Should().Be("Provisioned");
    }
}
