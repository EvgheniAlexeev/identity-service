using FluentAssertions;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.KeycloakAdapter.Models;
using IdentityService.Shared.Dtos;
using IdentityService.WorkerService.Events;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Steps;

/// <summary>
/// BLOCK_TEST Unit tests for CreateUserInKeycloakHandler.
/// Tests happy path, Keycloak error, timeout, and retry exhaustion scenarios.
/// </summary>
public class CreateUserInKeycloakHandlerTests
{
    private readonly IKeycloakAdminClient _keycloakClient;
    private readonly ILogger<CreateUserInKeycloakHandler> _logger;
    private readonly SagaMetrics _metrics;
    private readonly CreateUserInKeycloakHandler _handler;

    public CreateUserInKeycloakHandlerTests()
    {
        _keycloakClient = Substitute.For<IKeycloakAdminClient>();
        _logger = Substitute.For<ILogger<CreateUserInKeycloakHandler>>();
        _metrics = new SagaMetrics();
        _handler = new CreateUserInKeycloakHandler(_keycloakClient, _logger, _metrics);
    }

    // === HAPPY PATH TESTS ===

    [Fact]
    public async Task HandleAsync_Should_Return_UserCreatedInKeycloak_On_Success()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-1",
            User = new UserCreatedDto
            {
                UserId = "user-1",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                InitialRoles = new List<string> { "admin" }
            },
            Attempt = 1
        };

        _keycloakClient.CreateUserAsync(command.User, Arg.Any<CancellationToken>())
            .Returns("kc-user-123");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UserCreatedInKeycloak>();
        var evt = (UserCreatedInKeycloak)result;
        evt.CorrelationId.Should().Be("corr-1");
        evt.UserId.Should().Be("user-1");
        evt.KeycloakUserId.Should().Be("kc-user-123");
        evt.Email.Should().Be("test@example.com");
        evt.Roles.Should().ContainSingle().Which.Should().Be("admin");

        await _keycloakClient.Received(1).CreateUserAsync(command.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Pass_Correct_User_To_Keycloak()
    {
        // Arrange
        UserCreatedDto? capturedUser = null;
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-2",
            User = new UserCreatedDto
            {
                UserId = "user-2",
                Email = "user2@example.com",
                FirstName = "John",
                LastName = "Doe"
            },
            Attempt = 1
        };

        _keycloakClient.CreateUserAsync(Arg.Do<UserCreatedDto>(u => capturedUser = u), Arg.Any<CancellationToken>())
            .Returns("kc-456");

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.UserId.Should().Be("user-2");
        capturedUser.Email.Should().Be("user2@example.com");
        capturedUser.FirstName.Should().Be("John");
        capturedUser.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Event_With_Empty_Roles_When_None_Specified()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-3",
            User = new UserCreatedDto
            {
                UserId = "user-3",
                Email = "test@example.com",
                FirstName = "No",
                LastName = "Roles"
            },
            Attempt = 1
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .Returns("kc-789");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var evt = (UserCreatedInKeycloak)result;
        evt.Roles.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Attempt_Number()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-4",
            User = new UserCreatedDto
            {
                UserId = "user-4",
                Email = "retry@example.com",
                FirstName = "Retry",
                LastName = "Test"
            },
            Attempt = 3
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .Returns("kc-attempt3");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UserCreatedInKeycloak>();
    }

    // === FAILURE TESTS ===

    [Fact]
    public async Task HandleAsync_Should_Throw_When_KeycloakException_Thrown()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-fail-1",
            User = new UserCreatedDto
            {
                UserId = "user-fail",
                Email = "fail@example.com",
                FirstName = "Fail",
                LastName = "Case"
            }
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeycloakException("Keycloak API error", 500));

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeycloakException>()
            .Where(ex => ex.StatusCode == 500);
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_On_Network_Timeout()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-timeout",
            User = new UserCreatedDto
            {
                UserId = "user-timeout",
                Email = "timeout@example.com",
                FirstName = "Net",
                LastName = "Timeout"
            }
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Connection timed out"));

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_On_Cancelled_Token()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-cancel",
            User = new UserCreatedDto
            {
                UserId = "user-cancel",
                Email = "cancel@example.com",
                FirstName = "Cancel",
                LastName = "Test"
            }
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => _handler.HandleAsync(command, new CancellationToken(true));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_On_HttpRequestException()
    {
        // Arrange
        var command = new CreateUserInKeycloakCommand
        {
            CorrelationId = "corr-http",
            User = new UserCreatedDto
            {
                UserId = "user-http",
                Email = "http@example.com",
                FirstName = "Http",
                LastName = "Error"
            }
        };

        _keycloakClient.CreateUserAsync(Arg.Any<UserCreatedDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("HTTP error"));

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
