using System.Net;
using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Rate limits behind a proxy: each real client gets its own budget, and only trusted proxies are believed.</summary>
public sealed class ClientIpRateLimitTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private const int CredentialLimit = 2;
    private const string TrustedProxy = "10.0.0.5";
    private const string UntrustedSource = "198.51.100.9";

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["ClientIp:Source"] = "ForwardedHeaders";
        settings["ClientIp:TrustedProxies:0"] = TrustedProxy;
        settings["Identity:RateLimits:CredentialAttemptsPerWindow"] = "2";
        settings["Identity:RateLimits:Window"] = "00:10:00";
    }

    [Fact]
    public async Task Login_ThroughTrustedProxy_LimitsEachForwardedClientSeparately()
    {
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var allowed = await LoginAsync(remoteIp: TrustedProxy, forwardedFor: "203.0.113.1");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await LoginAsync(remoteIp: TrustedProxy, forwardedFor: "203.0.113.1");
        using var otherClient = await LoginAsync(remoteIp: TrustedProxy, forwardedFor: "203.0.113.2");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherClient.StatusCode);
    }

    [Fact]
    public async Task Login_ForwardedForFromUntrustedSource_IsIgnoredSoSpoofingDoesNotBypassTheLimit()
    {
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var allowed = await LoginAsync(remoteIp: UntrustedSource, forwardedFor: $"203.0.113.{attempt + 10}");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await LoginAsync(remoteIp: UntrustedSource, forwardedFor: "203.0.113.99");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    private async Task<HttpResponseMessage> LoginAsync(string remoteIp, string forwardedFor)
    {
        using var client = CreateCookielessClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, remoteIp);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        return await client.LoginAsync("victim@example.com", "a wrong password guess");
    }
}
