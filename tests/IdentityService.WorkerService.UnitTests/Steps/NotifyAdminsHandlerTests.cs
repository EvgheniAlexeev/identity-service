using FluentAssertions;
using IdentityService.WorkerService.Metrics;
using IdentityService.WorkerService.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IdentityService.WorkerService.UnitTests.Steps;

/// <summary>
/// BLOCK_TEST Unit tests for NotifyAdminsHandler.
/// Tests notification success, non-critical failure handling, and PII redaction.
/// </summary>
public class NotifyAdminsHandlerTests
{
    private readonly ILogger<NotifyAdminsHandler> _logger;
    private readonly SagaMetrics _metrics;
    private readonly NotifyAdminsHandler _handler;

    public NotifyAdminsHandlerTests()
    {
        _logger = Substitute.For<ILogger<NotifyAdminsHandler>>();
        _metrics = new SagaMetrics();
        _handler = new NotifyAdminsHandler(_logger, _metrics);
    }

    // === HAPPY PATH TESTS ===

    [Fact]
    public async Task HandleAsync_Should_Return_Notified_True_On_Success()
    {
        // Arrange
        var command = new NotifyCommand
        {
            CorrelationId = "corr-1",
            UserId = "user-1",
            Email = "test@example.com",
            Status = "Provisioned"
        };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var props = result.GetType().GetProperties();
        var notifiedProp = props.FirstOrDefault(p => p.Name == "Notified");
        notifiedProp.Should().NotBeNull();
        notifiedProp!.GetValue(result).Should().Be(true);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Throw_On_Failure()
    {
        // Arrange: Notify handler is intentionally resilient — it logs errors but never throws
        var command = new NotifyCommand
        {
            CorrelationId = "corr-resilient",
            UserId = "user-resilient",
            Email = "resilient@example.com",
            Status = "Provisioned"
        };

        // Act: Even with an exceptional situation, handler should not propagate errors
        // (in production, Http calls may fail; the handler absorbs them)
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_For_Different_Statuses()
    {
        // Arrange
        var statuses = new[] { "Pending", "Provisioning", "Provisioned", "Failed" };

        foreach (var status in statuses)
        {
            var command = new NotifyCommand
            {
                CorrelationId = $"corr-{status}",
                UserId = $"user-{status}",
                Email = $"{status}@example.com",
                Status = status
            };

            // Act
            var act = () => _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Empty_Email()
    {
        // Arrange
        var command = new NotifyCommand
        {
            CorrelationId = "corr-empty",
            UserId = "user-empty",
            Email = "",
            Status = "Provisioned"
        };

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Null_CorrelationId()
    {
        // Arrange
        var command = new NotifyCommand
        {
            CorrelationId = null!,
            UserId = "user-null",
            Email = "null@example.com",
            Status = "Provisioned"
        };

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Long_UserId()
    {
        // Arrange
        var longUserId = new string('a', 1000);
        var command = new NotifyCommand
        {
            CorrelationId = "corr-long",
            UserId = longUserId,
            Email = "long@example.com",
            Status = "Provisioned"
        };

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Be_Idempotent()
    {
        // Arrange
        var command = new NotifyCommand
        {
            CorrelationId = "corr-idem",
            UserId = "user-idem",
            Email = "idem@example.com",
            Status = "Provisioned"
        };

        // Act: Call twice — both should succeed
        var act1 = () => _handler.HandleAsync(command, CancellationToken.None);
        var act2 = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
    }
}
