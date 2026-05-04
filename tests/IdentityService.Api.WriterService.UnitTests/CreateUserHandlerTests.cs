using FluentAssertions;
using IdentityService.Api.WriterService.Handlers;
using IdentityService.Api.WriterService.Models;
using IdentityService.CacheLayer.MongoDB;
using IdentityService.CacheLayer.Repositories;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.Api.WriterService.UnitTests;

/// <summary>
/// Comprehensive unit tests for CreateUserHandler with mocked dependencies.
/// </summary>
public class CreateUserHandlerTests
{
    private readonly IUserCacheRepository _cache;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _cache = Substitute.For<IUserCacheRepository>();
        _publisher = Substitute.For<IMessagePublisher>();
        _logger = Substitute.For<ILogger<CreateUserHandler>>();
        _handler = new CreateUserHandler(_cache, _publisher, _logger);
    }

    // =========== Happy Path ===========

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_AndPublishCommand()
    {
        var request = ValidRequest();

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("user-1");
        result.Data.CorrelationId.Should().NotBeEmpty();

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ProvisionUserCommand>(c => c.User.UserId == "user-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldUpsertCacheEntry()
    {
        var request = ValidRequest();

        await _handler.HandleAsync(request);

        await _cache.Received(1).UpsertAsync(
            Arg.Is<UserCacheEntry>(e =>
                e.UserId == "user-1" &&
                e.Email == "test@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WithoutRoles()
    {
        var request = new CreateUserRequest
        {
            UserId = "no-roles",
            Email = "noroles@test.com",
            FirstName = "No",
            LastName = "Roles"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("no-roles");
    }

    [Fact]
    public async Task HandleAsync_ShouldPreserveRoles()
    {
        var request = new CreateUserRequest
        {
            UserId = "role-user",
            Email = "roles@test.com",
            FirstName = "Role",
            LastName = "User",
            InitialRoles = new List<string> { "admin", "viewer", "editor" }
        };

        await _handler.HandleAsync(request);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ProvisionUserCommand>(c =>
                c.User.InitialRoles!.Contains("admin") &&
                c.User.InitialRoles.Contains("viewer") &&
                c.User.InitialRoles.Contains("editor")),
            Arg.Any<CancellationToken>());
    }

    // =========== Validation Failures ===========

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIdEmpty()
    {
        var request = ValidRequest() with { UserId = "" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<ProvisionUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenEmailEmpty()
    {
        var request = ValidRequest() with { Email = "" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenEmailInvalid()
    {
        var request = ValidRequest() with { Email = "not-an-email" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenFirstNameEmpty()
    {
        var request = ValidRequest() with { FirstName = "" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenLastNameEmpty()
    {
        var request = ValidRequest() with { LastName = "" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAllFieldsEmpty()
    {
        var request = new CreateUserRequest
        {
            UserId = "", Email = "", FirstName = "", LastName = ""
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIdTooLong()
    {
        var request = ValidRequest() with { UserId = new string('x', 101) };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenEmailTooLong()
    {
        // UserCreatedValidator has MaximumLength(255) on Email.
        // Use a clearly over-limit email that also fails EmailAddress format.
        var request = ValidRequest() with
        {
            Email = new string('x', 300)
        };

        var result = await _handler.HandleAsync(request);

        // Should fail — 300 chars without @ breaks EmailAddress and MaximumLength(255)
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenFirstNameTooLong()
    {
        var request = ValidRequest() with { FirstName = new string('x', 101) };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenLastNameTooLong()
    {
        var request = ValidRequest() with { LastName = new string('x', 101) };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenEmptyRoleInList()
    {
        var request = ValidRequest() with { InitialRoles = new List<string> { "" } };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRoleNameTooLong()
    {
        var request = ValidRequest() with { InitialRoles = new List<string> { new string('x', 101) } };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    // =========== Boundary Values ===========

    [Fact]
    public async Task HandleAsync_ShouldAccept_MaxLengthUserId()
    {
        var request = ValidRequest() with { UserId = new string('x', 100) };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldAccept_MaxLengthEmail()
    {
        var request = ValidRequest() with { Email = new string('a', 245) + "@b.cd" };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldAccept_MaxLengthNames()
    {
        var request = ValidRequest() with
        {
            FirstName = new string('A', 100),
            LastName = new string('B', 100)
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldAccept_SingleCharValues()
    {
        var request = new CreateUserRequest
        {
            UserId = "1", Email = "a@b.c", FirstName = "A", LastName = "B"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    // =========== Idempotency ===========

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCalledTwice()
    {
        var request = ValidRequest();

        var r1 = await _handler.HandleAsync(request);
        var r2 = await _handler.HandleAsync(request);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r1.Data!.UserId.Should().Be(r2.Data!.UserId);
    }

    // =========== Correlation ID ===========

    [Fact]
    public async Task HandleAsync_ShouldGenerateUniqueCorrelationIds()
    {
        var ids = new HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            var request = new CreateUserRequest
            {
                UserId = $"corr-{i}", Email = $"c{i}@test.com",
                FirstName = "C", LastName = "User"
            };
            var result = await _handler.HandleAsync(request);
            ids.Add(result.Data!.CorrelationId);
        }
        ids.Should().HaveCount(30);
    }

    [Fact]
    public async Task HandleAsync_CorrelationId_ShouldBe32Chars()
    {
        var result = await _handler.HandleAsync(ValidRequest());
        result.Data!.CorrelationId.Should().HaveLength(32);
    }

    // =========== Error Handling / DLQ ===========

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCacheThrows()
    {
        _cache.UpsertAsync(Arg.Any<UserCacheEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("DB down"));

        var result = await _handler.HandleAsync(ValidRequest());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal server error");

        // Should publish FailedIdentityEvent to DLQ
        await _publisher.Received(1).PublishAsync(
            Arg.Any<FailedIdentityEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPublisherThrows()
    {
        _publisher.PublishAsync<ProvisionUserCommand>(
                Arg.Any<ProvisionUserCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("MQ down"));

        var result = await _handler.HandleAsync(ValidRequest());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotPublish_WhenValidationFails()
    {
        var request = ValidRequest() with { UserId = "" };

        await _handler.HandleAsync(request);

        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // =========== Command IdempotencyKey ===========

    [Fact]
    public async Task HandleAsync_ShouldSetIdempotencyKeyOnCommand()
    {
        await _handler.HandleAsync(ValidRequest());

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ProvisionUserCommand>(c => !string.IsNullOrEmpty(c.IdempotencyKey)),
            Arg.Any<CancellationToken>());
    }

    // =========== Cache Entry ===========

    [Fact]
    public async Task HandleAsync_ShouldSetExpiresAtOnCacheEntry()
    {
        await _handler.HandleAsync(ValidRequest());

        await _cache.Received(1).UpsertAsync(
            Arg.Is<UserCacheEntry>(e => e.ExpiresAt > DateTime.UtcNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreRolesInCache()
    {
        var request = ValidRequest() with { InitialRoles = new List<string> { "admin", "viewer" } };

        await _handler.HandleAsync(request);

        await _cache.Received(1).UpsertAsync(
            Arg.Is<UserCacheEntry>(e =>
                e.Roles.Contains("admin") && e.Roles.Contains("viewer")),
            Arg.Any<CancellationToken>());
    }

    // =========== Cancellation ===========

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var request = ValidRequest();

        await _handler.HandleAsync(request, cts.Token);

        await _cache.Received(1).UpsertAsync(
            Arg.Any<UserCacheEntry>(), Arg.Is(cts.Token));
        await _publisher.Received(1).PublishAsync(
            Arg.Any<ProvisionUserCommand>(), Arg.Is(cts.Token));
    }

    private static CreateUserRequest ValidRequest() => new()
    {
        UserId = "user-1",
        Email = "test@example.com",
        FirstName = "John",
        LastName = "Doe"
    };
}
