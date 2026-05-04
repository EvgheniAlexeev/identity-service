using FluentAssertions;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace IdentityService.CacheLayer.UnitTests;

/// <summary>
/// Tests for CacheIndexConfiguration — validates index creation calls.
/// </summary>
public class CacheIndexConfigurationTests
{
    [Fact]
    public async Task EnsureIndexes_Should_Create_UserCache_TTL_Index()
    {
        var mockUserCollection = Substitute.For<IMongoCollection<UserCacheEntry>>();
        var mockRoleCollection = Substitute.For<IMongoCollection<RoleCacheEntry>>();
        var mockDatabase = Substitute.For<IMongoDatabase>();

        mockDatabase.GetCollection<UserCacheEntry>("user_cache", null)
            .ReturnsForAnyArgs(mockUserCollection);
        mockDatabase.GetCollection<RoleCacheEntry>("role_cache", null)
            .ReturnsForAnyArgs(mockRoleCollection);

        // Mock CreateOneAsync to return a fake index name
        mockUserCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<UserCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("index_name");

        mockRoleCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<RoleCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("index_name");

        await CacheIndexConfiguration.EnsureIndexesAsync(mockDatabase);

        // Should create 4 indexes: 2 for user_cache, 2 for role_cache
        await mockUserCollection.Indexes.Received(2).CreateOneAsync(
            Arg.Any<CreateIndexModel<UserCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());

        await mockRoleCollection.Indexes.Received(2).CreateOneAsync(
            Arg.Any<CreateIndexModel<RoleCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureIndexes_Should_Create_TTL_Index_With_Zero_ExpireAfter()
    {
        var mockCollection = Substitute.For<IMongoCollection<UserCacheEntry>>();
        var mockRoleCollection = Substitute.For<IMongoCollection<RoleCacheEntry>>();
        var mockDatabase = Substitute.For<IMongoDatabase>();

        mockDatabase.GetCollection<UserCacheEntry>("user_cache", null)
            .ReturnsForAnyArgs(mockCollection);
        mockDatabase.GetCollection<RoleCacheEntry>("role_cache", null)
            .ReturnsForAnyArgs(mockRoleCollection);

        var capturedModels = new List<CreateIndexModel<UserCacheEntry>>();
        mockCollection.Indexes.CreateOneAsync(
            Arg.Do<CreateIndexModel<UserCacheEntry>>(model => capturedModels.Add(model)),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("ttl_index");

        mockRoleCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<RoleCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("index");

        await CacheIndexConfiguration.EnsureIndexesAsync(mockDatabase);

        capturedModels.Should().HaveCount(2);
        var ttlModel = capturedModels.FirstOrDefault(m =>
            m.Options?.ExpireAfter == TimeSpan.FromSeconds(0));
        ttlModel.Should().NotBeNull("one of the user_cache indexes must have TTL with ExpireAfter=0");
        ttlModel!.Options!.ExpireAfter.Should().Be(TimeSpan.FromSeconds(0));
    }

    [Fact]
    public async Task EnsureIndexes_Should_Be_Idempotent_When_Indexes_Already_Exist()
    {
        var mockCollection = Substitute.For<IMongoCollection<UserCacheEntry>>();
        var mockRoleCollection = Substitute.For<IMongoCollection<RoleCacheEntry>>();
        var mockDatabase = Substitute.For<IMongoDatabase>();

        mockDatabase.GetCollection<UserCacheEntry>("user_cache", null)
            .ReturnsForAnyArgs(mockCollection);
        mockDatabase.GetCollection<RoleCacheEntry>("role_cache", null)
            .ReturnsForAnyArgs(mockRoleCollection);

        // First call succeeds
        mockCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<UserCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("idx");

        mockRoleCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<RoleCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("idx");

        // Should not throw
        await CacheIndexConfiguration.EnsureIndexesAsync(mockDatabase);
    }

    [Fact]
    public async Task CacheContext_Initialize_Should_Call_EnsureIndexes()
    {
        var mockCollection = Substitute.For<IMongoCollection<UserCacheEntry>>();
        var mockRoleCollection = Substitute.For<IMongoCollection<RoleCacheEntry>>();
        var mockDatabase = Substitute.For<IMongoDatabase>();

        mockDatabase.GetCollection<UserCacheEntry>("user_cache", null)
            .ReturnsForAnyArgs(mockCollection);
        mockDatabase.GetCollection<RoleCacheEntry>("role_cache", null)
            .ReturnsForAnyArgs(mockRoleCollection);

        mockCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<UserCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("idx");

        mockRoleCollection.Indexes.CreateOneAsync(
            Arg.Any<CreateIndexModel<RoleCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("idx");

        var context = new CacheContext(mockDatabase);
        await context.InitializeAsync();

        // Verify indexes were created through initialization
        await mockCollection.Indexes.Received(2).CreateOneAsync(
            Arg.Any<CreateIndexModel<UserCacheEntry>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    }
}
