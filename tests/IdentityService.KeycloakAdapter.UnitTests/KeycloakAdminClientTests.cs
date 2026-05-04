using System.Net;
using System.Text.Json;
using FluentAssertions;
using IdentityService.KeycloakAdapter.Admin;
using IdentityService.KeycloakAdapter.Models;
using IdentityService.Shared.Dtos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace IdentityService.KeycloakAdapter.UnitTests;

/// <summary>
/// Tests for KeycloakAdminClient using MockHttp to simulate API responses.
/// </summary>
public class KeycloakAdminClientTests
{
    private readonly ILogger<KeycloakAdminClient> _logger;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfig _config;
    private readonly KeycloakAdminClient _client;

    public KeycloakAdminClientTests()
    {
        _logger = Substitute.For<ILogger<KeycloakAdminClient>>();
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://auth.example.com");

        _config = new KeycloakConfig
        {
            Url = "https://auth.example.com",
            Realm = "test-realm",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        _client = new KeycloakAdminClient(_httpClient, _config, _logger);
    }

    [Fact]
    public async Task CreateUser_Should_Return_KeycloakUserId_From_Location_Header()
    {
        // Arrange
        var user = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockHttp.Expect(HttpMethod.Post, "https://auth.example.com/admin/realms/test-realm/users")
            .Respond(req =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.Headers.Location = new Uri(
                    "https://auth.example.com/admin/realms/test-realm/users/kc-user-abc-123");
                return response;
            });

        // Act
        var result = await _client.CreateUserAsync(user);

        // Assert
        result.Should().Be("kc-user-abc-123");
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CreateUser_Should_Throw_KeycloakException_On_Api_Error()
    {
        var user = new UserCreatedDto
        {
            UserId = "user-456",
            Email = "conflict@example.com",
            FirstName = "Jane",
            LastName = "Smith"
        };

        _mockHttp.Expect(HttpMethod.Post, "https://auth.example.com/admin/realms/test-realm/users")
            .Respond(HttpStatusCode.Conflict, "application/json",
                JsonSerializer.Serialize(new { error = "User already exists" }));

        // Act
        var act = () => _client.CreateUserAsync(user);

        // Assert
        await act.Should().ThrowAsync<KeycloakException>()
            .Where(ex => ex.StatusCode == 409);
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AssignRole_Should_Succeed_On_NoContent_Response()
    {
        _mockHttp.Expect(HttpMethod.Post,
                "https://auth.example.com/admin/realms/test-realm/users/kc-user-123/role-mappings/realm")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var act = () => _client.AssignRoleAsync("kc-user-123", "role-admin");

        // Assert — should not throw
        await act.Should().NotThrowAsync();
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AssignRole_Should_Throw_On_Error_Response()
    {
        _mockHttp.Expect(HttpMethod.Post,
                "https://auth.example.com/admin/realms/test-realm/users/kc-user-123/role-mappings/realm")
            .Respond(HttpStatusCode.BadRequest);

        var act = () => _client.AssignRoleAsync("kc-user-123", "role-invalid");

        await act.Should().ThrowAsync<KeycloakException>()
            .Where(ex => ex.StatusCode == 400);
    }

    [Fact]
    public async Task GetUser_Should_Return_User_When_Found()
    {
        var expectedUser = new KeycloakUser
        {
            Id = "kc-user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Enabled = true
        };

        _mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/admin/realms/test-realm/users/kc-user-123")
            .Respond("application/json",
                JsonSerializer.Serialize(expectedUser, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

        var result = await _client.GetUserAsync("kc-user-123");

        result.Should().NotBeNull();
        result!.Id.Should().Be("kc-user-123");
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetUser_Should_Return_Null_When_NotFound()
    {
        _mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/admin/realms/test-realm/users/nonexistent")
            .Respond(HttpStatusCode.NotFound);

        var result = await _client.GetUserAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUser_Should_Throw_On_Server_Error()
    {
        _mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/admin/realms/test-realm/users/any-user")
            .Respond(HttpStatusCode.InternalServerError);

        var act = () => _client.GetUserAsync("any-user");

        await act.Should().ThrowAsync<KeycloakException>()
            .Where(ex => ex.StatusCode == 500);
    }

    [Fact]
    public async Task DeleteUser_Should_Succeed_On_NoContent()
    {
        _mockHttp.Expect(HttpMethod.Delete,
                "https://auth.example.com/admin/realms/test-realm/users/kc-user-123")
            .Respond(HttpStatusCode.NoContent);

        var act = () => _client.DeleteUserAsync("kc-user-123");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteUser_Should_Not_Throw_On_Already_Deleted()
    {
        _mockHttp.Expect(HttpMethod.Delete,
                "https://auth.example.com/admin/realms/test-realm/users/kc-user-123")
            .Respond(HttpStatusCode.NotFound);

        var act = () => _client.DeleteUserAsync("kc-user-123");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task KeycloakException_Should_Store_StatusCode()
    {
        var ex = new KeycloakException("Test error", 500);
        ex.StatusCode.Should().Be(500);
        ex.Message.Should().Be("Test error");
    }
}
