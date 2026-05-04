using FluentAssertions;
using IdentityService.WorkerService.Sagas;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Additional boundary tests for ProvisionUserSaga state behaviors.
/// </summary>
public class ProvisionUserSagaEdgeCaseTests
{
    [Fact]
    public void SagaState_Default_User_Is_Not_Null()
    {
        var state = new ProvisionUserSagaState();
        state.User.Should().NotBeNull();
    }

    [Fact]
    public void SagaState_Default_AuditHistory_Is_Empty_List()
    {
        var state = new ProvisionUserSagaState();
        state.AuditHistory.Should().NotBeNull();
        state.AuditHistory.Should().BeEmpty();
    }

    [Fact]
    public void SagaState_Can_Transition_Through_All_Statuses()
    {
        var state = new ProvisionUserSagaState();
        state.Status.Should().Be("Provisioning");

        state.Status = "Provisioned";
        state.Status.Should().Be("Provisioned");

        state.Status = "Failed";
        state.Status.Should().Be("Failed");
    }

    [Fact]
    public void SagaState_Can_Transition_Through_All_Steps()
    {
        var state = new ProvisionUserSagaState();
        state.CurrentStep.Should().Be("CreateInKeycloak");

        state.CurrentStep = "UpdateCache";
        state.CurrentStep.Should().Be("UpdateCache");

        state.CurrentStep = "Notify";
        state.CurrentStep.Should().Be("Notify");

        state.CurrentStep = "Complete";
        state.CurrentStep.Should().Be("Complete");
    }

    [Fact]
    public void SagaState_WasCompensated_Defaults_To_False()
    {
        var state = new ProvisionUserSagaState();
        state.WasCompensated.Should().BeFalse();
    }

    [Fact]
    public void SagaState_ErrorProperties_Are_Null_By_Default()
    {
        var state = new ProvisionUserSagaState();
        state.ErrorReason.Should().BeNull();
        state.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void SagaState_CompletedAt_Is_Null_By_Default()
    {
        var state = new ProvisionUserSagaState();
        state.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void SagaStepAudit_With_All_Failure_Outcomes()
    {
        var outcomes = new[] { "Success", "Failed", "Skipped", "Retrying" };
        foreach (var outcome in outcomes)
        {
            var audit = new SagaStepAudit
            {
                StepName = "TestStep",
                Outcome = outcome,
                ErrorMessage = outcome == "Failed" ? "test error" : null,
                Timestamp = DateTime.UtcNow,
                DurationMs = 42
            };

            audit.Outcome.Should().Be(outcome);
        }
    }
}
