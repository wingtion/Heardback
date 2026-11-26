using System.Net;
using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Rate limits behind Netlify: each client IP that Netlify vouches for gets its own budget.</summary>
public sealed class NetlifyProxyRateLimitTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private const int CredentialLimit = 2;
    private const string NetlifyEgress = "10.0.0.5";

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        NetlifyProxyTests.ConfigureNetlify(settings);
        settings["Identity:RateLimits:CredentialAttemptsPerWindow"] = "2";
        settings["Identity:RateLimits:Window"] = "00:10:00";
    }

    [Fact]
    public async Task Login_ThroughNetlify_LimitsEachClientSeparately()
    {
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var allowed = await LoginAsync(clientIp: "203.0.113.1");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await LoginAsync(clientIp: "203.0.113.1");
        using var otherClient = await LoginAsync(clientIp: "203.0.113.2");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherClient.StatusCode);
    }

    [Fact]
    public async Task Login_ThroughNetlifyWithSpoofedForwardedFor_IgnoresTheSpoofedHeader()
    {
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var allowed = await LoginAsync(clientIp: "203.0.113.1", forwardedFor: $"198.51.100.{attempt + 10}");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await LoginAsync(clientIp: "203.0.113.1", forwardedFor: "198.51.100.99");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    private async Task<HttpResponseMessage> LoginAsync(string clientIp, string? forwardedFor = null)
    {
        using var client = CreateCookielessClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, NetlifyEgress);
        client.DefaultRequestHeaders.Add(NetlifySignature.HeaderName, NetlifySignature.Create());
        client.DefaultRequestHeaders.Add(NetlifySignature.ClientIpHeader, clientIp);
        if (forwardedFor is not null)
        {
            client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        }

        return await client.LoginAsync("victim@example.com", "a wrong password guess");
    }
}
