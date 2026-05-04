using FluentAssertions;
using IdentityService.Api.ReaderService.Handlers;
using IdentityService.Api.ReaderService.Models;
using IdentityService.Api.ReaderService.Validators;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.Api.ReaderService.UnitTests;

/// <summary>
/// Comprehensive unit tests for GetUserHandler with mocked IUserCacheRepository.
/// </summary>
public class GetUserHandlerTests
{
    private readonly IUserCacheRepository _cache;
    private readonly ILogger<GetUserHandler> _logger;
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        _cache = Substitute.For<IUserCacheRepository>();
        _logger = Substitute.For<ILogger<GetUserHandler>>();
        _handler = new GetUserHandler(_cache, _logger);
    }

    // =========== GetUserByUserId ===========

    [Fact]
    public async Task HandleAsync_ShouldReturnUser_WhenFound()
    {
        var entry = CreateEntry("user-1", "user1@test.com", new List<string> { "admin" });
        _cache.GetByUserIdAsync("user-1").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "user-1" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("user-1");
        result.Data.Email.Should().Be("user1@test.com");
        result.Data.Roles.Should().Contain("admin");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUser_WithMultipleRoles()
    {
        var entry = CreateEntry("user-multi", "multi@test.com", new List<string> { "admin", "viewer", "editor" });
        _cache.GetByUserIdAsync("user-multi").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "user-multi" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Roles.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenNotExist()
    {
        _cache.GetByUserIdAsync("missing").Returns((UserCacheEntry?)null);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "missing" });

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_ForUnknownUser()
    {
        _cache.GetByUserIdAsync("unknown").Returns((UserCacheEntry?)null);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "unknown" });

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUser_WithEmptyRoles()
    {
        var entry = CreateEntry("no-roles", "noroles@test.com", new List<string>());
        _cache.GetByUserIdAsync("no-roles").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "no-roles" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCacheThrows()
    {
        _cache.GetByUserIdAsync("error-user")
            .ThrowsAsync(new MongoException("Connection failed"));

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "error-user" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal server error");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_ForValidEmail()
    {
        var entry = CreateEntry("email-user", "test@example.com", new List<string>());
        _cache.GetByUserIdAsync("email-user").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "email-user" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Email.Should().Be("test@example.com");
    }

    // =========== GetAllUsers ===========

    [Fact]
    public async Task HandleGetAllAsync_ShouldReturnEmpty_WhenNoUsers()
    {
        _cache.GetAllActiveAsync().Returns(new List<UserCacheEntry>());

        var results = await _handler.HandleGetAllAsync();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldReturnAllActiveUsers()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("u1", "u1@test.com"),
            CreateEntry("u2", "u2@test.com"),
            CreateEntry("u3", "u3@test.com")
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetAllAsync();

        results.Should().HaveCount(3);
        results.Select(u => u.UserId).Should().BeEquivalentTo(new[] { "u1", "u2", "u3" });
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldMapAllFields()
    {
        var entry = CreateEntry("map-user", "map@test.com", new List<string> { "r1", "r2" });
        _cache.GetAllActiveAsync().Returns(new List<UserCacheEntry> { entry });

        var results = await _handler.HandleGetAllAsync();

        var dto = results[0];
        dto.UserId.Should().Be("map-user");
        dto.Email.Should().Be("map@test.com");
        dto.Status.Should().Be("Provisioned");
        dto.Roles.Should().BeEquivalentTo(new[] { "r1", "r2" });
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldHandleLargeList()
    {
        var entries = Enumerable.Range(1, 100)
            .Select(i => CreateEntry($"u{i}", $"u{i}@test.com"))
            .ToList();
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetAllAsync();

        results.Should().HaveCount(100);
    }

    // =========== GetUsersByStatus ===========

    [Fact]
    public async Task HandleGetByStatusAsync_ShouldReturnMatching()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("s1", "s1@test.com"),
            CreateEntry("s2", "s2@test.com")
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByStatusAsync(
            new GetUsersByStatusRequest { Status = "Provisioned" });

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleGetByStatusAsync_ShouldBeCaseInsensitive()
    {
        var entries = new List<UserCacheEntry> { CreateEntry("s1", "s1@test.com") };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByStatusAsync(
            new GetUsersByStatusRequest { Status = "provisioned" });

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleGetByStatusAsync_ShouldReturnEmpty_ForUnknownStatus()
    {
        _cache.GetAllActiveAsync().Returns(new List<UserCacheEntry> { CreateEntry("s1", "s1@test.com") });

        var results = await _handler.HandleGetByStatusAsync(
            new GetUsersByStatusRequest { Status = "Archived" });

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleGetByStatusAsync_ShouldReturnEmpty_WhenNoUsers()
    {
        _cache.GetAllActiveAsync().Returns(new List<UserCacheEntry>());

        var results = await _handler.HandleGetByStatusAsync(
            new GetUsersByStatusRequest { Status = "Provisioned" });

        results.Should().BeEmpty();
    }

    // =========== GetUsersByRole ===========

    [Fact]
    public async Task HandleGetByRoleAsync_ShouldReturnMatchingUsers()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("r1", "r1@test.com", new List<string> { "admin" }),
            CreateEntry("r2", "r2@test.com", new List<string> { "viewer" })
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByRoleAsync(
            new GetUsersByRoleRequest { RoleName = "admin" });

        results.Should().HaveCount(1);
        results[0].UserId.Should().Be("r1");
    }

    [Fact]
    public async Task HandleGetByRoleAsync_ShouldReturnMultipleMatches()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("rm1", "rm1@test.com", new List<string> { "admin", "viewer" }),
            CreateEntry("rm2", "rm2@test.com", new List<string> { "admin" })
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByRoleAsync(
            new GetUsersByRoleRequest { RoleName = "admin" });

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleGetByRoleAsync_ShouldBeCaseInsensitive()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("rc", "rc@test.com", new List<string> { "Admin" })
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByRoleAsync(
            new GetUsersByRoleRequest { RoleName = "ADMIN" });

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleGetByRoleAsync_ShouldReturnEmpty_ForNoMatch()
    {
        var entries = new List<UserCacheEntry>
        {
            CreateEntry("rn", "rn@test.com", new List<string> { "viewer" })
        };
        _cache.GetAllActiveAsync().Returns(entries);

        var results = await _handler.HandleGetByRoleAsync(
            new GetUsersByRoleRequest { RoleName = "nonexistent" });

        results.Should().BeEmpty();
    }

    // =========== Validator Tests ===========

    [Fact]
    public async Task GetUserRequestValidator_ShouldPass_ForValidUserId()
    {
        var logger = Substitute.For<ILogger<GetUserRequestValidator>>();
        var validator = new GetUserRequestValidator(logger);

        var result = await validator.ValidateAsync(new GetUserRequest { UserId = "valid-user" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserRequestValidator_ShouldFail_ForEmptyUserId()
    {
        var logger = Substitute.For<ILogger<GetUserRequestValidator>>();
        var validator = new GetUserRequestValidator(logger);

        var result = await validator.ValidateAsync(new GetUserRequest { UserId = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task GetUserRequestValidator_ShouldFail_ForTooLongUserId()
    {
        var logger = Substitute.For<ILogger<GetUserRequestValidator>>();
        var validator = new GetUserRequestValidator(logger);

        var result = await validator.ValidateAsync(
            new GetUserRequest { UserId = new string('x', 101) });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserRequestValidator_ShouldPass_ForMaxLengthUserId()
    {
        var logger = Substitute.For<ILogger<GetUserRequestValidator>>();
        var validator = new GetUserRequestValidator(logger);

        var result = await validator.ValidateAsync(
            new GetUserRequest { UserId = new string('x', 100) });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserRequestValidator_ShouldPass_ForSingleCharUserId()
    {
        var logger = Substitute.For<ILogger<GetUserRequestValidator>>();
        var validator = new GetUserRequestValidator(logger);

        var result = await validator.ValidateAsync(new GetUserRequest { UserId = "a" });

        result.IsValid.Should().BeTrue();
    }

    // =========== Edge Cases ===========

    [Fact]
    public async Task HandleAsync_ShouldHandleSpecialCharactersInUserId()
    {
        var entry = CreateEntry("user@special!test#123", "special@test.com");
        _cache.GetByUserIdAsync("user@special!test#123").Returns(entry);

        var result = await _handler.HandleAsync(
            new GetUserRequest { UserId = "user@special!test#123" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("user@special!test#123");
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleUnicodeInEmail()
    {
        var entry = CreateEntry("u", "tést@éxample.com");
        _cache.GetByUserIdAsync("u").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "u" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Email.Should().Be("tést@éxample.com");
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateCacheException()
    {
        _cache.GetByUserIdAsync("crash")
            .ThrowsAsync(new TimeoutException("Cache timeout"));

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "crash" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal server error");
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldPropagateCacheException()
    {
        _cache.GetAllActiveAsync().ThrowsAsync(new MongoException("DB down"));

        await _handler.Invoking(h => h.HandleGetAllAsync())
            .Should().ThrowAsync<MongoException>();
    }

    // =========== Cancellation ===========

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var entry = CreateEntry("ct", "ct@test.com");
        _cache.GetByUserIdAsync("ct", cts.Token).Returns(entry);

        var result = await _handler.HandleAsync(
            new GetUserRequest { UserId = "ct" }, cts.Token);

        result.IsSuccess.Should().BeTrue();
        await _cache.Received(1).GetByUserIdAsync("ct", cts.Token);
    }

    [Fact]
    public async Task HandleGetAllAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _cache.GetAllActiveAsync(cts.Token).Returns(new List<UserCacheEntry>());

        var results = await _handler.HandleGetAllAsync(cts.Token);

        results.Should().BeEmpty();
        await _cache.Received(1).GetAllActiveAsync(cts.Token);
    }

    // =========== DTO Mapping ===========

    [Fact]
    public async Task HandleAsync_MappedDto_ShouldHaveStatusProvisioned()
    {
        var entry = CreateEntry("status-user", "status@test.com");
        _cache.GetByUserIdAsync("status-user").Returns(entry);

        var result = await _handler.HandleAsync(
            new GetUserRequest { UserId = "status-user" });

        result.Data!.Status.Should().Be("Provisioned");
    }

    [Fact]
    public async Task HandleAsync_MappedDto_ShouldHaveCreatedAt()
    {
        var createdAt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var entry = new UserCacheEntry
        {
            Id = "date-user", UserId = "date-user",
            Email = "date@test.com", Roles = new List<string>(),
            CreatedAt = createdAt, ExpiresAt = createdAt.AddDays(30)
        };
        _cache.GetByUserIdAsync("date-user").Returns(entry);

        var result = await _handler.HandleAsync(new GetUserRequest { UserId = "date-user" });

        result.Data!.CreatedAt.Should().Be(createdAt);
    }

    // =========== Multiple Simultaneous Reads ===========

    [Fact]
    public async Task HandleAsync_ShouldWorkAfterManyReads()
    {
        var entry = CreateEntry("read", "read@test.com");
        _cache.GetByUserIdAsync("read").Returns(entry);

        for (int i = 0; i < 20; i++)
        {
            var result = await _handler.HandleAsync(new GetUserRequest { UserId = "read" });
            result.IsSuccess.Should().BeTrue($"attempt {i} should succeed");
        }
    }

    private static UserCacheEntry CreateEntry(string userId, string email, List<string>? roles = null)
    {
        return new UserCacheEntry
        {
            Id = userId,
            UserId = userId,
            Email = email,
            Roles = roles ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
    }
}
