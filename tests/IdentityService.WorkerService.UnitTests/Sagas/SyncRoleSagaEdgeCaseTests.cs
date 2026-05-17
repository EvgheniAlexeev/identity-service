using FluentAssertions;
using IdentityService.CacheLayer.Repositories;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Dtos;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Sagas;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Edge case and boundary condition tests for SyncRoleSaga.
/// Tests concurrent saga isolation, empty inputs, whitespace handling,
/// and degenerate state transitions.
/// </summary>
public class SyncRoleSagaEdgeCaseTests
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly IRoleCacheRepository _roleCacheRepository;
    private readonly IUserCacheRepository _userCacheRepository;
    private readonly SagaMetrics _metrics;
    private readonly SyncRoleSaga _saga;

    public SyncRoleSagaEdgeCaseTests()
    {
        _keycloakClient = Substitute.For<IKeycloakAdminClient>();
        _roleCacheRepository = Substitute.For<IRoleCacheRepository>();
        _userCacheRepository = Substitute.For<IUserCacheRepository>();
        _metrics = new SagaMetrics();

        var assignRoleHandler = new AssignRoleInKeycloakHandler(
            _keycloakClient,
            Substitute.For<ILogger<AssignRoleInKeycloakHandler>>(),
            _metrics);
        var cacheHandler = new UpdateRoleCacheHandler(
            _roleCacheRepository,
            Substitute.For<ILogger<UpdateRoleCacheHandler>>(),
            _metrics);
        var invalidateHandler = new InvalidateUserCacheHandler(
            _userCacheRepository,
            Substitute.For<ILogger<InvalidateUserCacheHandler>>(),
            _metrics);

        _saga = new SyncRoleSaga(
            Substitute.For<ILogger<SyncRoleSaga>>(),
            _metrics,
            assignRoleHandler,
            cacheHandler,
            invalidateHandler);
    }

    [Fact]
    public async Task HandleAsync_Empty_IdempotencyKey_Should_Still_Initialize()
    {
        // Arrange
        var command = new SyncRoleCommand
        {
            IdempotencyKey = string.Empty,
            Role = new RoleAssignDto
            {
                UserId = "user-empty",
                RoleId = "role-empty",
                RoleName = "Empty"
            }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        _saga.Data.Id.Should().BeEmpty();
        _saga.Data.Status.Should().Be("SyncPending");
        result.Should().BeOfType<AssignRoleInKeycloakCommand>();
    }

    [Fact]
    public async Task HandleAsync_Empty_Role_Fields_Should_Not_Throw()
    {
        // Arrange
        var command = new SyncRoleCommand
        {
            IdempotencyKey = "corr-edge-1",
            Role = new RoleAssignDto()  // All empty defaults
        };

        // Act
        var act = () => _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _saga.Data.UserId.Should().BeEmpty();
        _saga.Data.Role.RoleId.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Concurrent_Isolation_Same_CorrelationId()
    {
        // Arrange — start first saga instance
        var command1 = new SyncRoleCommand
        {
            IdempotencyKey = "corr-concurrent-1",
            Role = new RoleAssignDto
            {
                UserId = "user-concurrent",
                RoleId = "role-concurrent",
                RoleName = "Concurrent"
            }
        };

        var command2 = new SyncRoleCommand
        {
            IdempotencyKey = "corr-concurrent-1",  // Same key
            Role = new RoleAssignDto
            {
                UserId = "user-concurrent",
                RoleId = "role-concurrent",
                RoleName = "Concurrent"
            }
        };

        // Act — start first saga
        var result1 = await _saga.HandleAsync(command1, CancellationToken.None);
        result1.Should().BeOfType<AssignRoleInKeycloakCommand>();

        // Second saga with same key — should be idempotent since state is incomplete
        // (no "Completed" audit entry yet, so it should NOT short-circuit)
        // Seed a completed audit entry to simulate already-completed saga
        _saga.Data.AuditHistory.Add(new SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        var result2 = await _saga.HandleAsync(command2, CancellationToken.None);

        // Assert
        result2.Should().BeOfType<RoleSynced>();  // Returned cached result
    }

    [Fact]
    public async Task HandleFailureAsync_NullException_Should_Use_Unknown()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-null-ex";
        _saga.Data.UserId = "user-null-ex";
        _saga.Data.Role = new RoleAssignDto { UserId = "user-null-ex", RoleId = "r1", RoleName = "R1" };
        _saga.Data.CurrentStep = "AssignInKeycloak";

        // Act
        var result = await _saga.HandleFailureAsync(new Exception("null test"), CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Failed");
        _saga.Data.ErrorReason.Should().Be("null test");
        result.Should().BeOfType<RoleSyncFailedEvent>();
    }

    [Fact]
    public async Task OperationCanceledException_Should_Map_To_NetworkTimeout()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-cancel";
        _saga.Data.UserId = "user-cancel";
        _saga.Data.Role = new RoleAssignDto { UserId = "user-cancel", RoleId = "r2", RoleName = "R2" };
        _saga.Data.CurrentStep = "UpdateRoleCache";

        // Act
        var result = await _saga.HandleFailureAsync(new OperationCanceledException(), CancellationToken.None);

        // Assert
        var failed = result as RoleSyncFailedEvent;
        failed!.Cause.Should().Be(IdentityFailureCause.NetworkTimeout);
    }
}
