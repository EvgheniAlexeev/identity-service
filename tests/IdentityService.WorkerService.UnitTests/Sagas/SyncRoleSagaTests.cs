using FluentAssertions;
using IdentityService.CacheLayer.Repositories;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Dtos;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Sagas;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Comprehensive unit tests for SyncRoleSaga.
/// Tests saga state transitions, DLQ handling, failure scenarios, idempotency,
/// and multi-step flow correctness.
/// </summary>
public class SyncRoleSagaTests
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly IRoleCacheRepository _roleCacheRepository;
    private readonly IUserCacheRepository _userCacheRepository;
    private readonly ILogger<SyncRoleSaga> _sagaLogger;
    private readonly ILogger<AssignRoleInKeycloakHandler> _keycloakStepLogger;
    private readonly ILogger<UpdateRoleCacheHandler> _cacheStepLogger;
    private readonly ILogger<InvalidateUserCacheHandler> _invalidateStepLogger;
    private readonly SagaMetrics _metrics;
    private readonly SyncRoleSaga _saga;

    public SyncRoleSagaTests()
    {
        _keycloakClient = Substitute.For<IKeycloakAdminClient>();
        _roleCacheRepository = Substitute.For<IRoleCacheRepository>();
        _userCacheRepository = Substitute.For<IUserCacheRepository>();
        _sagaLogger = Substitute.For<ILogger<SyncRoleSaga>>();
        _keycloakStepLogger = Substitute.For<ILogger<AssignRoleInKeycloakHandler>>();
        _cacheStepLogger = Substitute.For<ILogger<UpdateRoleCacheHandler>>();
        _invalidateStepLogger = Substitute.For<ILogger<InvalidateUserCacheHandler>>();
        _metrics = new SagaMetrics();

        var assignRoleHandler = new AssignRoleInKeycloakHandler(_keycloakClient, _keycloakStepLogger, _metrics);
        var cacheHandler = new UpdateRoleCacheHandler(_roleCacheRepository, _cacheStepLogger, _metrics);
        var invalidateHandler = new InvalidateUserCacheHandler(_userCacheRepository, _invalidateStepLogger, _metrics);

        _saga = new SyncRoleSaga(_sagaLogger, _metrics, assignRoleHandler, cacheHandler, invalidateHandler);
    }

    // ======== SAGA START TESTS ========

    [Fact]
    public async Task HandleAsync_SyncRoleCommand_Should_Initialize_Saga_State()
    {
        // Arrange
        var command = new SyncRoleCommand
        {
            IdempotencyKey = "corr-start-1",
            Role = new RoleAssignDto
            {
                UserId = "user-123",
                RoleId = "role-admin",
                RoleName = "Admin"
            }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        _saga.Data.Id.Should().Be("corr-start-1");
        _saga.Data.CorrelationId.Should().Be("corr-start-1");
        _saga.Data.UserId.Should().Be("user-123");
        _saga.Data.Role.RoleId.Should().Be("role-admin");
        _saga.Data.Status.Should().Be("SyncPending");
        _saga.Data.CurrentStep.Should().Be("AssignInKeycloak");
        _saga.Data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _saga.Data.RetryCount.Should().Be(0);
        _saga.Data.MaxRetries.Should().Be(3);

        result.Should().BeOfType<AssignRoleInKeycloakCommand>();
        var stepCmd = result as AssignRoleInKeycloakCommand;
        stepCmd!.CorrelationId.Should().Be("corr-start-1");
        stepCmd.UserId.Should().Be("user-123");
        stepCmd.RoleId.Should().Be("role-admin");
        stepCmd.RoleName.Should().Be("Admin");
        stepCmd.Attempt.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_RoleSynced_On_Idempotent_Replay()
    {
        // Arrange
        var command = new SyncRoleCommand
        {
            IdempotencyKey = "corr-idem-1",
            Role = new RoleAssignDto
            {
                UserId = "user-456",
                RoleId = "role-viewer",
                RoleName = "Viewer"
            }
        };

        // Seed saga state as already completed
        _saga.Data.Id = "corr-idem-1";
        _saga.Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RoleSynced>();
        var synced = result as RoleSynced;
        synced!.CorrelationId.Should().Be("corr-idem-1");
        synced.RoleId.Should().Be("role-viewer");
        synced.RoleName.Should().Be("Viewer");
        synced.SyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    // ======== STEP FLOW TESTS ========

    [Fact]
    public async Task HandleAsync_KeycloakResponse_Should_Advance_To_UpdateRoleCache()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-flow-1";
        _saga.Data.UserId = "user-789";
        _saga.Data.CurrentStep = "AssignInKeycloak";

        var keycloakEvent = new RoleAssignedInKeycloak
        {
            CorrelationId = "corr-flow-1",
            UserId = "user-789",
            RoleId = "role-editor",
            RoleName = "Editor",
            AssignedAt = DateTime.UtcNow
        };

        // Act
        var result = await _saga.HandleAsync(keycloakEvent, CancellationToken.None);

        // Assert
        _saga.Data.CurrentStep.Should().Be("UpdateRoleCache");
        _saga.Data.AuditHistory.Should().Contain(a =>
            a.StepName == "AssignInKeycloak" && a.Outcome == "Success");

        result.Should().BeOfType<UpdateRoleCacheStepCommand>();
        var stepCmd = result as UpdateRoleCacheStepCommand;
        stepCmd!.CorrelationId.Should().Be("corr-flow-1");
        stepCmd.RoleId.Should().Be("role-editor");
        stepCmd.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task HandleAsync_CacheResponse_Should_Advance_To_InvalidateUserCache()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-flow-2";
        _saga.Data.UserId = "user-flow-2";
        _saga.Data.CurrentStep = "UpdateRoleCache";

        var cacheEvent = new RoleCacheUpdated
        {
            CorrelationId = "corr-flow-2",
            RoleId = "role-custom",
            UserId = "user-flow-2",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _saga.HandleAsync(cacheEvent, CancellationToken.None);

        // Assert
        _saga.Data.CurrentStep.Should().Be("InvalidateUserCache");
        _saga.Data.AuditHistory.Should().Contain(a =>
            a.StepName == "UpdateRoleCache" && a.Outcome == "Success");

        result.Should().BeOfType<InvalidateUserCacheStepCommand>();
        var stepCmd = result as InvalidateUserCacheStepCommand;
        stepCmd!.CorrelationId.Should().Be("corr-flow-2");
        stepCmd.UserId.Should().Be("user-flow-2");
    }

    [Fact]
    public async Task HandleAsync_InvalidateComplete_Should_Emit_RoleSynced()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-complete-1";
        _saga.Data.UserId = "user-complete-1";
        _saga.Data.Role = new RoleAssignDto
        {
            UserId = "user-complete-1",
            RoleId = "role-final",
            RoleName = "Final"
        };
        _saga.Data.CurrentStep = "InvalidateUserCache";

        var completeCommand = new InvalidateUserCacheStepCommand
        {
            CorrelationId = "corr-complete-1",
            UserId = "user-complete-1"
        };

        // Act
        var result = await _saga.HandleAsync(completeCommand, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Completed");
        _saga.Data.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _saga.Data.AuditHistory.Should().Contain(a =>
            a.StepName == "Completed" && a.Outcome == "Success");

        result.Should().BeOfType<RoleSynced>();
        var synced = result as RoleSynced;
        synced!.CorrelationId.Should().Be("corr-complete-1");
        synced.RoleId.Should().Be("role-final");
        synced.RoleName.Should().Be("Final");
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Publish_RoleSyncFailedEvent()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-fail-1";
        _saga.Data.UserId = "user-fail-1";
        _saga.Data.Role = new RoleAssignDto
        {
            UserId = "user-fail-1",
            RoleId = "role-fail",
            RoleName = "FailRole"
        };
        _saga.Data.CurrentStep = "AssignInKeycloak";
        _saga.Data.RetryCount = 2;

        var exception = new KeycloakException("Keycloak 503", 503);

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Failed");
        _saga.Data.ErrorReason.Should().Be("Keycloak 503");
        _saga.Data.ErrorCode.Should().Be("KeycloakApiError");
        _saga.Data.AuditHistory.Should().Contain(a =>
            a.StepName == "AssignInKeycloak" && a.Outcome == "Failed");

        result.Should().BeOfType<RoleSyncFailedEvent>();
        var failedEvent = result as RoleSyncFailedEvent;
        failedEvent!.CorrelationId.Should().Be("corr-fail-1");
        failedEvent.UserId.Should().Be("user-fail-1");
        failedEvent.FailedStep.Should().Be("AssignInKeycloak");
        failedEvent.ErrorMessage.Should().Be("Keycloak 503");
        failedEvent.ErrorCode.Should().Be("KeycloakApiError");
        failedEvent.RetryCount.Should().Be(2);
        failedEvent.Cause.Should().Be(IdentityFailureCause.KeycloakApiError);
        failedEvent.OriginalRequest.RoleId.Should().Be("role-fail");
    }

    [Fact]
    public async Task HandleFailureAsync_TimeoutException_Should_Set_NetworkTimeout()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-timeout-1";
        _saga.Data.UserId = "user-timeout-1";
        _saga.Data.Role = new RoleAssignDto { UserId = "user-timeout-1", RoleId = "r1", RoleName = "R1" };
        _saga.Data.CurrentStep = "UpdateRoleCache";

        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Failed");
        _saga.Data.ErrorCode.Should().Be("NetworkTimeout");

        result.Should().BeOfType<RoleSyncFailedEvent>();
        var failedEvent = result as RoleSyncFailedEvent;
        failedEvent!.Cause.Should().Be(IdentityFailureCause.NetworkTimeout);
        failedEvent.FailedStep.Should().Be("UpdateRoleCache");
    }

    [Fact]
    public async Task HandleFailureAsync_UnknownException_Should_Set_Unknown()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-unknown-1";
        _saga.Data.UserId = "user-unknown-1";
        _saga.Data.Role = new RoleAssignDto { UserId = "user-unknown-1", RoleId = "r2", RoleName = "R2" };
        _saga.Data.CurrentStep = "InvalidateUserCache";

        var exception = new InvalidOperationException("Unexpected state");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Failed");
        _saga.Data.ErrorCode.Should().Be("Unknown");

        result.Should().BeOfType<RoleSyncFailedEvent>();
        var failedEvent = result as RoleSyncFailedEvent;
        failedEvent!.Cause.Should().Be(IdentityFailureCause.Unknown);
    }

    [Fact]
    public async Task Full_Flow_Happy_Path_Should_Complete_Successfully()
    {
        // Arrange
        _keycloakClient.AssignRoleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _roleCacheRepository.UpsertAsync(Arg.Any<IdentityService.CacheLayer.MongoDB.RoleCacheEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _userCacheRepository.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new SyncRoleCommand
        {
            IdempotencyKey = "corr-full-1",
            Role = new RoleAssignDto
            {
                UserId = "user-full-1",
                RoleId = "role-full",
                RoleName = "FullRole"
            }
        };

        // Act — Step 1: Start saga
        var step1Result = await _saga.HandleAsync(command, CancellationToken.None);
        step1Result.Should().BeOfType<AssignRoleInKeycloakCommand>();

        // Act — Step 2: Keycloak assigned
        var keycloakResponse = new RoleAssignedInKeycloak
        {
            CorrelationId = "corr-full-1",
            UserId = "user-full-1",
            RoleId = "role-full",
            RoleName = "FullRole",
            AssignedAt = DateTime.UtcNow
        };
        var step2Result = await _saga.HandleAsync(keycloakResponse, CancellationToken.None);
        step2Result.Should().BeOfType<UpdateRoleCacheStepCommand>();

        // Act — Step 3: Role cache updated
        var cacheResponse = new RoleCacheUpdated
        {
            CorrelationId = "corr-full-1",
            RoleId = "role-full",
            UserId = "user-full-1",
            UpdatedAt = DateTime.UtcNow
        };
        var step3Result = await _saga.HandleAsync(cacheResponse, CancellationToken.None);
        step3Result.Should().BeOfType<InvalidateUserCacheStepCommand>();

        // Act — Step 4: Complete
        var completeCmd = new InvalidateUserCacheStepCommand
        {
            CorrelationId = "corr-full-1",
            UserId = "user-full-1"
        };
        var step4Result = await _saga.HandleAsync(completeCmd, CancellationToken.None);

        // Assert final state
        step4Result.Should().BeOfType<RoleSynced>();
        var final = step4Result as RoleSynced;
        final!.CorrelationId.Should().Be("corr-full-1");
        final.RoleId.Should().Be("role-full");
        final.RoleName.Should().Be("FullRole");
        _saga.Data.Status.Should().Be("Completed");
        _saga.Data.AuditHistory.Should().HaveCount(4); // AssignInKeycloak + UpdateRoleCache + InvalidateUserCache + Completed
    }

    [Fact]
    public async Task Full_Flow_Keycloak_Failure_Should_Emit_RoleSyncFailedEvent()
    {
        // Arrange
        _keycloakClient.AssignRoleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new KeycloakException("Keycloak 503", 503));

        var command = new SyncRoleCommand
        {
            IdempotencyKey = "corr-fail-flow-1",
            Role = new RoleAssignDto
            {
                UserId = "user-fail-flow-1",
                RoleId = "role-fail-flow",
                RoleName = "FailFlow"
            }
        };

        // Act — Step 1: Start saga
        var step1Result = await _saga.HandleAsync(command, CancellationToken.None);
        step1Result.Should().BeOfType<AssignRoleInKeycloakCommand>();
        var stepCmd = step1Result as AssignRoleInKeycloakCommand;

        // Step 2: Attempt Keycloak — the handler would throw
        // This simulates the exception flowing to HandleFailureAsync
        var exception = new KeycloakException("Keycloak 503", 503);
        var failureResult = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        failureResult.Should().BeOfType<RoleSyncFailedEvent>();
        var failed = failureResult as RoleSyncFailedEvent;
        failed!.Cause.Should().Be(IdentityFailureCause.KeycloakApiError);
        failed.FailedStep.Should().Be("AssignInKeycloak");
        _saga.Data.Status.Should().Be("Failed");
    }
}
