using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using IdentityService.KeycloakAdapter.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace IdentityService.KeycloakAdapter.UnitTests;

/// <summary>
/// Tests for JwksCache — caching, refresh, and invalidation.
/// </summary>
public class JwksCacheTests
{
    private readonly ILogger<JwksCache> _logger;
    private const string JwksUrl = "https://auth.example.com/realms/test/certs";

    public JwksCacheTests()
    {
        _logger = Substitute.For<ILogger<JwksCache>>();
    }

    private string CreateJwksJson()
    {
        var rsa = RSA.Create(2048);
        var key = JsonWebKeyConverter.ConvertFromRSASecurityKey(
            new RsaSecurityKey(rsa.ExportParameters(false)));
        key.KeyId = "test-kid";
        key.Alg = SecurityAlgorithms.RsaSha256;
        key.Use = "sig";

        return JsonSerializer.Serialize(new { keys = new[] { key } });
    }

    [Fact]
    public async Task GetJwks_Should_Fetch_From_Endpoint_On_First_Call()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());

        var httpClient = mockHttp.ToHttpClient();
        var cache = new JwksCache(_logger, 3600);

        var result = await cache.GetJwksAsync(httpClient, JwksUrl);

        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("test-kid");
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetJwks_Should_Use_Cache_On_Second_Call()
    {
        var mockHttp = new MockHttpMessageHandler();
        // Expect exactly one HTTP call
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());

        var httpClient = mockHttp.ToHttpClient();
        var cache = new JwksCache(_logger, 3600);

        // First call — fetches from endpoint
        var result1 = await cache.GetJwksAsync(httpClient, JwksUrl);
        result1.Keys.Should().HaveCount(1);

        // Second call — should use cache, no HTTP request
        var result2 = await cache.GetJwksAsync(httpClient, JwksUrl);
        result2.Keys.Should().HaveCount(1);

        // Cache should indicate it won't expire soon
        cache.CacheExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verify only one HTTP call was made
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetJwks_Should_Refresh_When_Cache_Expired()
    {
        var mockHttp = new MockHttpMessageHandler();
        // Two calls expected: one initial, one refresh
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());

        var httpClient = mockHttp.ToHttpClient();
        // Set TTL to 0 so cache expires immediately
        var cache = new JwksCache(_logger, 0);

        // First call fetches and sets TTL=0 (already expired)
        await cache.GetJwksAsync(httpClient, JwksUrl);

        // Second call should refresh because TTL is 0
        await cache.GetJwksAsync(httpClient, JwksUrl);

        // Both expectations should be consumed
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Invalidate_Should_Cause_Refresh_On_Next_Call()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());

        var httpClient = mockHttp.ToHttpClient();
        var cache = new JwksCache(_logger, 3600);

        // Initial fetch
        await cache.GetJwksAsync(httpClient, JwksUrl);

        // Invalidate the cache
        cache.Invalidate();
        cache.CacheExpiresAt.Should().Be(DateTime.MinValue);

        // Next call should fetch again
        await cache.GetJwksAsync(httpClient, JwksUrl);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetJwks_Should_Throw_When_Endpoint_Fails()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        var cache = new JwksCache(_logger, 3600);

        var act = () => cache.GetJwksAsync(httpClient, JwksUrl);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public void CacheExpiresAt_Should_Default_To_MinValue_When_Empty()
    {
        var cache = new JwksCache(_logger, 3600);
        cache.CacheExpiresAt.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task CacheExpiresAt_Should_Be_Set_After_Fetch()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, JwksUrl)
            .Respond("application/json", CreateJwksJson());

        var httpClient = mockHttp.ToHttpClient();
        var cache = new JwksCache(_logger, 3600);

        var before = DateTime.UtcNow;
        await cache.GetJwksAsync(httpClient, JwksUrl);

        cache.CacheExpiresAt.Should().BeAfter(before.AddSeconds(3595)); // ~1hr
    }
}
