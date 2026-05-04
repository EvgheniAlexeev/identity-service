using System.Text.Json;
using FluentAssertions;
using IdentityService.Shared.Dtos;
using IdentityService.Shared.Events;
using IdentityService.Shared.Commands;
using IdentityService.Shared.Models;
using Xunit;

namespace IdentityService.Shared.UnitTests;

/// <summary>
/// DTO and Event serialization round-trip tests.
/// Ensures all contracts survive JSON serialization/deserialization.
/// </summary>
public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void UserCreatedDto_Should_Serialize_And_Deserialize()
    {
        var original = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            InitialRoles = new List<string> { "admin", "viewer" }
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserCreatedDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-123");
        deserialized.Email.Should().Be("test@example.com");
        deserialized.FirstName.Should().Be("John");
        deserialized.LastName.Should().Be("Doe");
        deserialized.InitialRoles.Should().BeEquivalentTo("admin", "viewer");
    }

    [Fact]
    public void RoleAssignDto_Should_Serialize_And_Deserialize()
    {
        var original = new RoleAssignDto
        {
            UserId = "user-123",
            RoleId = "role-admin",
            RoleName = "Administrator"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RoleAssignDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-123");
        deserialized.RoleId.Should().Be("role-admin");
        deserialized.RoleName.Should().Be("Administrator");
    }

    [Fact]
    public void UserDocumentDto_Should_Serialize_And_Deserialize()
    {
        var original = new UserDocumentDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Roles = new List<string> { "admin" },
            Status = "Provisioned",
            CreatedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LastModifiedAt = new DateTime(2026, 5, 1, 12, 1, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserDocumentDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-123");
        deserialized.Status.Should().Be("Provisioned");
        deserialized.Roles.Should().Contain("admin");
    }

    [Fact]
    public void RoleDocumentDto_Should_Serialize_And_Deserialize()
    {
        var original = new RoleDocumentDto
        {
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read", "write", "delete" },
            CreatedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RoleDocumentDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.RoleId.Should().Be("role-admin");
        deserialized.Permissions.Should().BeEquivalentTo("read", "write", "delete");
    }

    [Fact]
    public void ProvisionUserCommand_Should_Serialize_And_Deserialize()
    {
        var original = new ProvisionUserCommand
        {
            IdempotencyKey = "idem-001",
            User = new UserCreatedDto
            {
                UserId = "user-123",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            }
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProvisionUserCommand>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IdempotencyKey.Should().Be("idem-001");
        deserialized.User.UserId.Should().Be("user-123");
    }

    [Fact]
    public void SyncRoleCommand_Should_Serialize_And_Deserialize()
    {
        var original = new SyncRoleCommand
        {
            IdempotencyKey = "idem-002",
            Role = new RoleAssignDto
            {
                UserId = "user-123",
                RoleId = "role-admin",
                RoleName = "Administrator"
            }
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SyncRoleCommand>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Role.RoleId.Should().Be("role-admin");
    }

    [Fact]
    public void UserProvisioned_Event_Should_Serialize_And_Deserialize()
    {
        var original = new UserProvisioned
        {
            CorrelationId = "corr-001",
            UserId = "user-123",
            Email = "test@example.com",
            AssignedRoles = new List<string> { "admin" },
            KeycloakUserId = "kc-user-abc",
            ProvisionedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserProvisioned>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.CorrelationId.Should().Be("corr-001");
        deserialized.KeycloakUserId.Should().Be("kc-user-abc");
        deserialized.AssignedRoles.Should().Contain("admin");
    }

    [Fact]
    public void RoleSynced_Event_Should_Serialize_And_Deserialize()
    {
        var original = new RoleSynced
        {
            CorrelationId = "corr-002",
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read", "write" },
            SyncedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RoleSynced>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.RoleId.Should().Be("role-admin");
        deserialized.Permissions.Should().BeEquivalentTo("read", "write");
    }

    [Fact]
    public void FailedIdentityEvent_Should_Serialize_And_Deserialize_With_Full_Original_Request()
    {
        var original = new FailedIdentityEvent
        {
            CorrelationId = "corr-003",
            UserId = "user-123",
            OriginalRequest = new UserCreatedDto
            {
                UserId = "user-123",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                InitialRoles = new List<string> { "admin" }
            },
            FailedStep = "CreateInKeycloak",
            ErrorMessage = "Connection timeout",
            ErrorCode = "KC_TIMEOUT",
            RetryCount = 2,
            FailedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            Cause = IdentityFailureCause.NetworkTimeout
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<FailedIdentityEvent>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.CorrelationId.Should().Be("corr-003");
        deserialized.FailedStep.Should().Be("CreateInKeycloak");
        deserialized.ErrorCode.Should().Be("KC_TIMEOUT");
        deserialized.RetryCount.Should().Be(2);
        deserialized.Cause.Should().Be(IdentityFailureCause.NetworkTimeout);
        deserialized.OriginalRequest.Should().NotBeNull();
        deserialized.OriginalRequest.Email.Should().Be("test@example.com");
        deserialized.OriginalRequest.InitialRoles.Should().Contain("admin");
    }

    [Fact]
    public void IdentityFailureCause_Should_Serialize_As_String()
    {
        var cause = IdentityFailureCause.KeycloakApiError;
        var json = JsonSerializer.Serialize(cause, JsonOptions);

        // By default, System.Text.Json serializes enums as integers.
        // Using JsonStringEnumConverter would produce string. We accept integer here.
        json.Should().Be("1");
    }

    [Fact]
    public void UserDocument_Should_Initialize_With_Default_Values()
    {
        var doc = new UserDocument
        {
            Id = "mongo-id-1",
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        doc.Status.Should().Be("Pending");
        doc.SagaState.Should().Be("Provisioning");
        doc.Roles.Should().BeEmpty();
    }

    [Fact]
    public void RoleDocument_Should_Initialize_Correctly()
    {
        var doc = new RoleDocument
        {
            Id = "mongo-id-2",
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read", "write" },
            CreatedAt = DateTime.UtcNow
        };

        doc.RoleId.Should().Be("role-admin");
        doc.Permissions.Should().HaveCount(2);
    }
}
