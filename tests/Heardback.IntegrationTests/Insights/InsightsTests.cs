using System.Net;
using System.Net.Http.Headers;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Insights;

/// <summary>The dashboard and follow-ups through the API, against real Postgres.</summary>
public sealed class InsightsTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task Dashboard_NoApplications_IsEmptyButComplete()
    {
        var dashboard = await GetDashboardAsync(Alice);

        Assert.Equal(0, dashboard.TotalApplications);
        Assert.Null(dashboard.ResponseRatePercent);
        Assert.Equal(12, dashboard.Weeks.Length);
        Assert.Empty(dashboard.Channels);
        Assert.Null(dashboard.ResponseTime.AverageDays);
    }

    [Fact]
    public async Task Dashboard_RealTimelines_GiveTheNumbers()
    {
        var interviewed = await CreateAsync(Alice, "Northwind", "LinkedInEasyApply", daysAgo: 10);
        await AddUpdateAsync(Alice, interviewed, "Interview", daysAgo: 6);
        await AddUpdateAsync(Alice, interviewed, "Rejected", daysAgo: 2);
        var rejected = await CreateAsync(Alice, "Contoso", "LinkedInEasyApply", daysAgo: 8);
        await AddUpdateAsync(Alice, rejected, "Rejected", daysAgo: 6);
        var noted = await CreateAsync(Alice, "Fabrikam", "Referral", daysAgo: 4);
        await AddUpdateAsync(Alice, noted, status: null, daysAgo: 1, note: "Sent a follow-up email");

        var dashboard = await GetDashboardAsync(Alice);

        Assert.Equal(3, dashboard.TotalApplications);
        Assert.Equal(2, dashboard.Responded);
        Assert.Equal(67, dashboard.ResponseRatePercent);
        Assert.Equal(3, dashboard.Weeks.Sum(week => week.Applications));
        Assert.Equal(
            [new ChannelDto("LinkedInEasyApply", 2, 2, 100), new ChannelDto("Referral", 1, 0, 0)],
            dashboard.Channels);
        Assert.Equal(new ResponseTimeDto(2, 3.0, 3.0), dashboard.ResponseTime);
        Assert.Equal(new FunnelDto(3, 1, 0, 2), dashboard.Funnel);
    }

    [Fact]
    public async Task DashboardAndFollowUps_AnotherUsersApplications_AreNeverCounted()
    {
        var bobs = await CreateAsync(Bob, "Northwind", "Referral", daysAgo: 30);
        await AddUpdateAsync(Bob, bobs, "Offer", daysAgo: 5);
        await CreateAsync(Bob, "Contoso", "Referral", daysAgo: 30);

        var dashboard = await GetDashboardAsync(Alice);
        var followUps = await GetFollowUpsAsync(Alice);

        Assert.Equal(0, dashboard.TotalApplications);
        Assert.Equal(0, followUps.TotalCount);
        Assert.Equal(2, (await GetDashboardAsync(Bob)).TotalApplications);
        Assert.Equal(1, (await GetFollowUpsAsync(Bob)).TotalCount);
    }

    [Fact]
    public async Task FollowUps_OnlyApplicationsWithNoResponse_LongestSilenceFirst()
    {
        await CreateAsync(Alice, "Recent", "CompanySite", daysAgo: 3);
        await CreateAsync(Alice, "Silent twenty", "CompanySite", daysAgo: 20);
        await CreateAsync(Alice, "Silent thirty", "Referral", daysAgo: 30);
        var answered = await CreateAsync(Alice, "Answered", "CompanySite", daysAgo: 40);
        await AddUpdateAsync(Alice, answered, "Rejected", daysAgo: 35);

        var followUps = await GetFollowUpsAsync(Alice);

        Assert.Equal(["Silent thirty", "Silent twenty"], followUps.Items.Select(item => item.Company));
        Assert.Equal([30, 20], followUps.Items.Select(item => item.NoResponseDays));
    }

    [Fact]
    public async Task FollowUps_AfterANote_LeavesTheList()
    {
        var silent = await CreateAsync(Alice, "Silent", "CompanySite", daysAgo: 20);

        await AddUpdateAsync(Alice, silent, status: null, daysAgo: 0, note: "Followed up by email");
        var followUps = await GetFollowUpsAsync(Alice);

        Assert.Equal(0, followUps.TotalCount);
    }

    [Fact]
    public async Task FollowUps_Paged_ReturnsThePageAndTheTotal()
    {
        for (var index = 0; index < 3; index++)
        {
            await CreateAsync(Alice, $"Silent {index}", "CompanySite", daysAgo: 20 + index);
        }

        var second = await GetFollowUpsAsync(Alice, "?page=2&pageSize=2");

        Assert.Equal(3, second.TotalCount);
        Assert.Equal(["Silent 0"], second.Items.Select(item => item.Company));
    }

    [Fact]
    public async Task FollowUps_InvalidPage_IsValidationError()
    {
        using var response = await SendAsync("/follow-ups?page=0", Alice);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DashboardAndFollowUps_Anonymous_AreUnauthorized()
    {
        using var dashboard = await SendAsync("/dashboard", accessToken: null);
        using var followUps = await SendAsync("/follow-ups", accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, followUps.StatusCode);
    }

    private async Task<Guid> CreateAsync(string accessToken, string company, string channel, int daysAgo) =>
        (await Client.CreateApplicationOkAsync(
            accessToken,
            NewApplication(
                company: company,
                url: null,
                postingText: $"Posting at {company}",
                channel: channel,
                appliedOn: Today.AddDays(-daysAgo)))).Id;

    private async Task AddUpdateAsync(string accessToken, Guid id, string? status, int daysAgo, string? note = null)
    {
        using var response = await Client.AddEventAsync(
            accessToken, id, new { status, occurredOn = Today.AddDays(-daysAgo), note });
        response.EnsureSuccessStatusCode();
    }

    private async Task<DashboardDto> GetDashboardAsync(string accessToken)
    {
        using var response = await SendAsync("/dashboard", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<DashboardDto>();
    }

    private async Task<FollowUpPageDto> GetFollowUpsAsync(string accessToken, string query = "")
    {
        using var response = await SendAsync("/follow-ups" + query, accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<FollowUpPageDto>();
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/insights" + path, UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await Client.SendAsync(request, CancellationToken);
    }
}

internal sealed record DashboardDto(
    int TotalApplications,
    int Responded,
    int? ResponseRatePercent,
    WeekDto[] Weeks,
    ChannelDto[] Channels,
    ResponseTimeDto ResponseTime,
    FunnelDto Funnel);

internal sealed record WeekDto(DateOnly WeekStart, int Applications);

internal sealed record ChannelDto(string Channel, int Applications, int Responded, int ResponseRatePercent);

internal sealed record ResponseTimeDto(int Responded, double? AverageDays, double? MedianDays);

internal sealed record FunnelDto(int Applied, int Interview, int Offer, int Rejected);

internal sealed record FollowUpDto(
    Guid ApplicationId,
    string Company,
    string Role,
    string Channel,
    DateOnly AppliedOn,
    DateOnly LastActivityOn,
    int NoResponseDays);

internal sealed record FollowUpPageDto(FollowUpDto[] Items, int Page, int PageSize, int TotalCount);
