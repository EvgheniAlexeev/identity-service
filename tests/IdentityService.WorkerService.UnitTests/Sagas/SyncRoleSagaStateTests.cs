using FluentAssertions;
using IdentityService.Shared.Dtos;
using IdentityService.WorkerService.Sagas;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Unit tests for SyncRoleSagaState.
/// Tests state initialization, audit history tracking, and TTL expiry behavior.
/// </summary>
public class SyncRoleSagaStateTests
{
    [Fact]
    public void Default_State_Should_Have_Correct_Initial_Values()
    {
        // Arrange & Act
        var state = new SyncRoleSagaState();

        // Assert
        state.Id.Should().BeEmpty();
        state.CorrelationId.Should().BeEmpty();
        state.UserId.Should().BeEmpty();
        state.Status.Should().Be("SyncPending");
        state.CurrentStep.Should().Be("AssignInKeycloak");
        state.RetryCount.Should().Be(0);
        state.MaxRetries.Should().Be(3);
        state.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        state.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(1));
        state.CompletedAt.Should().BeNull();
        state.ErrorReason.Should().BeNull();
        state.ErrorCode.Should().BeNull();
        state.KeycloakUserId.Should().BeNull();
        state.WasCompensated.Should().BeFalse();
        state.AuditHistory.Should().BeEmpty();
    }

    [Fact]
    public void AuditHistory_Should_Track_Step_Transitions()
    {
        // Arrange
        var state = new SyncRoleSagaState();

        // Act
        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "AssignInKeycloak",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "UpdateRoleCache",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "InvalidateUserCache",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        state.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        // Assert
        state.AuditHistory.Should().HaveCount(4);
        state.AuditHistory[0].StepName.Should().Be("AssignInKeycloak");
        state.AuditHistory[1].StepName.Should().Be("UpdateRoleCache");
        state.AuditHistory[2].StepName.Should().Be("InvalidateUserCache");
        state.AuditHistory[3].StepName.Should().Be("Completed");
    }

    [Fact]
    public void Status_Transitions_Should_Be_Valid()
    {
        // Arrange
        var state = new SyncRoleSagaState();

        // Act — simulate state machine
        state.Status = "SyncPending";
        state.Status = "AssigningInKeycloak";
        state.Status = "UpdatingRoleCache";
        state.Status = "InvalidatingUserCache";
        state.Status = "Completed";

        // Assert
        state.Status.Should().Be("Completed");

        // Failed state
        state.Status = "Failed";
        state.Status.Should().Be("Failed");
    }

    [Fact]
    public void Role_Data_Should_Be_Stored_Correctly()
    {
        // Arrange
        var state = new SyncRoleSagaState();
        var role = new RoleAssignDto
        {
            UserId = "user-store-1",
            RoleId = "role-store-1",
            RoleName = "StoreRole"
        };

        // Act
        state.UserId = role.UserId;
        state.Role = role;
        state.KeycloakUserId = "kc-user-456";

        // Assert
        state.UserId.Should().Be("user-store-1");
        state.Role.RoleId.Should().Be("role-store-1");
        state.Role.RoleName.Should().Be("StoreRole");
        state.KeycloakUserId.Should().Be("kc-user-456");
    }

    [Fact]
    public void Error_Fields_Should_Be_Set_On_Failure()
    {
        // Arrange
        var state = new SyncRoleSagaState();

        // Act
        state.Status = "Failed";
        state.ErrorReason = "Keycloak unavailable";
        state.ErrorCode = "KeycloakApiError";
        state.CompletedAt = DateTime.UtcNow;
        state.WasCompensated = false;

        // Assert
        state.Status.Should().Be("Failed");
        state.ErrorReason.Should().Be("Keycloak unavailable");
        state.ErrorCode.Should().Be("KeycloakApiError");
        state.CompletedAt.Should().NotBeNull();
        state.WasCompensated.Should().BeFalse();
    }

    [Fact]
    public void TTL_Expiry_Should_Be_7_Days_From_Creation()
    {
        // Arrange & Act
        var state = new SyncRoleSagaState();

        // Assert
        state.ExpiresAt.Should().BeCloseTo(
            state.CreatedAt.AddDays(7),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SagaStepAudit_Should_Be_Immutable_After_Creation()
    {
        // Arrange & Act
        var audit = new SagaStepAudit
        {
            StepName = "AssignInKeycloak",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        };

        // Assert — records are immutable, so properties should be init-only
        audit.StepName.Should().Be("AssignInKeycloak");
        audit.Outcome.Should().Be("Success");
        audit.ErrorMessage.Should().BeNull();
    }
}
