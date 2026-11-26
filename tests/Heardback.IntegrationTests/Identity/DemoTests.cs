using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;
using Heardback.Shared.Users;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Heardback.IntegrationTests.Identity;

/// <summary>
/// "Try the demo" against real Postgres (ADR-0018): a sandbox with sample data, limited in number and per IP, kept
/// away from everything members own, and removed when its time is up.
/// </summary>
public sealed class DemoTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private const int MaxActiveVisitors = 3;
    private const int VisitorsPerIp = 2;
    private const int SampleApplicationCount = 16;
    private const int NeighborCount = 4;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan NeighborsTimeout = TimeSpan.FromSeconds(30);

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Identity:Demo:Enabled"] = "true";
        settings["Identity:Demo:Lifetime"] = Lifetime.ToString();
        settings["Identity:Demo:MaxActiveVisitors"] = MaxActiveVisitors.ToString(System.Globalization.CultureInfo.InvariantCulture);
        settings["Identity:Demo:VisitorsPerIp"] = VisitorsPerIp.ToString(System.Globalization.CultureInfo.InvariantCulture);
        settings["Identity:Demo:CleanupInterval"] = CleanupInterval.ToString();
    }

    [Fact]
    public async Task StartDemo_Enabled_SignsInADemoVisitorWithSampleData()
    {
        var demo = await Client.StartDemoOkAsync();

        var applications = await Client.ListApplicationsOkAsync(demo.AccessToken, "?pageSize=50");
        using var cv = await Client.SendAsAsync(HttpMethod.Get, "/api/cv", demo.AccessToken);

        Assert.True(demo.User.IsDemo);
        Assert.False(demo.User.IsAdmin);
        Assert.Equal(Clock.GetUtcNow() + Lifetime, demo.User.DemoEndsAt);
        Assert.Equal(SampleApplicationCount, applications.TotalCount);
        Assert.Equal(HttpStatusCode.OK, cv.StatusCode);
    }

    [Fact]
    public async Task StartDemo_Disabled_IsNotFound()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.StartDemoAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartDemo_AllPlacesTaken_IsRefusedUntilTheOldestEnds()
    {
        for (var visitor = 0; visitor < MaxActiveVisitors; visitor++)
        {
            await CreateClient().StartDemoOkAsync(clientIp: $"203.0.113.{visitor + 1}");
        }

        using var refused = await CreateClient().StartDemoAsync(clientIp: "203.0.113.99");
        var problem = await refused.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal("demo.full", problem?["code"]?.GetValue<string>());
        Assert.NotNull(refused.Headers.RetryAfter);
    }

    [Fact]
    public async Task StartDemo_ConcurrentRequestsForTheLastPlaces_NeverExceedTheLimit()
    {
        var attempts = Enumerable.Range(1, MaxActiveVisitors + 3)
            .Select(attempt => CreateClient().StartDemoAsync(clientIp: $"198.51.100.{attempt}"));

        var responses = await Task.WhenAll(attempts);

        Assert.Equal(MaxActiveVisitors, responses.Count(response => response.IsSuccessStatusCode));
        Assert.All(
            responses.Where(response => !response.IsSuccessStatusCode),
            response => Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode));
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task StartDemo_SameIpOverItsLimit_IsRateLimited()
    {
        for (var visitor = 0; visitor < VisitorsPerIp; visitor++)
        {
            await CreateClient().StartDemoOkAsync(clientIp: "203.0.113.7");
        }

        using var limited = await CreateClient().StartDemoAsync(clientIp: "203.0.113.7");
        using var otherIp = await CreateClient().StartDemoAsync(clientIp: "203.0.113.8");

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherIp.StatusCode);
    }

    [Fact]
    public async Task DemoVisitor_AdminPagesAndPasswordChange_AreForbidden()
    {
        var demo = await Client.StartDemoOkAsync();

        using var members = await Client.SendAsAsync(HttpMethod.Get, "/api/admin/members", demo.AccessToken);
        using var invite = await Client.SendAsAsync(HttpMethod.Post, "/api/admin/invite-codes", demo.AccessToken, new { });
        using var password = await Client.SendAsAsync(
            HttpMethod.Post,
            "/api/account/password",
            demo.AccessToken,
            new { currentPassword = "anything-at-all-1", newPassword = "a new password 123" });
        var problem = await password.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, members.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, invite.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, password.StatusCode);
        Assert.Equal("account.demo_no_password", problem?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Login_AsADemoAccount_IsRefused()
    {
        var demo = await Client.StartDemoOkAsync();
        await WaitForNeighborsAsync();

        using var visitor = await CreateClient().LoginAsync(demo.User.Email, string.Empty.PadRight(12, 'x'));
        using var neighbor = await CreateClient().LoginAsync("neighbor-1@demo.invalid", string.Empty.PadRight(12, 'x'));

        Assert.Equal(HttpStatusCode.Unauthorized, visitor.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, neighbor.StatusCode);
    }

    [Fact]
    public async Task Register_WithADemoAddress_IsAValidationError()
    {
        var admin = await Client.RegisterAdminAsync();
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);

        using var response = await CreateClient().RegisterAsync("visitor-1@demo.invalid", code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListMembers_DemoAccountsExist_ShowsOnlyMembers()
    {
        var admin = await Client.RegisterAdminAsync();
        await CreateClient().StartDemoOkAsync();
        await WaitForNeighborsAsync();

        using var response = await Client.SendAsAsync(HttpMethod.Get, "/api/admin/members", admin.AccessToken);
        var page = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, page?["totalCount"]?.GetValue<int>());
    }

    [Fact]
    public async Task Refresh_AfterTheDemoEnded_IsRefused()
    {
        using var browser = CreateClient();
        await browser.StartDemoOkAsync();
        Clock.Advance(Lifetime + TimeSpan.FromMinutes(1));

        using var refresh = await browser.RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Cleanup_AfterTheDemoEnded_RemovesTheVisitorAndAllTheirData()
    {
        var demo = await Client.StartDemoOkAsync();
        await WaitForNeighborsAsync();

        // Passing the interval wakes the cleanup; by then the visitor has expired. The clock keeps moving because
        // the service starts its timer only after the last neighbor's data is in.
        await WaitUntilAsync(async () =>
        {
            Clock.Advance(CleanupInterval);
            return (await ListDemoAccountsAsync()).Visitors.Count == 0;
        });

        using var me = await Client.GetMeAsync(demo.AccessToken);
        var remaining = await ListDemoAccountsAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.applications", demo.User.Id));
        Assert.Equal(0, await CountRowsAsync("matching.cvs", demo.User.Id));
        Assert.Equal(NeighborCount, remaining.Neighbors.Count);
    }

    [Fact]
    public async Task Community_DemoVisitor_SeesTheNeighborsAndNeverMembers()
    {
        var admin = await Client.RegisterAdminAsync();
        var members = new[]
        {
            admin.AccessToken,
            (await CreateClient().RegisterUserAsync(admin.AccessToken, "bob@example.com")).AccessToken,
            (await CreateClient().RegisterUserAsync(admin.AccessToken, "carol@example.com")).AccessToken,
        };
        foreach (var member in members)
        {
            await ApplyToHaldenAsync(member);
        }

        await WaitForNeighborsAsync();
        var demo = await Client.StartDemoOkAsync();

        var visitorsView = await GetPostingNumbersAsync(demo.AccessToken, await FindHaldenAsync(demo.AccessToken));

        // Halden has three neighbors and three members; the visitor counts only the neighbors.
        Assert.Equal(3, visitorsView?["members"]?.GetValue<int>());
    }

    [Fact]
    public async Task Community_Member_NeverSeesDemoAccounts()
    {
        var admin = await Client.RegisterAdminAsync();
        await WaitForNeighborsAsync();
        await CreateClient().StartDemoOkAsync();
        var membersApplication = await ApplyToHaldenAsync(admin.AccessToken);

        var membersView = await GetPostingNumbersAsync(admin.AccessToken, membersApplication);

        // Three neighbors and a visitor applied to Halden too, but none of them are members.
        Assert.Null(membersView);
    }

    private async Task<Guid> ApplyToHaldenAsync(string accessToken)
    {
        using var response = await Client.SendAsAsync(
            HttpMethod.Post,
            "/api/applications",
            accessToken,
            new
            {
                company = "Halden Games",
                role = "Unity Developer",
                url = "https://halden-games.example/jobs/unity",
                channel = "LinkedInEasyApply",
                appliedOn = DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime).AddDays(-20),
            });
        response.EnsureSuccessStatusCode();
        return (await response.ReadAsync<ApplicationDto>()).Id;
    }

    private async Task<Guid> FindHaldenAsync(string accessToken)
    {
        var page = await Client.ListApplicationsOkAsync(accessToken, "?search=Halden");
        return Assert.Single(page.Items).Id;
    }

    private async Task<JsonObject?> GetPostingNumbersAsync(string accessToken, Guid applicationId)
    {
        using var response = await Client.SendAsAsync(
            HttpMethod.Get, $"/api/community/applications/{applicationId}", accessToken);
        response.EnsureSuccessStatusCode();
        var community = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);
        return community?["posting"] as JsonObject;
    }

    private async Task<DemoAccountIds> ListDemoAccountsAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDemoAccounts>().ListAsync(CancellationToken);
    }

    // The neighbors are created by a background service when the API starts.
    private Task WaitForNeighborsAsync() =>
        WaitUntilAsync(async () => (await ListDemoAccountsAsync()).Neighbors.Count == NeighborCount);

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + NeighborsTimeout;
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the demo background service.");
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken);
        }
    }

    private async Task<long> CountRowsAsync(string table, Guid userId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE user_id = @userId", connection);
        command.Parameters.AddWithValue("userId", userId);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }
}
