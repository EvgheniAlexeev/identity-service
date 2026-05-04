using FluentAssertions;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.CacheLayer.UnitTests;

/// <summary>
/// Unit tests for UserCacheRepository using mocked MongoDB.
/// </summary>
public class UserCacheRepositoryTests
{
    private readonly ILogger<UserCacheRepository> _logger;
    private readonly IMongoCollection<UserCacheEntry> _mockCollection;
    private readonly CacheContext _mockContext;
    private readonly UserCacheRepository _repository;

    public UserCacheRepositoryTests()
    {
        _logger = Substitute.For<ILogger<UserCacheRepository>>();
        _mockCollection = Substitute.For<IMongoCollection<UserCacheEntry>>();

        var mockDatabase = Substitute.For<IMongoDatabase>();
        mockDatabase.GetCollection<UserCacheEntry>("user_cache", null)
            .ReturnsForAnyArgs(_mockCollection);

        _mockContext = new CacheContext(mockDatabase);
        _repository = new UserCacheRepository(_mockContext, _logger);
    }

    [Fact]
    public async Task GetByUserId_Should_Return_Entry_When_Found()
    {
        var entry = new UserCacheEntry
        {
            UserId = "user-1",
            Email = "test@example.com",
            Roles = new List<string> { "admin" }
        };

        // Mock FindAsync to return our entry
        var mockCursor = Substitute.For<IAsyncCursor<UserCacheEntry>>();
        mockCursor.Current.Returns(new[] { entry });
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<FindOptions<UserCacheEntry, UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var result = await _repository.GetByUserIdAsync("user-1");

        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByUserId_Should_Return_Null_When_Not_Found()
    {
        var mockCursor = Substitute.For<IAsyncCursor<UserCacheEntry>>();
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<FindOptions<UserCacheEntry, UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var result = await _repository.GetByUserIdAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Upsert_Should_Call_ReplaceOne_With_Upsert()
    {
        var entry = new UserCacheEntry
        {
            UserId = "user-1",
            Email = "test@example.com",
            Roles = new List<string> { "admin" },
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Is<UserCacheEntry>(e => e.UserId == "user-1"),
            Arg.Is<ReplaceOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));

        await _repository.UpsertAsync(entry);

        await _mockCollection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<UserCacheEntry>(),
            Arg.Is<ReplaceOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Should_Call_DeleteOne()
    {
        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));

        await _repository.DeleteAsync("user-1");

        await _mockCollection.Received(1).DeleteOneAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByUserId_When_MongoDb_Throws_Should_Propagate_Exception()
    {
        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<FindOptions<UserCacheEntry, UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Connection refused"));

        var act = () => _repository.GetByUserIdAsync("user-1");
        await act.Should().ThrowAsync<MongoException>()
            .WithMessage("Connection refused");
    }

    [Fact]
    public async Task GetAllActive_Should_Return_Non_Expired_Entries()
    {
        var activeEntry = new UserCacheEntry
        {
            UserId = "active-user",
            Email = "active@example.com",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var mockCursor = Substitute.For<IAsyncCursor<UserCacheEntry>>();
        mockCursor.Current.Returns(new[] { activeEntry });
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<FindOptions<UserCacheEntry, UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var results = await _repository.GetAllActiveAsync();

        results.Should().HaveCount(1);
        results[0].UserId.Should().Be("active-user");
    }

    [Fact]
    public async Task GetAllActive_Should_Return_Empty_When_None_Active()
    {
        var mockCursor = Substitute.For<IAsyncCursor<UserCacheEntry>>();
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<UserCacheEntry>>(),
            Arg.Any<FindOptions<UserCacheEntry, UserCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var results = await _repository.GetAllActiveAsync();
        results.Should().BeEmpty();
    }
}
