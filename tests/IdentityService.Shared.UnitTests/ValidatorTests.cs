using FluentAssertions;
using IdentityService.Shared.Dtos;
using IdentityService.Shared.Validators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IdentityService.Shared.UnitTests;

/// <summary>
/// Validator tests for UserCreatedDto and RoleAssignDto.
/// Covers: valid requests pass, invalid inputs rejected, boundary conditions.
/// </summary>
public class ValidatorTests
{
    private readonly ILogger<UserCreatedValidator> _userLogger;
    private readonly ILogger<RoleAssignValidator> _roleLogger;
    private readonly UserCreatedValidator _userValidator;
    private readonly RoleAssignValidator _roleValidator;

    public ValidatorTests()
    {
        _userLogger = Substitute.For<ILogger<UserCreatedValidator>>();
        _roleLogger = Substitute.For<ILogger<RoleAssignValidator>>();
        _userValidator = new UserCreatedValidator(_userLogger);
        _roleValidator = new RoleAssignValidator(_roleLogger);
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Pass_For_Valid_Request()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            InitialRoles = new List<string> { "admin", "viewer" }
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Pass_Without_InitialRoles()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-456",
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_UserId_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_UserId_Too_Long()
    {
        var dto = new UserCreatedDto
        {
            UserId = new string('x', 101),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_Email_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_Email_Invalid_Format()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "not-an-email",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_Email_Too_Long()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = new string('a', 250) + "@b.com", // >255
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_FirstName_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "",
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_LastName_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = ""
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_FirstName_Too_Long()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = new string('x', 101),
            LastName = "Doe"
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_InitialRole_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            InitialRoles = new List<string> { "" }
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "InitialRoles[0]" || e.PropertyName.Contains("InitialRoles"));
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_InitialRole_Too_Long()
    {
        var dto = new UserCreatedDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            InitialRoles = new List<string> { new string('x', 101) }
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "InitialRoles[0]" || e.PropertyName.Contains("InitialRoles"));
    }

    [Fact]
    public async Task UserCreatedValidator_Should_Fail_When_All_Fields_Empty()
    {
        var dto = new UserCreatedDto
        {
            UserId = "",
            Email = "",
            FirstName = "",
            LastName = ""
        };

        var result = await _userValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task RoleAssignValidator_Should_Pass_For_Valid_Request()
    {
        var dto = new RoleAssignDto
        {
            UserId = "user-123",
            RoleId = "role-admin",
            RoleName = "Administrator"
        };

        var result = await _roleValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RoleAssignValidator_Should_Fail_When_UserId_Empty()
    {
        var dto = new RoleAssignDto
        {
            UserId = "",
            RoleId = "role-admin",
            RoleName = "Administrator"
        };

        var result = await _roleValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task RoleAssignValidator_Should_Fail_When_RoleId_Empty()
    {
        var dto = new RoleAssignDto
        {
            UserId = "user-123",
            RoleId = "",
            RoleName = "Administrator"
        };

        var result = await _roleValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleId");
    }

    [Fact]
    public async Task RoleAssignValidator_Should_Fail_When_RoleName_Empty()
    {
        var dto = new RoleAssignDto
        {
            UserId = "user-123",
            RoleId = "role-admin",
            RoleName = ""
        };

        var result = await _roleValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleName");
    }

    [Fact]
    public async Task RoleAssignValidator_Should_Fail_When_All_Fields_Empty()
    {
        var dto = new RoleAssignDto
        {
            UserId = "",
            RoleId = "",
            RoleName = ""
        };

        var result = await _roleValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
