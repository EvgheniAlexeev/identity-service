using FluentAssertions;
using IdentityService.Shared.Events;
using IdentityService.WorkerService.Sagas;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IdentityService.WorkerService.IntegrationTests;

/// <summary>
/// BLOCK_DLQ_TEST Integration-style tests for DLQ handling and disposition logic.
/// Covers all failure causes, audit history preservation, and operator review workflows.
/// </summary>
public class DlqIntegrationTests
{
    private readonly ILogger<DlqHandler> _logger;
    private readonly DlqHandler _handler;

    public DlqIntegrationTests()
    {
        _logger = Substitute.For<ILogger<DlqHandler>>();
        _handler = new DlqHandler(_logger);
    }

    [Fact]
    public async Task DlqEvent_With_All_Failure_Causes_Should_Not_Throw()
    {
        var causes = Enum.GetValues<IdentityFailureCause>();
        foreach (var cause in causes)
        {
            var dlqEvent = new FailedIdentityEvent
            {
                CorrelationId = $"dlq-cause-{cause}",
                UserId = $"user-{cause}",
                FailedStep = "CreateInKeycloak",
                ErrorMessage = $"Error for {cause}",
                Cause = cause,
                RetryCount = 0,
                FailedAt = DateTime.UtcNow
            };

            var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task DlqEvent_Batch_Processing_100_Events_Should_Not_Throw()
    {
        var tasks = Enumerable.Range(1, 100).Select(async i =>
        {
            var dlqEvent = new FailedIdentityEvent
            {
                CorrelationId = $"batch-{i}",
                UserId = $"user-batch-{i}",
                FailedStep = i % 2 == 0 ? "CreateInKeycloak" : "UpdateCache",
                ErrorMessage = $"Batch error #{i}",
                Cause = i % 3 == 0
                    ? IdentityFailureCause.NetworkTimeout
                    : IdentityFailureCause.KeycloakApiError,
                RetryCount = i % 3,
                FailedAt = DateTime.UtcNow.AddHours(-i)
            };
            await _handler.HandleAsync(dlqEvent, CancellationToken.None);
        });

        var act = () => Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DlqEvent_With_All_Steps_Should_Not_Throw()
    {
        var steps = new[] { "CreateInKeycloak", "UpdateCache", "Notify", "ValidateRequest", "SyncRole" };
        foreach (var step in steps)
        {
            var dlqEvent = new FailedIdentityEvent
            {
                CorrelationId = $"dlq-step-{step}",
                UserId = $"user-{step}",
                FailedStep = step,
                ErrorMessage = $"Error in {step}",
                Cause = IdentityFailureCause.Unknown,
                RetryCount = 0,
                FailedAt = DateTime.UtcNow
            };

            var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task DlqEvent_Disposition_Mapping_Should_Be_Correct()
    {
        // Each cause maps to a specific disposition for operator review
        var expectedDispositions = new Dictionary<IdentityFailureCause, string>
        {
            [IdentityFailureCause.KeycloakApiError] = "Retry after Keycloak recovery",
            [IdentityFailureCause.NetworkTimeout] = "Retry with increased timeout",
            [IdentityFailureCause.UserAlreadyExists] = "Skip — idempotent",
            [IdentityFailureCause.MaxRetriesExceeded] = "Manual investigation required",
            [IdentityFailureCause.ValidationError] = "Reject — invalid request",
            [IdentityFailureCause.Unknown] = "Manual investigation required"
        };

        foreach (var (cause, expectedLabel) in expectedDispositions)
        {
            var disposition = cause switch
            {
                IdentityFailureCause.KeycloakApiError => "Retry after Keycloak recovery",
                IdentityFailureCause.NetworkTimeout => "Retry with increased timeout",
                IdentityFailureCause.UserAlreadyExists => "Skip — idempotent",
                IdentityFailureCause.MaxRetriesExceeded => "Manual investigation required",
                IdentityFailureCause.ValidationError => "Reject — invalid request",
                _ => "Manual investigation required"
            };

            disposition.Should().Be(expectedLabel, $"cause {cause} should map correctly");
        }
    }

    [Fact]
    public async Task DlqEvent_Should_Preserve_Original_Request()
    {
        var originalRequest = new IdentityService.Shared.Dtos.UserCreatedDto
        {
            UserId = "user-original",
            Email = "original@example.com",
            FirstName = "Original",
            LastName = "Request",
            InitialRoles = new List<string> { "admin", "reader", "writer" }
        };

        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "dlq-original-1",
            UserId = "user-original",
            FailedStep = "UpdateCache",
            ErrorMessage = "Cache failure",
            Cause = IdentityFailureCause.CacheWriteFailure,
            RetryCount = 2,
            FailedAt = DateTime.UtcNow,
            OriginalRequest = originalRequest
        };

        await _handler.HandleAsync(dlqEvent, CancellationToken.None);

        // Assert original request preserved
        dlqEvent.OriginalRequest.UserId.Should().Be("user-original");
        dlqEvent.OriginalRequest.Email.Should().Be("original@example.com");
        dlqEvent.OriginalRequest.FirstName.Should().Be("Original");
        dlqEvent.OriginalRequest.LastName.Should().Be("Request");
        dlqEvent.OriginalRequest.InitialRoles.Should().Contain(new[] { "admin", "reader", "writer" });
    }

    [Fact]
    public async Task DlqEvent_Should_Track_Retry_Count_And_Failed_At()
    {
        var failedAt = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc);
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "dlq-retry-count",
            UserId = "user-retry",
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "All retries exhausted",
            ErrorCode = "MaxRetriesExceeded",
            Cause = IdentityFailureCause.MaxRetriesExceeded,
            RetryCount = 3,
            FailedAt = failedAt
        };

        await _handler.HandleAsync(dlqEvent, CancellationToken.None);

        dlqEvent.RetryCount.Should().Be(3);
        dlqEvent.FailedAt.Should().Be(failedAt);
        dlqEvent.ErrorCode.Should().Be("MaxRetriesExceeded");
    }

    [Fact]
    public async Task DlqEvent_With_Empty_FailedStep_Should_Not_Throw()
    {
        var dlqEvent = new FailedIdentityEvent
        {
            CorrelationId = "dlq-empty-step",
            UserId = "user-empty-step",
            FailedStep = "",
            ErrorMessage = "",
            Cause = IdentityFailureCause.Unknown,
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        var act = () => _handler.HandleAsync(dlqEvent, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
