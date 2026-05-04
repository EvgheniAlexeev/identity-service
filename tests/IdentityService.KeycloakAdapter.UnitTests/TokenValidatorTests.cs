using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using IdentityService.KeycloakAdapter.Auth;
using IdentityService.KeycloakAdapter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace IdentityService.KeycloakAdapter.UnitTests;

/// <summary>
/// Tests for TokenValidator using real JWT generation and mocked JWKS endpoint.
/// </summary>
public class TokenValidatorTests
{
    private readonly ILogger<TokenValidator> _logger;
    private readonly ILogger<JwksCache> _jwkLogger;
    private readonly KeycloakConfig _config;
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _signingKey;
    private readonly string _issuer;

    public TokenValidatorTests()
    {
        _logger = Substitute.For<ILogger<TokenValidator>>();
        _jwkLogger = Substitute.For<ILogger<JwksCache>>();
        _rsa = RSA.Create(2048);
        _signingKey = new RsaSecurityKey(_rsa) { KeyId = "test-kid-1" };
        _issuer = "https://auth.example.com/realms/test-realm";

        _config = new KeycloakConfig
        {
            Url = "https://auth.example.com",
            Realm = "test-realm",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };
    }

    private string CreateJwtToken(
        string subject,
        string audience,
        DateTime? expires = null,
        string? issuer = null,
        SecurityKey? signingKey = null)
    {
        var now = DateTime.UtcNow;
        var key = signingKey ?? _signingKey;
        var tokenIssuer = issuer ?? _issuer;

        var tokenExpires = expires ?? now.AddMinutes(5);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Email, $"{subject}@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("preferred_username", subject)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = tokenExpires,
            Issuer = tokenIssuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }

    private string CreateJwksResponse()
    {
        var key = JsonWebKeyConverter.ConvertFromRSASecurityKey(
            new RsaSecurityKey(_rsa.ExportParameters(false)));
        key.KeyId = "test-kid-1";
        key.Alg = SecurityAlgorithms.RsaSha256;
        key.Use = "sig";

        return System.Text.Json.JsonSerializer.Serialize(new { keys = new[] { key } });
    }

    [Fact]
    public async Task Validate_Should_Accept_Valid_Token()
    {
        // Arrange
        var token = CreateJwtToken("user-123", "test-client");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        // Act
        var result = await validator.ValidateAsync(token);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Subject.Should().NotBeNull();
        result.Issuer.Should().Be(_issuer);
        result.Claims.Should().NotBeEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Validate_Should_Reject_Expired_Token()
    {
        // Create a token already expired: issued 10 min ago, expires 5 min ago
        var pastTime = DateTime.UtcNow.AddMinutes(-10);
        var expiredTime = DateTime.UtcNow.AddMinutes(-5);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user-123"),
            new Claim(JwtRegisteredClaimNames.Email, "user-123@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiredTime,
            Issuer = _issuer,
            Audience = "test-client",
            IssuedAt = pastTime,
            NotBefore = pastTime,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(tokenDescriptor));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task Validate_Should_Reject_Token_With_Wrong_Signature()
    {
        // Create a token signed with a DIFFERENT key
        var wrongRsa = RSA.Create(2048);
        var wrongKey = new RsaSecurityKey(wrongRsa) { KeyId = "wrong-kid" };
        var token = CreateJwtToken("user-123", "test-client", signingKey: wrongKey);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_Should_Reject_Token_With_Wrong_Issuer()
    {
        var token = CreateJwtToken("user-123", "test-client",
            issuer: "https://evil.example.com/realms/fake");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_Should_Reject_Token_With_Wrong_Audience()
    {
        var token = CreateJwtToken("user-123", "wrong-client");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_Should_Reject_Garbage_Token()
    {
        var mockHttp = new MockHttpMessageHandler();
        // JWKS won't even be fetched since the token is unparseable
        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync("this-is-not-a-jwt-token");

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TokenValidationResult_Should_Report_Correct_Claims()
    {
        var token = CreateJwtToken("user-789", "test-client");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                "https://auth.example.com/realms/test-realm/protocol/openid-connect/certs")
            .Respond("application/json", CreateJwksResponse());

        var httpClient = mockHttp.ToHttpClient();
        var jwksCache = new JwksCache(_jwkLogger, 3600);
        var validator = new TokenValidator(jwksCache, httpClient, _config, _logger);

        var result = await validator.ValidateAsync(token);

        result.IsValid.Should().BeTrue();
        result.Subject.Should().NotBeNull();
        result.Issuer.Should().Be(_issuer);
        result.ExpiresAt.Should().HaveValue();
        result.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email ||
            c.Type == ClaimTypes.Email ||
            c.Type == "email");
    }
}
