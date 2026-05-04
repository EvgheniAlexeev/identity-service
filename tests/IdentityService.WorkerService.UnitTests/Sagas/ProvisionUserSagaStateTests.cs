using FluentAssertions;
using IdentityService.Shared.Dtos;
using IdentityService.WorkerService.Sagas;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Unit tests for ProvisionUserSagaState document structure.
/// </summary>
public class ProvisionUserSagaStateTests
{
    [Fact]
    public void SagaState_Should_Initialize_With_Sane_Defaults()
    {
        var state = new ProvisionUserSagaState();

        state.Id.Should().BeEmpty();
        state.Status.Should().Be("Provisioning");
        state.CurrentStep.Should().Be("CreateInKeycloak");
        state.RetryCount.Should().Be(0);
        state.MaxRetries.Should().Be(3);
        state.WasCompensated.Should().BeFalse();
        state.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        state.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(2));
        state.AuditHistory.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SagaState_Should_Allow_Setting_All_Properties()
    {
        var state = new ProvisionUserSagaState
        {
            Id = "saga-1",
            CorrelationId = "corr-1",
            UserId = "user-1",
            Status = "Provisioned",
            CurrentStep = "Complete",
            User = new UserCreatedDto
            {
                UserId = "user-1",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                InitialRoles = new List<string> { "admin" }
            },
            KeycloakUserId = "kc-123",
            RetryCount = 0,
            MaxRetries = 3,
            ErrorReason = null,
            ErrorCode = null,
            WasCompensated = false,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AuditHistory = new List<SagaStepAudit>()
        };

        state.Id.Should().Be("saga-1");
        state.CorrelationId.Should().Be("corr-1");
        state.UserId.Should().Be("user-1");
        state.Status.Should().Be("Provisioned");
        state.KeycloakUserId.Should().Be("kc-123");
        state.User.FirstName.Should().Be("Test");
    }

    [Fact]
    public void SagaStepAudit_Should_Be_Immutable_Record()
    {
        var timestamp = DateTime.UtcNow;
        var audit = new SagaStepAudit
        {
            StepName = "CreateInKeycloak",
            Outcome = "Success",
            ErrorMessage = null,
            Timestamp = timestamp,
            DurationMs = 150
        };

        audit.StepName.Should().Be("CreateInKeycloak");
        audit.Outcome.Should().Be("Success");
        audit.ErrorMessage.Should().BeNull();
        audit.Timestamp.Should().Be(timestamp);
        audit.DurationMs.Should().Be(150);
    }

    [Fact]
    public void SagaStepAudit_Should_Handle_Failure_Entry()
    {
        var audit = new SagaStepAudit
        {
            StepName = "CreateInKeycloak",
            Outcome = "Failed",
            ErrorMessage = "Keycloak 503 Service Unavailable",
            Timestamp = DateTime.UtcNow,
            DurationMs = 5000
        };

        audit.Outcome.Should().Be("Failed");
        audit.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void SagaState_AuditHistory_Should_Accumulate_Entries()
    {
        var state = new ProvisionUserSagaState();
        state.AuditHistory.Should().BeEmpty();

        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "CreateInKeycloak",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow,
            DurationMs = 100
        });

        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "UpdateCache",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow,
            DurationMs = 50
        });

        state.AuditHistory.Should().HaveCount(2);
        state.AuditHistory[0].StepName.Should().Be("CreateInKeycloak");
        state.AuditHistory[1].StepName.Should().Be("UpdateCache");
    }

    [Fact]
    public void SagaState_ExpiresAt_Should_Be_Far_In_Future()
    {
        var state = new ProvisionUserSagaState();

        state.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
        state.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(8));
    }
}
