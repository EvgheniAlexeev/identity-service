using FluentAssertions;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Steps;

/// <summary>
/// BLOCK_TEST Unit tests for UpdateUserCacheHandler.
/// Tests cache upsert happy path, MongoDB failure, and cache entry validation.
/// </summary>
public class UpdateUserCacheHandlerTests
{
    private readonly IUserCacheRepository _cacheRepository;
    private readonly ILogger<UpdateUserCacheHandler> _logger;
    private readonly SagaMetrics _metrics;
    private readonly UpdateUserCacheHandler _handler;

    public UpdateUserCacheHandlerTests()
    {
        _cacheRepository = Substitute.For<IUserCacheRepository>();
        _logger = Substitute.For<ILogger<UpdateUserCacheHandler>>();
        _metrics = new SagaMetrics();
        _handler = new UpdateUserCacheHandler(_cacheRepository, _logger, _metrics);
    }

    // === HAPPY PATH TESTS ===

    [Fact]
    public async Task HandleAsync_Should_Return_CacheUpdated_On_Success()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            KeycloakUserId = "kc-123",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "admin", "user" }
        };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CacheUpdated>();
        var evt = (CacheUpdated)result;
        evt.CorrelationId.Should().Be("corr-1");
        evt.UserId.Should().Be("user-1");

        await _cacheRepository.Received(1).UpsertAsync(
            Arg.Is<UserCacheEntry>(e =>
                e.UserId == "user-1" &&
                e.Email == "test@example.com" &&
                e.Roles.Contains("admin") &&
                e.Roles.Contains("user")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Set_ExpiresAt_In_Future()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-ttl",
            UserId = "user-ttl",
            KeycloakUserId = "kc-ttl",
            Email = "ttl@example.com",
            FirstName = "TTL",
            LastName = "Test"
        };

        UserCacheEntry? capturedEntry = null;
        await _cacheRepository.UpsertAsync(
            Arg.Do<UserCacheEntry>(e => capturedEntry = e),
            Arg.Any<CancellationToken>());

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_Should_Set_Id_Same_As_UserId()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-id",
            UserId = "user-id-42",
            KeycloakUserId = "kc-id-42",
            Email = "idtest@example.com",
            FirstName = "Id",
            LastName = "Test"
        };

        UserCacheEntry? capturedEntry = null;
        await _cacheRepository.UpsertAsync(
            Arg.Do<UserCacheEntry>(e => capturedEntry = e),
            Arg.Any<CancellationToken>());

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Id.Should().Be("user-id-42");
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Empty_Roles()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-empty",
            UserId = "user-empty",
            KeycloakUserId = "kc-empty",
            Email = "empty@example.com",
            FirstName = "Empty",
            LastName = "Roles"
        };

        UserCacheEntry? capturedEntry = null;
        await _cacheRepository.UpsertAsync(
            Arg.Do<UserCacheEntry>(e => capturedEntry = e),
            Arg.Any<CancellationToken>());

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Null_Roles()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-null",
            UserId = "user-null",
            KeycloakUserId = "kc-null",
            Email = "nullroles@example.com",
            FirstName = "Null",
            LastName = "Roles",
            Roles = null
        };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CacheUpdated>();
        await _cacheRepository.Received(1).UpsertAsync(
            Arg.Is<UserCacheEntry>(e => e.Roles.Count == 0),
            Arg.Any<CancellationToken>());
    }

    // === FAILURE TESTS ===

    [Fact]
    public async Task HandleAsync_Should_Throw_When_Cache_Upsert_Fails()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-fail",
            UserId = "user-fail",
            KeycloakUserId = "kc-fail",
            Email = "fail@example.com",
            FirstName = "Cache",
            LastName = "Fail"
        };

        _cacheRepository.UpsertAsync(Arg.Any<UserCacheEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("MongoDB timeout"));

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_MongoDB_Connection_Lost()
    {
        // Arrange
        var command = new UpdateUserCacheCommand
        {
            CorrelationId = "corr-mongo-fail",
            UserId = "user-mongo-fail",
            KeycloakUserId = "kc-mongo-fail",
            Email = "mongo@example.com",
            FirstName = "Mongo",
            LastName = "Fail"
        };

        _cacheRepository.UpsertAsync(Arg.Any<UserCacheEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("MongoDB connection lost"));

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
