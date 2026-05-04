using FluentAssertions;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.CacheLayer.Repositories;
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
/// BLOCK_TEST Comprehensive unit tests for ProvisionUserSaga.
/// Tests saga state transitions, DLQ handling, failure scenarios, idempotency,
/// and multi-step flow correctness.
/// </summary>
public class ProvisionUserSagaTests
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly IUserCacheRepository _cacheRepository;
    private readonly ILogger<ProvisionUserSaga> _sagaLogger;
    private readonly ILogger<CreateUserInKeycloakHandler> _keycloakStepLogger;
    private readonly ILogger<UpdateUserCacheHandler> _cacheStepLogger;
    private readonly ILogger<NotifyAdminsHandler> _notifyLogger;
    private readonly SagaMetrics _metrics;
    private readonly ProvisionUserSaga _saga;

    public ProvisionUserSagaTests()
    {
        _keycloakClient = Substitute.For<IKeycloakAdminClient>();
        _cacheRepository = Substitute.For<IUserCacheRepository>();
        _sagaLogger = Substitute.For<ILogger<ProvisionUserSaga>>();
        _keycloakStepLogger = Substitute.For<ILogger<CreateUserInKeycloakHandler>>();
        _cacheStepLogger = Substitute.For<ILogger<UpdateUserCacheHandler>>();
        _notifyLogger = Substitute.For<ILogger<NotifyAdminsHandler>>();
        _metrics = new SagaMetrics();

        var keycloakHandler = new CreateUserInKeycloakHandler(_keycloakClient, _keycloakStepLogger, _metrics);
        var cacheHandler = new UpdateUserCacheHandler(_cacheRepository, _cacheStepLogger, _metrics);
        var notifyHandler = new NotifyAdminsHandler(_notifyLogger, _metrics);

        _saga = new ProvisionUserSaga(_sagaLogger, _metrics, keycloakHandler, cacheHandler, notifyHandler);
    }

    // ======== SAGA START TESTS ========

    [Fact]
    public async Task HandleAsync_ProvisionUserCommand_Should_Initialize_Saga_State()
    {
        // Arrange
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-start-1",
            User = new UserCreatedDto
            {
                UserId = "user-start-1",
                Email = "start@example.com",
                FirstName = "Start",
                LastName = "Test",
                InitialRoles = new List<string> { "admin" }
            }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        _saga.Data.Id.Should().Be("corr-start-1");
        _saga.Data.CorrelationId.Should().Be("corr-start-1");
        _saga.Data.UserId.Should().Be("user-start-1");
        _saga.Data.Status.Should().Be("Provisioning");
        _saga.Data.CurrentStep.Should().Be("CreateInKeycloak");
        _saga.Data.RetryCount.Should().Be(0);
        _saga.Data.MaxRetries.Should().Be(3);
        _saga.Data.User.UserId.Should().Be("user-start-1");

        result.Should().BeOfType<CreateUserInKeycloakCommand>();
    }

    [Fact]
    public async Task HandleAsync_ProvisionUserCommand_Should_Emit_CreateUserCommand()
    {
        // Arrange
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-emit-1",
            User = new UserCreatedDto
            {
                UserId = "user-emit-1",
                Email = "emit@example.com",
                FirstName = "Emit",
                LastName = "Test",
                InitialRoles = new List<string> { "reader" }
            }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        var stepCmd = result.Should().BeOfType<CreateUserInKeycloakCommand>().Subject;
        stepCmd.CorrelationId.Should().Be("corr-emit-1");
        stepCmd.User.UserId.Should().Be("user-emit-1");
        stepCmd.Attempt.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Duplicate_Saga_That_Already_Completed()
    {
        // Arrange
        _saga.Data.Id = "corr-dup";
        _saga.Data.CorrelationId = "corr-dup";
        _saga.Data.UserId = "user-dup";
        _saga.Data.Status = "Provisioned";
        _saga.Data.KeycloakUserId = "kc-dup";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-dup",
            Email = "dup@example.com",
            FirstName = "Dup",
            LastName = "Test",
            InitialRoles = new List<string> { "admin" }
        };
        _saga.Data.AuditHistory = new List<SagaStepAudit>
        {
            new() { StepName = "Completed", Outcome = "Success", Timestamp = DateTime.UtcNow }
        };

        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-dup",
            User = new UserCreatedDto { UserId = "user-dup", Email = "dup@example.com" }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UserProvisioned>();
        var provisioned = (UserProvisioned)result;
        provisioned.CorrelationId.Should().Be("corr-dup");
        provisioned.UserId.Should().Be("user-dup");
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Reject_New_Saga_With_Empty_Audit()
    {
        // Arrange: Fresh saga state with no completed audit entries
        _saga.Data = new ProvisionUserSagaState();
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-fresh",
            User = new UserCreatedDto
            {
                UserId = "user-fresh",
                Email = "fresh@example.com",
                FirstName = "Fresh",
                LastName = "Start"
            }
        };

        // Act
        var result = await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreateUserInKeycloakCommand>();
    }

    // ======== KEYCLOAK RESPONSE TESTS ========

    [Fact]
    public async Task HandleAsync_UserCreatedInKeycloak_Should_Proceed_To_Cache_Step()
    {
        // Arrange
        _saga.Data.Id = "corr-kc-1";
        _saga.Data.CorrelationId = "corr-kc-1";
        _saga.Data.UserId = "user-kc-1";
        _saga.Data.Status = "Provisioning";
        _saga.Data.CurrentStep = "CreateInKeycloak";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-kc-1",
            Email = "kc@example.com",
            FirstName = "KC",
            LastName = "Test",
            InitialRoles = new List<string> { "admin", "reader" }
        };

        var kcEvent = new UserCreatedInKeycloak
        {
            CorrelationId = "corr-kc-1",
            UserId = "user-kc-1",
            KeycloakUserId = "kc-abc-123",
            Email = "kc@example.com",
            FirstName = "KC",
            LastName = "Test",
            Roles = new List<string> { "admin", "reader" },
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _saga.HandleAsync(kcEvent, CancellationToken.None);

        // Assert
        _saga.Data.KeycloakUserId.Should().Be("kc-abc-123");
        _saga.Data.CurrentStep.Should().Be("UpdateCache");
        _saga.Data.AuditHistory.Should().ContainSingle(a => a.StepName == "CreateInKeycloak" && a.Outcome == "Success");

        var cacheCmd = result.Should().BeOfType<UpdateUserCacheCommand>().Subject;
        cacheCmd.UserId.Should().Be("user-kc-1");
        cacheCmd.KeycloakUserId.Should().Be("kc-abc-123");
        cacheCmd.Roles.Should().Contain(new[] { "admin", "reader" });
    }

    [Fact]
    public async Task HandleAsync_UserCreatedInKeycloak_Should_Preserve_Roles()
    {
        // Arrange
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-roles",
            Email = "roles@example.com",
            FirstName = "Role",
            LastName = "Preserve",
            InitialRoles = new List<string> { "role-a", "role-b", "role-c" }
        };

        var kcEvent = new UserCreatedInKeycloak
        {
            CorrelationId = "corr-roles",
            UserId = "user-roles",
            KeycloakUserId = "kc-roles",
            Email = "roles@example.com",
            FirstName = "Role",
            LastName = "Preserve",
            Roles = new List<string> { "role-a", "role-b", "role-c" },
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _saga.HandleAsync(kcEvent, CancellationToken.None);

        // Assert
        var cacheCmd = result.Should().BeOfType<UpdateUserCacheCommand>().Subject;
        cacheCmd.Roles.Should().HaveCount(3);
        cacheCmd.Roles.Should().Contain("role-a");
        cacheCmd.Roles.Should().Contain("role-b");
        cacheCmd.Roles.Should().Contain("role-c");
    }

    // ======== CACHE RESPONSE TESTS ========

    [Fact]
    public async Task HandleAsync_CacheUpdated_Should_Proceed_To_Notify_Step()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-cache-1";
        _saga.Data.UserId = "user-cache-1";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-cache-1",
            Email = "cache@example.com",
            FirstName = "Cache",
            LastName = "Test"
        };
        _saga.Data.CurrentStep = "UpdateCache";
        _saga.Data.KeycloakUserId = "kc-cache-1";

        var cacheEvent = new CacheUpdated
        {
            CorrelationId = "corr-cache-1",
            UserId = "user-cache-1",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _saga.HandleAsync(cacheEvent, CancellationToken.None);

        // Assert
        _saga.Data.CurrentStep.Should().Be("Notify");
        _saga.Data.AuditHistory.Should().ContainSingle(a => a.StepName == "UpdateCache" && a.Outcome == "Success");

        var notifyCmd = result.Should().BeOfType<NotifyCommand>().Subject;
        notifyCmd.UserId.Should().Be("user-cache-1");
        notifyCmd.Status.Should().Be("Provisioned");
    }

    // ======== SAGA COMPLETION TESTS ========

    [Fact]
    public async Task HandleAsync_NotifyCommand_Should_Complete_Saga()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-complete-1";
        _saga.Data.UserId = "user-complete-1";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-complete-1",
            Email = "complete@example.com",
            FirstName = "Complete",
            LastName = "Test",
            InitialRoles = new List<string> { "admin" }
        };
        _saga.Data.KeycloakUserId = "kc-complete-1";
        _saga.Data.CurrentStep = "Notify";
        _saga.Data.Status = "Provisioning";

        var notifyCmd = new NotifyCommand
        {
            CorrelationId = "corr-complete-1",
            UserId = "user-complete-1",
            Email = "complete@example.com",
            Status = "Provisioned"
        };

        // Act
        var result = await _saga.HandleAsync(notifyCmd, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Provisioned");
        _saga.Data.CompletedAt.Should().NotBeNull();
        _saga.Data.AuditHistory.Should().ContainSingle(a => a.StepName == "Completed" && a.Outcome == "Success");

        var provisionedEvent = result.Should().BeOfType<UserProvisioned>().Subject;
        provisionedEvent.CorrelationId.Should().Be("corr-complete-1");
        provisionedEvent.UserId.Should().Be("user-complete-1");
        provisionedEvent.KeycloakUserId.Should().Be("kc-complete-1");
        provisionedEvent.Email.Should().Be("complete@example.com");
        provisionedEvent.AssignedRoles.Should().ContainSingle("admin");
    }

    [Fact]
    public async Task HandleAsync_NotifyCommand_Should_Set_ProvisionedAt()
    {
        // Arrange
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-time",
            Email = "time@example.com",
            FirstName = "Time",
            LastName = "Test"
        };
        _saga.Data.KeycloakUserId = "kc-time";

        var notifyCmd = new NotifyCommand
        {
            CorrelationId = "corr-time",
            UserId = "user-time",
            Email = "time@example.com",
            Status = "Provisioned"
        };

        var before = DateTime.UtcNow;

        // Act
        var result = await _saga.HandleAsync(notifyCmd, CancellationToken.None);

        // Assert
        var provisionedEvent = (UserProvisioned)result;
        provisionedEvent.ProvisionedAt.Should().BeAfter(before);
        provisionedEvent.ProvisionedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
    }

    // ======== FAILURE & DLQ TESTS ========

    [Fact]
    public async Task HandleFailureAsync_Should_Publish_FailedIdentityEvent_With_Full_Context()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-dlq-1";
        _saga.Data.UserId = "user-dlq-1";
        _saga.Data.CurrentStep = "CreateInKeycloak";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-dlq-1",
            Email = "dlq@example.com",
            FirstName = "DLQ",
            LastName = "Test",
            InitialRoles = new List<string> { "admin" }
        };
        _saga.Data.Status = "Provisioning";
        _saga.Data.RetryCount = 3;

        var exception = new KeycloakException("Keycloak unavailable", 503);

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.Status.Should().Be("Failed");
        _saga.Data.ErrorReason.Should().Be("Keycloak unavailable");

        var dlqEvent = result.Should().BeOfType<FailedIdentityEvent>().Subject;
        dlqEvent.CorrelationId.Should().Be("corr-dlq-1");
        dlqEvent.UserId.Should().Be("user-dlq-1");
        dlqEvent.FailedStep.Should().Be("CreateInKeycloak");
        dlqEvent.ErrorMessage.Should().Be("Keycloak unavailable");
        dlqEvent.Cause.Should().Be(IdentityFailureCause.KeycloakApiError);
        dlqEvent.RetryCount.Should().Be(3);
        dlqEvent.OriginalRequest.UserId.Should().Be("user-dlq-1");
        dlqEvent.OriginalRequest.Email.Should().Be("dlq@example.com");
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Classify_Network_Timeout()
    {
        // Arrange
        _saga.Data.CurrentStep = "CreateInKeycloak";
        var exception = new TimeoutException("Connection timed out after 30s");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.Cause.Should().Be(IdentityFailureCause.NetworkTimeout);
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Classify_Operation_Cancelled()
    {
        // Arrange
        _saga.Data.CurrentStep = "UpdateCache";
        var exception = new OperationCanceledException("Cancelled by timeout");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.Cause.Should().Be(IdentityFailureCause.NetworkTimeout);
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Classify_Unknown_Exception()
    {
        // Arrange
        _saga.Data.CurrentStep = "UpdateCache";
        var exception = new InvalidOperationException("Some unexpected error");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.Cause.Should().Be(IdentityFailureCause.Unknown);
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Record_Failure_Audit()
    {
        // Arrange
        _saga.Data.CurrentStep = "UpdateCache";
        var exception = new InvalidOperationException("Cache failure");

        // Act
        await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.AuditHistory.Should().ContainSingle(a =>
            a.StepName == "UpdateCache" &&
            a.Outcome == "Failed" &&
            a.ErrorMessage == "Cache failure");
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Set_CompletedAt_Timestamp()
    {
        // Arrange
        _saga.Data.CurrentStep = "CreateInKeycloak";
        var exception = new Exception("test error");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        _saga.Data.CompletedAt.Should().NotBeNull();
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task HandleFailureAsync_At_Cache_Step_Should_Preserve_Original_Request()
    {
        // Arrange
        _saga.Data.CurrentStep = "UpdateCache";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-cache-fail",
            Email = "cache-fail@example.com",
            FirstName = "CacheFail",
            LastName = "Test",
            InitialRoles = new List<string> { "reader", "writer" }
        };
        _saga.Data.KeycloakUserId = "already-created-kc-id";

        var exception = new InvalidOperationException("MongoDB write failure");

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.FailedStep.Should().Be("UpdateCache");
        dlqEvent.OriginalRequest.UserId.Should().Be("user-cache-fail");
        dlqEvent.OriginalRequest.InitialRoles.Should().Contain(new[] { "reader", "writer" });
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Classify_Keycloak_Api_Error_As_Retryable()
    {
        // Arrange
        _saga.Data.CurrentStep = "CreateInKeycloak";
        var exception = new KeycloakException("Internal server error", 500);

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.Cause.Should().Be(IdentityFailureCause.KeycloakApiError);
    }

    [Fact]
    public async Task HandleFailureAsync_Should_Classify_Keycloak_404_Not_Found()
    {
        // Arrange
        _saga.Data.CurrentStep = "CreateInKeycloak";
        var exception = new KeycloakException("User not found", 404);

        // Act
        var result = await _saga.HandleFailureAsync(exception, CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.Cause.Should().Be(IdentityFailureCause.UserAlreadyExists);
    }

    // ======== FULL FLOW TESTS ========

    [Fact]
    public async Task Full_Saga_Flow_Should_Transition_Through_All_Steps()
    {
        // Step 1: Start saga
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-full-1",
            User = new UserCreatedDto
            {
                UserId = "user-full-1",
                Email = "full@example.com",
                FirstName = "Full",
                LastName = "Flow",
                InitialRoles = new List<string> { "admin" }
            }
        };
        var step1Result = await _saga.HandleAsync(command, CancellationToken.None);
        step1Result.Should().BeOfType<CreateUserInKeycloakCommand>();
        _saga.Data.CurrentStep.Should().Be("CreateInKeycloak");

        // Step 2: Keycloak response
        var kcEvent = new UserCreatedInKeycloak
        {
            CorrelationId = "corr-full-1",
            UserId = "user-full-1",
            KeycloakUserId = "kc-full-1",
            Email = "full@example.com",
            FirstName = "Full",
            LastName = "Flow",
            Roles = new List<string> { "admin" },
            CreatedAt = DateTime.UtcNow
        };
        var step2Result = await _saga.HandleAsync(kcEvent, CancellationToken.None);
        step2Result.Should().BeOfType<UpdateUserCacheCommand>();
        _saga.Data.CurrentStep.Should().Be("UpdateCache");

        // Step 3: Cache response
        var cacheEvent = new CacheUpdated
        {
            CorrelationId = "corr-full-1",
            UserId = "user-full-1",
            UpdatedAt = DateTime.UtcNow
        };
        var step3Result = await _saga.HandleAsync(cacheEvent, CancellationToken.None);
        step3Result.Should().BeOfType<NotifyCommand>();
        _saga.Data.CurrentStep.Should().Be("Notify");

        // Step 4: Completion
        var step4Result = await _saga.HandleAsync((NotifyCommand)step3Result, CancellationToken.None);
        step4Result.Should().BeOfType<UserProvisioned>();
        _saga.Data.Status.Should().Be("Provisioned");
        _saga.Data.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Full_Saga_Flow_With_Failure_At_Step2_Should_Publish_DLQ()
    {
        // Step 1: Start saga
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-fail-s2",
            User = new UserCreatedDto
            {
                UserId = "user-fail-s2",
                Email = "fail2@example.com",
                FirstName = "Fail",
                LastName = "Step2"
            }
        };
        await _saga.HandleAsync(command, CancellationToken.None);

        // Step 2: Simulate failure instead of processing Keycloak response
        _saga.Data.CurrentStep = "CreateInKeycloak";
        var failureResult = await _saga.HandleFailureAsync(
            new KeycloakException("504 Gateway Timeout", 504), CancellationToken.None);

        // Assert
        var dlqEvent = (FailedIdentityEvent)failureResult;
        dlqEvent.FailedStep.Should().Be("CreateInKeycloak");
        dlqEvent.Cause.Should().Be(IdentityFailureCause.KeycloakApiError);
        _saga.Data.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Full_Saga_Flow_With_Failure_After_Keycloak_Should_Preserve_KC_User_Id()
    {
        // Arrange
        _saga.Data.CorrelationId = "corr-fail-cache";
        _saga.Data.UserId = "user-fail-cache";
        _saga.Data.KeycloakUserId = "kc-already-created";
        _saga.Data.User = new UserCreatedDto
        {
            UserId = "user-fail-cache",
            Email = "failcache@example.com",
            FirstName = "FailCache",
            LastName = "Test"
        };
        _saga.Data.CurrentStep = "UpdateCache";

        // Act
        var result = await _saga.HandleFailureAsync(
            new InvalidOperationException("Cache failed"), CancellationToken.None);

        // Assert
        _saga.Data.KeycloakUserId.Should().Be("kc-already-created");
        var dlqEvent = (FailedIdentityEvent)result;
        dlqEvent.FailedStep.Should().Be("UpdateCache");
    }

    // ======== AUDIT HISTORY TESTS ========

    [Fact]
    public async Task Audit_History_Should_Record_All_Steps_In_Full_Flow()
    {
        // Arrange & Act: Run full saga flow
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-audit",
            User = new UserCreatedDto
            {
                UserId = "user-audit",
                Email = "audit@example.com",
                FirstName = "Audit",
                LastName = "Test",
                InitialRoles = new List<string> { "admin" }
            }
        };
        await _saga.HandleAsync(command, CancellationToken.None);
        await _saga.HandleAsync(new UserCreatedInKeycloak
        {
            CorrelationId = "corr-audit", UserId = "user-audit",
            KeycloakUserId = "kc-audit", Email = "audit@example.com",
            FirstName = "Audit", LastName = "Test",
            Roles = new List<string> { "admin" }, CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        await _saga.HandleAsync(new CacheUpdated
        {
            CorrelationId = "corr-audit", UserId = "user-audit",
            UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        await _saga.HandleAsync(new NotifyCommand
        {
            CorrelationId = "corr-audit", UserId = "user-audit",
            Email = "audit@example.com", Status = "Provisioned"
        }, CancellationToken.None);

        // Assert
        _saga.Data.AuditHistory.Should().HaveCount(3);
        _saga.Data.AuditHistory[0].StepName.Should().Be("CreateInKeycloak");
        _saga.Data.AuditHistory[0].Outcome.Should().Be("Success");
        _saga.Data.AuditHistory[1].StepName.Should().Be("UpdateCache");
        _saga.Data.AuditHistory[1].Outcome.Should().Be("Success");
        _saga.Data.AuditHistory[2].StepName.Should().Be("Completed");
        _saga.Data.AuditHistory[2].Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task Audit_History_Should_Include_Failure_Entries()
    {
        // Arrange
        _saga.Data.CurrentStep = "CreateInKeycloak";
        _saga.Data.CorrelationId = "corr-audit-fail";
        _saga.Data.UserId = "user-audit-fail";

        // Act
        await _saga.HandleFailureAsync(new TimeoutException("timeout"), CancellationToken.None);

        // Assert
        _saga.Data.AuditHistory.Should().ContainSingle(a =>
            a.StepName == "CreateInKeycloak" &&
            a.Outcome == "Failed" &&
            a.ErrorMessage == "timeout");
    }

    // ======== TTL TESTS ========

    [Fact]
    public async Task Saga_State_Should_Set_ExpiresAt_To_7_Days()
    {
        // Arrange
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "corr-ttl",
            User = new UserCreatedDto
            {
                UserId = "user-ttl",
                Email = "ttl@example.com",
                FirstName = "TTL",
                LastName = "Test"
            }
        };

        // Act
        await _saga.HandleAsync(command, CancellationToken.None);

        // Assert
        _saga.Data.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }
}
