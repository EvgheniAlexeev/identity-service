using FluentAssertions;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Sagas;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Sagas;

/// <summary>
/// BLOCK_TEST Unit tests for DlqHandler.
/// Tests DLQ event handling, disposition routing, and error classification.
/// </summary>
public class DlqHandlerTests
{
    private readonly ILogger<DlqHandler> _logger;
    private readonly DlqHandler _handler;

    public DlqHandlerTests()
    {
        _logger = Substitute.For<ILogger<DlqHandler>>();
        _handler = new DlqHandler(_logger);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_Keycloak_Api_Error()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "Keycloak 503",
            Cause = IdentityFailureCause.KeycloakApiError,
            RetryCount = 3,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_Network_Timeout()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-2",
            UserId = "user-2",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "Connection timed out",
            Cause = IdentityFailureCause.NetworkTimeout,
            RetryCount = 3,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_User_Already_Exists()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-3",
            UserId = "user-3",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "User already exists",
            Cause = IdentityFailureCause.UserAlreadyExists,
            RetryCount = 1,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_Max_Retries_Exceeded()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-4",
            UserId = "user-4",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "All retries exhausted",
            Cause = IdentityFailureCause.MaxRetriesExceeded,
            RetryCount = 3,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_Validation_Error()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-5",
            UserId = "user-5",
            FailedStep = "ValidateRequest",
            ErrorMessage = "Invalid email format",
            Cause = IdentityFailureCause.ValidationError,
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_For_Unknown_Cause()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-6",
            UserId = "user-6",
            FailedStep = "Unknown",
            ErrorMessage = "Unexpected error",
            Cause = IdentityFailureCause.Unknown,
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Cancelled_Token()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-7",
            UserId = "user-7",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "Cancelled",
            Cause = IdentityFailureCause.KeycloakApiError,
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, new CancellationToken(true));

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Null_Optional_Fields()
    {
        // Arrange
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "corr-8",
            UserId = "user-8",
            FailedStep = string.Empty,
            ErrorMessage = string.Empty,
            Cause = IdentityFailureCause.Unknown,
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        // Act
        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Multiple_Consecutive_DLQ_Events()
    {
        // Arrange
        var events = Enumerable.Range(1, 50).Select(i => new FailedIdentityEvent
        {
            CorrelationId = $"corr-dlq-{i}",
            UserId = $"user-dlq-{i}",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = $"Error #{i}",
            Cause = IdentityFailureCause.KeycloakApiError,
            RetryCount = 3,
            FailedAt = DateTime.UtcNow
        });

        // Act
        foreach (var e in events)
        {
            var act = () => _handler.HandleAsync(e, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
    }
}
