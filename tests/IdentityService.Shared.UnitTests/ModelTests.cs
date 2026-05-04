using FluentAssertions;
using IdentityService.Shared.Models;
using Xunit;

namespace IdentityService.Shared.UnitTests;

/// <summary>
/// Model instantiation and default-value tests.
/// </summary>
public class ModelTests
{
    [Fact]
    public void UserDocument_Should_Be_Created_With_All_Properties()
    {
        var now = DateTime.UtcNow;
        var doc = new UserDocument
        {
            Id = "mongo-id-1",
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            KeycloakUserId = "kc-abc-123",
            Roles = new List<string> { "admin", "viewer" },
            Status = "Provisioned",
            SagaState = "Completed",
            CreatedAt = now,
            ModifiedAt = now.AddMinutes(5)
        };

        doc.Id.Should().Be("mongo-id-1");
        doc.UserId.Should().Be("user-123");
        doc.Email.Should().Be("test@example.com");
        doc.KeycloakUserId.Should().Be("kc-abc-123");
        doc.Roles.Should().HaveCount(2);
        doc.Status.Should().Be("Provisioned");
        doc.SagaState.Should().Be("Completed");
        doc.CreatedAt.Should().Be(now);
        doc.ModifiedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void UserDocument_Defaults_Should_Be_Pending_And_Provisioning()
    {
        var doc = new UserDocument
        {
            UserId = "user-new",
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User"
        };

        doc.Status.Should().Be("Pending");
        doc.SagaState.Should().Be("Provisioning");
        doc.Roles.Should().BeEmpty();
        doc.KeycloakUserId.Should().BeNull();
        doc.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public void UserDocument_Should_Be_ValueEqual_When_Same_Properties()
    {
        var now = DateTime.UtcNow;
        var doc1 = new UserDocument
        {
            Id = "id-1",
            UserId = "user-1",
            Email = "a@b.com",
            FirstName = "A",
            LastName = "B",
            CreatedAt = now
        };

        var doc2 = new UserDocument
        {
            Id = "id-1",
            UserId = "user-1",
            Email = "a@b.com",
            FirstName = "A",
            LastName = "B",
            CreatedAt = now
        };

        // Records have value-based equality
        doc1.Should().BeEquivalentTo(doc2);
    }

    [Fact]
    public void UserDocument_Should_Not_Be_Equal_When_Properties_Differ()
    {
        var doc1 = new UserDocument
        {
            Id = "id-1",
            UserId = "user-1",
            Email = "a@b.com",
            FirstName = "A",
            LastName = "B"
        };

        var doc2 = new UserDocument
        {
            Id = "id-2",
            UserId = "user-2",
            Email = "c@d.com",
            FirstName = "C",
            LastName = "D"
        };

        doc1.Should().NotBe(doc2);
    }

    [Fact]
    public void RoleDocument_Should_Be_Created_With_All_Properties()
    {
        var now = DateTime.UtcNow;
        var doc = new RoleDocument
        {
            Id = "mongo-id-2",
            RoleId = "role-admin",
            RoleName = "Administrator",
            Permissions = new List<string> { "read", "write", "delete" },
            CreatedAt = now,
            ModifiedAt = now.AddHours(1)
        };

        doc.RoleId.Should().Be("role-admin");
        doc.RoleName.Should().Be("Administrator");
        doc.Permissions.Should().HaveCount(3);
        doc.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void RoleDocument_Should_Be_ValueEqual_When_Same_Properties()
    {
        var doc1 = new RoleDocument
        {
            Id = "id-1",
            RoleId = "role-1",
            RoleName = "Role One",
            Permissions = new List<string> { "read" }
        };

        var doc2 = new RoleDocument
        {
            Id = "id-1",
            RoleId = "role-1",
            RoleName = "Role One",
            Permissions = new List<string> { "read" }
        };

        doc1.Should().BeEquivalentTo(doc2);
    }

    [Fact]
    public void UserDocument_With_Expression_Should_Create_Copy()
    {
        var original = new UserDocument
        {
            Id = "id-1",
            UserId = "user-1",
            Email = "old@example.com",
            FirstName = "John",
            LastName = "Doe",
            Status = "Pending"
        };

        var updated = original with { Status = "Provisioned", KeycloakUserId = "kc-1" };

        updated.Status.Should().Be("Provisioned");
        updated.KeycloakUserId.Should().Be("kc-1");
        updated.UserId.Should().Be("user-1"); // unchanged
        updated.Email.Should().Be("old@example.com"); // unchanged
        original.Status.Should().Be("Pending"); // original unaffected
    }
}
