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
/// Unit tests for RoleCacheRepository using mocked MongoDB.
/// </summary>
public class RoleCacheRepositoryTests
{
    private readonly ILogger<RoleCacheRepository> _logger;
    private readonly IMongoCollection<RoleCacheEntry> _mockCollection;
    private readonly CacheContext _mockContext;
    private readonly RoleCacheRepository _repository;

    public RoleCacheRepositoryTests()
    {
        _logger = Substitute.For<ILogger<RoleCacheRepository>>();
        _mockCollection = Substitute.For<IMongoCollection<RoleCacheEntry>>();

        var mockDatabase = Substitute.For<IMongoDatabase>();
        mockDatabase.GetCollection<RoleCacheEntry>("role_cache", null)
            .ReturnsForAnyArgs(_mockCollection);

        _mockContext = new CacheContext(mockDatabase);
        _repository = new RoleCacheRepository(_mockContext, _logger);
    }

    [Fact]
    public async Task GetByRoleId_Should_Return_Entry_When_Found()
    {
        var entry = new RoleCacheEntry
        {
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read", "write" }
        };

        var mockCursor = Substitute.For<IAsyncCursor<RoleCacheEntry>>();
        mockCursor.Current.Returns(new[] { entry });
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<FindOptions<RoleCacheEntry, RoleCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var result = await _repository.GetByRoleIdAsync("role-admin");

        result.Should().NotBeNull();
        result!.RoleName.Should().Be("Administrator");
        result.Permissions.Should().BeEquivalentTo("read", "write");
    }

    [Fact]
    public async Task GetByRoleId_Should_Return_Null_When_Not_Found()
    {
        var mockCursor = Substitute.For<IAsyncCursor<RoleCacheEntry>>();
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<FindOptions<RoleCacheEntry, RoleCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var result = await _repository.GetByRoleIdAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Upsert_Should_Call_ReplaceOne_With_Upsert()
    {
        var entry = new RoleCacheEntry
        {
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read" }
        };

        _mockCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Is<RoleCacheEntry>(e => e.RoleId == "role-admin"),
            Arg.Is<ReplaceOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));

        await _repository.UpsertAsync(entry);

        await _mockCollection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<RoleCacheEntry>(),
            Arg.Is<ReplaceOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Should_Call_DeleteOne()
    {
        _mockCollection.DeleteOneAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));

        await _repository.DeleteAsync("role-admin");

        await _mockCollection.Received(1).DeleteOneAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllActive_Should_Return_Non_Expired_Roles()
    {
        var activeRole = new RoleCacheEntry
        {
            RoleId = "role-active",
            RoleName = "Active Role",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var mockCursor = Substitute.For<IAsyncCursor<RoleCacheEntry>>();
        mockCursor.Current.Returns(new[] { activeRole });
        mockCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);

        _mockCollection.FindAsync(
            Arg.Any<FilterDefinition<RoleCacheEntry>>(),
            Arg.Any<FindOptions<RoleCacheEntry, RoleCacheEntry>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockCursor));

        var results = await _repository.GetAllActiveAsync();
        results.Should().HaveCount(1);
        results[0].RoleId.Should().Be("role-active");
    }
}
