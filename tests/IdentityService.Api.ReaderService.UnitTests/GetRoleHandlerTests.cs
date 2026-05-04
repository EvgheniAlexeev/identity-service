using FluentAssertions;
using IdentityService.Api.ReaderService.Handlers;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.Api.ReaderService.UnitTests;

/// <summary>
/// Unit tests for GetRoleHandler with mocked IRoleCacheRepository.
/// </summary>
public class GetRoleHandlerTests
{
    private readonly IRoleCacheRepository _cache;
    private readonly ILogger<GetRoleHandler> _logger;
    private readonly GetRoleHandler _handler;

    public GetRoleHandlerTests()
    {
        _cache = Substitute.For<IRoleCacheRepository>();
        _logger = Substitute.For<ILogger<GetRoleHandler>>();
        _handler = new GetRoleHandler(_cache, _logger);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRole_WhenFound()
    {
        var entry = new RoleCacheEntry
        {
            RoleId = "role-admin", RoleName = "Administrator",
            Permissions = new List<string> { "read", "write", "delete" },
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        _cache.GetByRoleIdAsync("role-admin").Returns(entry);

        var result = await _handler.HandleAsync("role-admin");

        result.IsSuccess.Should().BeTrue();
        result.Data!.RoleId.Should().Be("role-admin");
        result.Data.RoleName.Should().Be("Administrator");
        result.Data.Permissions.Should().HaveCount(3);
        result.Data.Permissions.Should().Contain(new[] { "read", "write", "delete" });
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenNotExist()
    {
        _cache.GetByRoleIdAsync("missing").Returns((RoleCacheEntry?)null);

        var result = await _handler.HandleAsync("missing");

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRole_WithEmptyPermissions()
    {
        var entry = new RoleCacheEntry
        {
            RoleId = "empty-perms", RoleName = "EmptyRole",
            Permissions = new List<string>(),
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        _cache.GetByRoleIdAsync("empty-perms").Returns(entry);

        var result = await _handler.HandleAsync("empty-perms");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCacheThrows()
    {
        _cache.GetByRoleIdAsync("error")
            .ThrowsAsync(new MongoException("Connection refused"));

        var result = await _handler.HandleAsync("error");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal server error");
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldReturnEmpty_WhenNoRoles()
    {
        _cache.GetAllActiveAsync().Returns(new List<RoleCacheEntry>());

        var results = await _handler.HandleGetAllAsync();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldReturnAllActiveRoles()
    {
        var entries = new List<RoleCacheEntry>
        {
            new() {
                RoleId = "r1", RoleName = "Admin",
                Permissions = new List<string> { "all" },
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            },
            new() {
                RoleId = "r2", RoleName = "Viewer",
                Permissions = new List<string> { "read" },
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            },
            new() {
                RoleId = "r3", RoleName = "Editor",
                Permissions = new List<string> { "read", "write" },
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            }
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetAllAsync();

        results.Should().HaveCount(3);
        results.Select(r => r.RoleName).Should().BeEquivalentTo(new[] { "Admin", "Viewer", "Editor" });
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldMapFieldsCorrectly()
    {
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entry = new RoleCacheEntry
        {
            RoleId = "map-role", RoleName = "MappedRole",
            Permissions = new List<string> { "p1", "p2" },
            CreatedAt = createdAt, ExpiresAt = createdAt.AddDays(30)
        };
        _cache.GetAllActiveAsync().Returns(new List<RoleCacheEntry> { entry });

        var results = await _handler.HandleGetAllAsync();

        var dto = results[0];
        dto.RoleId.Should().Be("map-role");
        dto.RoleName.Should().Be("MappedRole");
        dto.Permissions.Should().BeEquivalentTo(new[] { "p1", "p2" });
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _cache.GetByRoleIdAsync("ct", cts.Token).Returns((RoleCacheEntry?)null);

        await _handler.HandleAsync("ct", cts.Token);

        await _cache.Received(1).GetByRoleIdAsync("ct", cts.Token);
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _cache.GetAllActiveAsync(cts.Token).Returns(new List<RoleCacheEntry>());

        await _handler.HandleGetAllAsync(cts.Token);

        await _cache.Received(1).GetAllActiveAsync(cts.Token);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleSpecialCharacters()
    {
        var entry = new RoleCacheEntry
        {
            RoleId = "rôle@spec!al", RoleName = "Spécial Rôle",
            Permissions = new List<string>(),
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        _cache.GetByRoleIdAsync("rôle@spec!al").Returns(entry);

        var result = await _handler.HandleAsync("rôle@spec!al");

        result.IsSuccess.Should().BeTrue();
        result.Data!.RoleId.Should().Be("rôle@spec!al");
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldPropagateException()
    {
        _cache.GetAllActiveAsync().ThrowsAsync(new MongoException("DB error"));

        await _handler.Invoking(h => h.HandleGetAllAsync())
            .Should().ThrowAsync<MongoException>();
    }
}
