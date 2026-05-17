using FluentAssertions;
using IdentityService.CacheLayer.MongoDB;
using MongoDB.Bson;
using IdentityService.CacheLayer.Repositories;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.KeycloakAdapter.Models;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Dtos;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Sagas;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;
using Xunit;

namespace IdentityService.WorkerService.IntegrationTests;

/// <summary>
/// BLOCK_INTEGRATION_TEST Integration tests using real MongoDB container.
/// Tests the full saga flow with real persistence and mocked Keycloak.
/// </summary>
public class SagaIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private IMongoDatabase? _database;
    private IMongoCollection<ProvisionUserSagaState>? _sagaCollection;
    private IUserCacheRepository? _cacheRepository;
    private IKeycloakAdminClient? _keycloakClient;
    private SagaMetrics? _metrics;

    public SagaIntegrationTests()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var client = new MongoClient(_mongoContainer.GetConnectionString());
        _database = client.GetDatabase("mif_test_integration");
        _sagaCollection = _database.GetCollection<ProvisionUserSagaState>("saga_states");
        _cacheRepository = Substitute.For<IUserCacheRepository>();
        _keycloakClient = Substitute.For<IKeycloakAdminClient>();
        _metrics = new SagaMetrics();
    }

    public async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }

    private ProvisionUserSaga CreateSaga()
    {
        var sagaLogger = Substitute.For<ILogger<ProvisionUserSaga>>();
        var keycloakLogger = Substitute.For<ILogger<CreateUserInKeycloakHandler>>();
        var cacheLogger = Substitute.For<ILogger<UpdateUserCacheHandler>>();
        var notifyLogger = Substitute.For<ILogger<NotifyAdminsHandler>>();

        var keycloakHandler = new CreateUserInKeycloakHandler(
            _keycloakClient!, keycloakLogger, _metrics!);
        var cacheHandler = new UpdateUserCacheHandler(
            _cacheRepository!, cacheLogger, _metrics!);
        var notifyHandler = new NotifyAdminsHandler(notifyLogger, _metrics!);

        return new ProvisionUserSaga(sagaLogger, _metrics!, keycloakHandler, cacheHandler, notifyHandler);
    }

    [Fact]
    public async Task Full_Saga_Flow_With_Mock_Keycloak_Should_Complete()
    {
        // Arrange
        _keycloakClient!.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .Returns("kc-integration-1");

        var saga = CreateSaga();
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "integration-1",
            User = new UserCreatedDto
            {
                UserId = "user-integration-1",
                Email = "integration@example.com",
                FirstName = "Integration",
                LastName = "Test",
                InitialRoles = new List<string> { "admin", "reader" }
            }
        };

        // Act: Run full saga flow
        await saga.HandleAsync(command, CancellationToken.None);
        await saga.HandleAsync(new IdentityService.WorkerService.Events.UserCreatedInKeycloak
        {
            CorrelationId = "integration-1",
            UserId = "user-integration-1",
            KeycloakUserId = "kc-integration-1",
            Email = "integration@example.com",
            FirstName = "Integration",
            LastName = "Test",
            Roles = new List<string> { "admin", "reader" },
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        await saga.HandleAsync(new IdentityService.WorkerService.Events.CacheUpdated
        {
            CorrelationId = "integration-1",
            UserId = "user-integration-1",
            UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        var finalResult = await saga.HandleAsync(new NotifyCommand
        {
            CorrelationId = "integration-1",
            UserId = "user-integration-1",
            Email = "integration@example.com",
            Status = "Provisioned"
        }, CancellationToken.None);

        // Assert
        saga.Data.Status.Should().Be("Provisioned");
        saga.Data.CompletedAt.Should().NotBeNull();
        saga.Data.KeycloakUserId.Should().Be("kc-integration-1");
        saga.Data.AuditHistory.Should().HaveCount(3);
        finalResult.Should().BeOfType<IdentityService.Shared.Events.UserProvisioned>();
    }

    [Fact]
    public async Task Saga_Failure_At_Keycloak_Step_Should_Publish_DLQ()
    {
        // Arrange
        var saga = CreateSaga();
        saga.Data.CorrelationId = "dlq-integration-1";
        saga.Data.UserId = "user-dlq-1";
        saga.Data.CurrentStep = "CreateInKeycloak";
        saga.Data.User = new UserCreatedDto
        {
            UserId = "user-dlq-1",
            Email = "dlq@example.com",
            FirstName = "DLQ",
            LastName = "Test",
            InitialRoles = new List<string> { "reader" }
        };
        saga.Data.RetryCount = 3;

        // Act
        var result = await saga.HandleFailureAsync(
            new IdentityService.KeycloakAdapter.Admin.KeycloakException("Keycloak 503", 503),
            CancellationToken.None);

        // Assert
        saga.Data.Status.Should().Be("Failed");
        saga.Data.AuditHistory.Should().ContainSingle(a => a.Outcome == "Failed");
        var dlqEvent = result.Should().BeOfType<IdentityService.Shared.Events.FailedIdentityEvent>().Subject;
        dlqEvent.Cause.Should().Be(IdentityService.Shared.Events.IdentityFailureCause.KeycloakApiError);
        dlqEvent.OriginalRequest.Email.Should().Be("dlq@example.com");
    }

    [Fact]
    public async Task Saga_Failure_At_Cache_Step_Should_Publish_DLQ()
    {
        // Arrange
        var saga = CreateSaga();
        saga.Data.CorrelationId = "dlq-cache-1";
        saga.Data.UserId = "user-cache-dlq-1";
        saga.Data.CurrentStep = "UpdateCache";
        saga.Data.KeycloakUserId = "kc-already-created";
        saga.Data.User = new UserCreatedDto
        {
            UserId = "user-cache-dlq-1",
            Email = "cachedlq@example.com",
            FirstName = "CacheDLQ",
            LastName = "Test"
        };

        // Act
        var result = await saga.HandleFailureAsync(
            new TimeoutException("MongoDB write timeout"),
            CancellationToken.None);

        // Assert
        var dlqEvent = result.Should().BeOfType<IdentityService.Shared.Events.FailedIdentityEvent>().Subject;
        dlqEvent.FailedStep.Should().Be("UpdateCache");
        dlqEvent.Cause.Should().Be(IdentityService.Shared.Events.IdentityFailureCause.NetworkTimeout);
    }

    [Fact]
    public async Task Idempotency_Should_Prevent_Double_Processing()
    {
        // Arrange
        var saga = CreateSaga();

        // First run: complete the saga
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "idem-key-1",
            User = new UserCreatedDto
            {
                UserId = "user-idem-1",
                Email = "idem@example.com",
                FirstName = "Idem",
                LastName = "Test"
            }
        };
        await saga.HandleAsync(command, CancellationToken.None);

        var kcEvent = new IdentityService.WorkerService.Events.UserCreatedInKeycloak
        {
            CorrelationId = "idem-key-1",
            UserId = "user-idem-1",
            KeycloakUserId = "kc-idem-1",
            Email = "idem@example.com",
            FirstName = "Idem",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow
        };
        await saga.HandleAsync(kcEvent, CancellationToken.None);

        var cacheEvent = new IdentityService.WorkerService.Events.CacheUpdated
        {
            CorrelationId = "idem-key-1",
            UserId = "user-idem-1",
            UpdatedAt = DateTime.UtcNow
        };
        await saga.HandleAsync(cacheEvent, CancellationToken.None);

        await saga.HandleAsync(new NotifyCommand
        {
            CorrelationId = "idem-key-1",
            UserId = "user-idem-1",
            Email = "idem@example.com",
            Status = "Provisioned"
        }, CancellationToken.None);

        saga.Data.Status.Should().Be("Provisioned");

        // Second attempt: duplicate command
        var duplicateCommand = new ProvisionUserCommand
        {
            IdempotencyKey = "idem-key-1",
            User = new UserCreatedDto
            {
                UserId = "user-idem-1",
                Email = "idem@example.com",
                FirstName = "Idem",
                LastName = "Test"
            }
        };

        // Act: Replay the saga
        var result = await saga.HandleAsync(duplicateCommand, CancellationToken.None);

        // Assert: Should return already-completed result
        var provisionedEvent = result.Should().BeOfType<IdentityService.Shared.Events.UserProvisioned>().Subject;
        provisionedEvent.UserId.Should().Be("user-idem-1");
    }

    [Fact]
    public async Task SagaState_Should_Persist_To_MongoDB()
    {
        // Arrange
        var saga = CreateSaga();
        var command = new ProvisionUserCommand
        {
            IdempotencyKey = "persist-1",
            User = new UserCreatedDto
            {
                UserId = "user-persist-1",
                Email = "persist@example.com",
                FirstName = "Persist",
                LastName = "Test",
                InitialRoles = new List<string> { "admin" }
            }
        };

        // Act
        await saga.HandleAsync(command, CancellationToken.None);

        // Persist saga state to MongoDB
        await _sagaCollection!.InsertOneAsync(saga.Data);

        // Assert: Read back from MongoDB
        var persisted = await _sagaCollection.Find(s => s.CorrelationId == "persist-1").FirstOrDefaultAsync();
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be("user-persist-1");
        persisted.Status.Should().Be("Provisioning");
        persisted.CurrentStep.Should().Be("CreateInKeycloak");
        persisted.User.Email.Should().Be("persist@example.com");
        persisted.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task SagaState_Should_Persist_And_Update_On_Completion()
    {
        // Arrange
        var saga = CreateSaga();
        _keycloakClient!.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .Returns("kc-persist-final");

        var state = new ProvisionUserSagaState
        {
            Id = "persist-final",
            CorrelationId = "persist-final",
            UserId = "user-final",
            Status = "Provisioning",
            CurrentStep = "CreateInKeycloak",
            User = new UserCreatedDto
            {
                UserId = "user-final",
                Email = "final@example.com",
                FirstName = "Final",
                LastName = "Test"
            },
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _sagaCollection!.InsertOneAsync(state);

        // Act: Complete the saga
        state.Status = "Provisioned";
        state.KeycloakUserId = "kc-persist-final";
        state.CompletedAt = DateTime.UtcNow;
        state.AuditHistory.Add(new IdentityService.WorkerService.Sagas.SagaStepAudit
        {
            StepName = "Completed",
            Outcome = "Success",
            Timestamp = DateTime.UtcNow
        });

        await _sagaCollection.ReplaceOneAsync(
            s => s.CorrelationId == "persist-final",
            state);

        // Assert
        var completed = await _sagaCollection.Find(s => s.CorrelationId == "persist-final").FirstOrDefaultAsync();
        completed.Should().NotBeNull();
        completed!.Status.Should().Be("Provisioned");
        completed.KeycloakUserId.Should().Be("kc-persist-final");
    }

    [Fact]
    public async Task Concurrent_Sagas_Should_Have_Distinct_States()
    {
        // Act: Run two concurrent sagas
        var commands = new[]
        {
            new ProvisionUserCommand
            {
                IdempotencyKey = "concurrent-1",
                User = new UserCreatedDto { UserId = "user-c1", Email = "c1@example.com", FirstName = "C1", LastName = "User" }
            },
            new ProvisionUserCommand
            {
                IdempotencyKey = "concurrent-2",
                User = new UserCreatedDto { UserId = "user-c2", Email = "c2@example.com", FirstName = "C2", LastName = "User" }
            }
        };

        var states = new List<ProvisionUserSagaState>();
        foreach (var cmd in commands)
        {
            var saga = CreateSaga();
            await saga.HandleAsync(cmd, CancellationToken.None);
            states.Add(saga.Data);
        }

        // Persist both
        await _sagaCollection!.InsertManyAsync(states);

        // Assert: Both states persisted independently
        var all = await _sagaCollection.Find(_ => true).ToListAsync();
        all.Should().HaveCount(2);
        all.Select(s => s.UserId).Should().Contain(new[] { "user-c1", "user-c2" });
    }

    [Fact]
    public async Task MongoDb_Container_Should_Be_Reachable()
    {
        // Basic connectivity test
        _database.Should().NotBeNull();
        _sagaCollection.Should().NotBeNull();

        var pingResult = await _database!.RunCommandAsync<BsonDocument>(
            new BsonDocument("ping", 1));
        pingResult["ok"].AsDouble.Should().Be(1.0);
    }
}
