using System.Globalization;
using System.Net;
using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;

namespace JobTrail.IntegrationTests.Identity;

public sealed class RateLimitTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private const int CredentialLimit = 3;
    private const int RefreshLimit = 2;
    private const string AttackerIp = "203.0.113.10";
    private const string OtherIp = "198.51.100.20";

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Identity:RateLimits:CredentialAttemptsPerWindow"] = CredentialLimit.ToString(CultureInfo.InvariantCulture);
        settings["Identity:RateLimits:RefreshesPerWindow"] = RefreshLimit.ToString(CultureInfo.InvariantCulture);
        settings["Identity:RateLimits:Window"] = "00:10:00";
    }

    [Fact]
    public async Task Login_MoreAttemptsThanLimitFromOneIp_ReturnsTooManyRequestsProblemWithRetryAfter()
    {
        using var client = ClientFrom(AttackerIp);
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var allowed = await client.LoginAsync("victim@example.com", "guess-number-" + attempt);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await client.LoginAsync("victim@example.com", "one more guess");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(blocked.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Login_LimitReachedOnOneIp_OtherIpIsStillAllowed()
    {
        using var attacker = ClientFrom(AttackerIp);
        for (var attempt = 0; attempt <= CredentialLimit; attempt++)
        {
            using var _ = await attacker.LoginAsync("victim@example.com", "guess-number-" + attempt);
        }

        using var other = ClientFrom(OtherIp);
        using var response = await other.LoginAsync("someone@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_AfterLoginAttemptsUsedTheBudget_IsAlsoLimited()
    {
        using var client = ClientFrom(AttackerIp);
        for (var attempt = 0; attempt < CredentialLimit; attempt++)
        {
            using var _ = await client.LoginAsync("victim@example.com", "guess-number-" + attempt);
        }

        using var register = await client.RegisterAsync("new@example.com", "ZZZZ-ZZZZ-ZZZZ");

        Assert.Equal(HttpStatusCode.TooManyRequests, register.StatusCode);
    }

    [Fact]
    public async Task Refresh_MoreThanLimitFromOneIp_ReturnsTooManyRequests()
    {
        using var client = ClientFrom(AttackerIp);
        for (var attempt = 0; attempt < RefreshLimit; attempt++)
        {
            using var allowed = await client.RefreshWithTokenAsync("guessed-token-" + attempt);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var blocked = await client.RefreshWithTokenAsync("another-guess");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task Me_ManyRequests_IsNotRateLimited()
    {
        var admin = await ClientFrom(OtherIp).RegisterAdminAsync();
        using var client = ClientFrom(AttackerIp);

        for (var request = 0; request < CredentialLimit * 3; request++)
        {
            using var response = await client.GetMeAsync(admin.AccessToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private HttpClient ClientFrom(string ip)
    {
        var client = CreateCookielessClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, ip);
        return client;
    }
}
