using FluentAssertions;
using IdentityService.Api.WriterService.Handlers;
using IdentityService.Api.WriterService.Models;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.Api.WriterService.UnitTests;

/// <summary>
/// Unit tests for AssignRoleHandler with mocked message publisher.
/// </summary>
public class AssignRoleHandlerTests
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<AssignRoleHandler> _logger;
    private readonly AssignRoleHandler _handler;

    public AssignRoleHandlerTests()
    {
        _publisher = Substitute.For<IMessagePublisher>();
        _logger = Substitute.For<ILogger<AssignRoleHandler>>();
        _handler = new AssignRoleHandler(_publisher, _logger);
    }

    // =========== Happy Path ===========

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_AndPublishCommand()
    {
        var request = new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = "role-admin",
            RoleName = "Administrator"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("user-1");
        result.Data.RoleId.Should().Be("role-admin");
        result.Data.CorrelationId.Should().NotBeEmpty();

        await _publisher.Received(1).PublishAsync(
            Arg.Is<SyncRoleCommand>(c =>
                c.Role.UserId == "user-1" &&
                c.Role.RoleId == "role-admin" &&
                c.Role.RoleName == "Administrator"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateUniqueCorrelationIds()
    {
        var ids = new HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            var request = new AssignRoleRequest
            {
                UserId = $"u{i}", RoleId = $"r{i}", RoleName = $"R{i}"
            };
            var result = await _handler.HandleAsync(request);
            ids.Add(result.Data!.CorrelationId);
        }
        ids.Should().HaveCount(30);
    }

    [Fact]
    public async Task HandleAsync_CorrelationId_ShouldBe32Chars()
    {
        var request = new AssignRoleRequest
        {
            UserId = "u", RoleId = "r", RoleName = "R"
        };

        var result = await _handler.HandleAsync(request);

        result.Data!.CorrelationId.Should().HaveLength(32);
    }

    // =========== Validation ===========

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIdEmpty()
    {
        var request = new AssignRoleRequest
        {
            UserId = "", RoleId = "r1", RoleName = "Admin"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("UserId");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRoleIdEmpty()
    {
        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "", RoleName = "Admin"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("RoleId");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRoleNameEmpty()
    {
        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "r1", RoleName = ""
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("RoleName");
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIdWhitespace()
    {
        var request = new AssignRoleRequest
        {
            UserId = "   ", RoleId = "r1", RoleName = "Admin"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenRoleIdWhitespace()
    {
        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "   ", RoleName = "Admin"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAllFieldsEmpty()
    {
        var request = new AssignRoleRequest
        {
            UserId = "", RoleId = "", RoleName = ""
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotPublish_WhenValidationFails()
    {
        var request = new AssignRoleRequest
        {
            UserId = "", RoleId = "r1", RoleName = "Admin"
        };

        await _handler.HandleAsync(request);

        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // =========== Error Handling ===========

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPublisherThrows()
    {
        _publisher.PublishAsync<SyncRoleCommand>(
                Arg.Any<SyncRoleCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("MQ down"));

        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "r1", RoleName = "Admin"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal server error");

        // Should publish FailedIdentityEvent to DLQ
        await _publisher.Received(1).PublishAsync(
            Arg.Any<FailedIdentityEvent>(), Arg.Any<CancellationToken>());
    }

    // =========== Command Properties ===========

    [Fact]
    public async Task HandleAsync_ShouldSetIdempotencyKey()
    {
        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "r1", RoleName = "Admin"
        };

        await _handler.HandleAsync(request);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<SyncRoleCommand>(c => !string.IsNullOrEmpty(c.IdempotencyKey)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldMapRoleCorrectly()
    {
        var request = new AssignRoleRequest
        {
            UserId = "map-user", RoleId = "map-role", RoleName = "Map Role"
        };

        await _handler.HandleAsync(request);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<SyncRoleCommand>(c =>
                c.Role.UserId == "map-user" &&
                c.Role.RoleId == "map-role" &&
                c.Role.RoleName == "Map Role"),
            Arg.Any<CancellationToken>());
    }

    // =========== Edge Cases ===========

    [Fact]
    public async Task HandleAsync_ShouldHandleSpecialCharacters()
    {
        var request = new AssignRoleRequest
        {
            UserId = "user@spec!al#test",
            RoleId = "role::special/name",
            RoleName = "Special & Role Name"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be("user@spec!al#test");
        result.Data.RoleId.Should().Be("role::special/name");
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleUnicode()
    {
        var request = new AssignRoleRequest
        {
            UserId = "unicode-u", RoleId = "rôle-général",
            RoleName = "Général Administratör"
        };

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    // =========== Cancellation ===========

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var request = new AssignRoleRequest
        {
            UserId = "u1", RoleId = "r1", RoleName = "Admin"
        };

        await _handler.HandleAsync(request, cts.Token);

        await _publisher.Received(1).PublishAsync(
            Arg.Any<SyncRoleCommand>(), Arg.Is(cts.Token));
    }

    // =========== Concurrent ===========

    [Fact]
    public async Task HandleAsync_ShouldHandleConcurrentRequests()
    {
        var tasks = Enumerable.Range(0, 20).Select(i =>
        {
            var req = new AssignRoleRequest
            {
                UserId = $"c{i}", RoleId = "r-viewer", RoleName = "Viewer"
            };
            return _handler.HandleAsync(req);
        });

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        results.Should().HaveCount(20);
        await _publisher.Received(20).PublishAsync(
            Arg.Any<SyncRoleCommand>(), Arg.Any<CancellationToken>());
    }
}
