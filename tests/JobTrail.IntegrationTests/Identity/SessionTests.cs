using System.Net;
using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;

namespace JobTrail.IntegrationTests.Identity;

public sealed class SessionTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Refresh_ValidCookie_ReturnsWorkingAccessTokenAndRotatesCookie()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var firstToken = registered.ReadRefreshToken();

        using var refreshed = await Client.RefreshAsync();
        var auth = await refreshed.ReadAuthAsync();
        using var me = await Client.GetMeAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(firstToken, refreshed.ReadRefreshToken());
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        using var response = await Client.RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorizedAndClearsCookie()
    {
        using var response = await CreateCookielessClient().RefreshWithTokenAsync("not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.ReadRefreshToken());
        Assert.NotNull(response.FindRefreshCookieHeader());
    }

    [Fact]
    public async Task Refresh_RotatedTokenReplayedAfterGracePeriod_RevokesWholeSession()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var stolenToken = registered.ReadRefreshToken()!;
        using var legitimateRefresh = await Client.RefreshAsync();

        Clock.Advance(GracePeriod + TimeSpan.FromSeconds(1));
        using var replay = await CreateCookielessClient().RefreshWithTokenAsync(stolenToken);
        using var legitimateAfterReplay = await Client.RefreshAsync();

        Assert.Equal(HttpStatusCode.OK, legitimateRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, legitimateAfterReplay.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatedTokenReplayed_RevokesEveryOneOfTheUsersSessions()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var stolenToken = registered.ReadRefreshToken()!;
        using var otherDevice = CreateClient();
        await otherDevice.SignInAsync("admin@example.com");
        using var legitimateRefresh = await Client.RefreshAsync();

        Clock.Advance(GracePeriod + TimeSpan.FromSeconds(1));
        using var replay = await CreateCookielessClient().RefreshWithTokenAsync(stolenToken);
        using var otherDeviceRefresh = await otherDevice.RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherDeviceRefresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatedTokenReplayed_LeavesOtherUsersSignedIn()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        using var registered = await CreateClient().RegisterAsync(
            "alice@example.com",
            await Client.CreateInviteCodeAsync(admin.AccessToken));
        var aliceToken = registered.ReadRefreshToken()!;
        using var cookieless = CreateCookielessClient();
        using var aliceRefresh = await cookieless.RefreshWithTokenAsync(aliceToken);

        Clock.Advance(GracePeriod + TimeSpan.FromSeconds(1));
        using var replay = await cookieless.RefreshWithTokenAsync(aliceToken);
        using var adminRefresh = await Client.RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminRefresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatedTokenReplayedWithinGracePeriod_IsRejectedButSessionSurvives()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var previousToken = registered.ReadRefreshToken()!;
        using var firstTab = await Client.RefreshAsync();

        using var secondTab = await CreateCookielessClient().RefreshWithTokenAsync(previousToken);
        using var firstTabAgain = await Client.RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, secondTab.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstTabAgain.StatusCode);
    }

    [Fact]
    public async Task Refresh_SameTokenConcurrently_OnlyOneRotationWins()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var token = registered.ReadRefreshToken()!;

        var statuses = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            using var response = await CreateCookielessClient().RefreshWithTokenAsync(token);
            return response.StatusCode;
        }));

        Assert.Single(statuses, status => status == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_AfterRefreshTokenLifetime_ReturnsUnauthorized()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var token = registered.ReadRefreshToken()!;

        Clock.Advance(TimeSpan.FromDays(14));
        using var response = await CreateCookielessClient().RefreshWithTokenAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RegularlyWithinLifetime_KeepsSessionAliveBeyondOriginalExpiry()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        using var client = CreateCookielessClient();
        var token = registered.ReadRefreshToken()!;

        Clock.Advance(TimeSpan.FromDays(10));
        using var first = await client.RefreshWithTokenAsync(token);
        Clock.Advance(TimeSpan.FromDays(10));
        using var second = await client.RefreshWithTokenAsync(first.ReadRefreshToken()!);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterAbsoluteSessionLifetime_RequiresSigningInAgainEvenWhenActive()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        using var client = CreateCookielessClient();
        var token = registered.ReadRefreshToken()!;

        // Refresh every 10 days, well inside the 14-day sliding window, until the 60-day limit.
        for (var day = 10; day < 60; day += 10)
        {
            Clock.Advance(TimeSpan.FromDays(10));
            using var refresh = await client.RefreshWithTokenAsync(token);
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
            token = refresh.ReadRefreshToken()!;
        }

        Clock.Advance(TimeSpan.FromDays(10));
        using var afterLimit = await client.RefreshWithTokenAsync(token);
        using var signInAgain = await CreateClient().LoginAsync("admin@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, afterLimit.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signInAgain.StatusCode);
    }

    [Fact]
    public async Task Refresh_NearAbsoluteLimit_CookieExpiresAtSessionEnd()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var sessionEnd = Clock.GetUtcNow() + TimeSpan.FromDays(60);

        Clock.Advance(TimeSpan.FromDays(13));
        using var first = await CreateCookielessClient().RefreshWithTokenAsync(registered.ReadRefreshToken()!);
        Clock.Advance(TimeSpan.FromDays(13));
        using var second = await CreateCookielessClient().RefreshWithTokenAsync(first.ReadRefreshToken()!);
        Clock.Advance(TimeSpan.FromDays(13));
        using var third = await CreateCookielessClient().RefreshWithTokenAsync(second.ReadRefreshToken()!);
        Clock.Advance(TimeSpan.FromDays(13));
        using var fourth = await CreateCookielessClient().RefreshWithTokenAsync(third.ReadRefreshToken()!);

        var expires = fourth.FindRefreshCookieHeader()!.Split(';')
            .Select(part => part.Trim())
            .Single(part => part.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))["expires=".Length..];
        Assert.Equal(
            sessionEnd.ToUnixTimeSeconds(),
            DateTimeOffset.Parse(expires, System.Globalization.CultureInfo.InvariantCulture).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorizedAndCookieIsCleared()
    {
        using var registered = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var token = registered.ReadRefreshToken()!;

        using var logout = await Client.LogoutAsync();
        using var refresh = await CreateCookielessClient().RefreshWithTokenAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.NotNull(logout.FindRefreshCookieHeader());
        Assert.Null(logout.ReadRefreshToken());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_OneDevice_LeavesOtherDeviceSignedIn()
    {
        await Client.RegisterAdminAsync("admin@example.com");
        var laptop = CreateClient();
        var phone = CreateClient();
        await laptop.SignInAsync("admin@example.com");
        await phone.SignInAsync("admin@example.com");

        using var logout = await laptop.LogoutAsync();
        using var phoneRefresh = await phone.RefreshAsync();

        Assert.Equal(HttpStatusCode.OK, phoneRefresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_StillReturnsNoContent()
    {
        using var response = await Client.LogoutAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
