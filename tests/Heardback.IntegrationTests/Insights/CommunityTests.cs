using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Identity;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Insights;

/// <summary>
/// Community numbers through the API, against real Postgres (ADR-0013): nothing is shown below three other members,
/// the viewer is never counted, and no other user's ids or text ever appear.
/// </summary>
public sealed class CommunityTests(PostgresFixture postgres) : ApplicationsTestBase(postgres), IAsyncLifetime
{
    private const string PostingUrl = "https://jobs.example.com/ghost-1";
    private const int SettledDaysAgo = 20;

    private string _carol = string.Empty;
    private string _dave = string.Empty;
    private string _erin = string.Empty;

    public new async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _carol = (await CreateClient().RegisterUserAsync(Alice, "carol@example.com")).AccessToken;
        _dave = (await CreateClient().RegisterUserAsync(Alice, "dave@example.com")).AccessToken;
        _erin = (await CreateClient().RegisterUserAsync(Alice, "erin@example.com")).AccessToken;
    }

    [Fact]
    public async Task ForApplication_TwoOtherMembers_ShowsNoNumbersAtAll()
    {
        var alices = await ApplyAsync(Alice);
        await ApplyAsync(Bob);
        await ApplyAsync(_carol);

        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Null(community.Posting);
        Assert.Null(community.Company);
        Assert.Null(community.Ghost);
    }

    [Fact]
    public async Task ForApplication_ThreeOtherMembers_CountsThemButNeverTheViewer()
    {
        var alices = await ApplyAsync(Alice);
        await AddUpdateAsync(Alice, alices, "Interview", daysAgo: 10);
        await ApplyAsync(Bob);
        await ApplyAsync(_carol);
        await ApplyAsync(_dave);

        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Equal(new NumbersDto(3, 3, 0, 0, null, null), community.Posting);
        Assert.Equal(new NumbersDto(3, 3, 0, 0, null, null), community.Company);
        Assert.NotNull(community.Ghost);
        Assert.Contains(community.Ghost.Reasons, reason => reason.Kind == "NoPostingResponse");
    }

    [Fact]
    public async Task ForApplication_ResponsesOfOthers_GiveRateAndTimes()
    {
        var alices = await ApplyAsync(Alice);
        var bobs = await ApplyAsync(Bob);
        await AddUpdateAsync(Bob, bobs, "Rejected", daysAgo: SettledDaysAgo - 4);
        var carols = await ApplyAsync(_carol);
        await AddUpdateAsync(_carol, carols, "Interview", daysAgo: SettledDaysAgo - 10);
        await ApplyAsync(_dave);

        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Equal(new NumbersDto(3, 3, 2, 67, 7.0, 7.0), community.Posting);
    }

    [Fact]
    public async Task ForApplication_FreshApplicationOfAnotherMember_IsNotCountedYet()
    {
        var alices = await ApplyAsync(Alice);
        await ApplyAsync(Bob);
        await ApplyAsync(_carol);
        await ApplyAsync(_dave, daysAgo: 3);

        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Null(community.Posting);
    }

    [Fact]
    public async Task ForApplication_AnotherMemberDeletes_LeavesTheNumbersAtOnce()
    {
        var alices = await ApplyAsync(Alice);
        await ApplyAsync(Bob);
        await ApplyAsync(_carol);
        var daves = await ApplyAsync(_dave);
        Assert.NotNull((await GetForApplicationAsync(Alice, alices)).Posting);

        using var deleted = await Client.DeleteApplicationAsync(_dave, daves);
        deleted.EnsureSuccessStatusCode();
        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Null(community.Posting);
    }

    [Fact]
    public async Task ForApplication_PostingFirstAddedLongAgo_IsAGhostReason()
    {
        var alices = await ApplyAsync(Alice);
        await ApplyAsync(Bob);
        await ApplyAsync(_carol);
        await ApplyAsync(_dave);

        await AdvanceDaysAsync(31);
        var community = await GetForApplicationAsync(Alice, alices);

        var longListed = Assert.Single(community.Ghost!.Reasons, reason => reason.Kind == "LongListed");
        Assert.Equal("First added here 31 days ago.", longListed.Text);
    }

    [Fact]
    public async Task ForApplication_AnotherUsersApplication_IsNotFound()
    {
        await ApplyAsync(Alice);
        var bobs = await ApplyAsync(Bob);

        using var response = await SendAsync($"/applications/{bobs}", Alice);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ForApplication_Response_HoldsNoIdsOrTextOfOtherMembers()
    {
        var alices = await ApplyAsync(Alice);
        var others = new List<Guid>();
        foreach (var member in new[] { Bob, _carol, _dave })
        {
            var id = await ApplyAsync(member, postingText: "Secret text of another member");
            await AddUpdateAsync(member, id, status: null, daysAgo: 1, note: "Private note of another member");
            others.Add(id);
        }

        using var response = await SendAsync($"/applications/{alices}", Alice);
        var json = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"members\":3", json, StringComparison.Ordinal);
        Assert.All(others, id => Assert.DoesNotContain(id.ToString(), json, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Secret text", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Private note", json, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForCompany_SpellingVariants_FormOneCompany()
    {
        var alices = await ApplyAsync(Alice, company: "Northwind", url: "https://jobs.example.com/a");
        await ApplyAsync(Bob, company: "Northwind Inc.", url: "https://jobs.example.com/b");
        await ApplyAsync(_carol, company: "NORTHWIND", url: "https://jobs.example.com/c");
        await ApplyAsync(_dave, company: "Northwind Ltd", url: "https://jobs.example.com/d");

        var community = await GetForApplicationAsync(Alice, alices);
        var company = await GetForCompanyAsync(Alice, community.CompanyKey!);

        Assert.Null(community.Posting);
        Assert.Equal(3, community.Company!.Members);
        Assert.Equal("northwind", company.CompanyKey);
        Assert.Equal("Northwind", company.Company);
        Assert.Equal(3, company.Numbers!.Members);
        Assert.Equal([alices], company.Applications.Select(application => application.ApplicationId));
    }

    [Fact]
    public async Task ForCompany_OneMemberWithManyApplications_CountsAsOneMember()
    {
        var alices = await ApplyAsync(Alice, url: "https://jobs.example.com/a");
        await ApplyAsync(Bob, url: "https://jobs.example.com/b1");
        await ApplyAsync(Bob, url: "https://jobs.example.com/b2");
        await ApplyAsync(Bob, url: "https://jobs.example.com/b3");
        await ApplyAsync(_carol, url: "https://jobs.example.com/c");

        var community = await GetForApplicationAsync(Alice, alices);

        Assert.Null(community.Company);
    }

    [Fact]
    public async Task ForCompany_RolePostedAgain_IsAGhostReason()
    {
        var alices = await ApplyAsync(Alice, url: "https://jobs.example.com/a");
        await ApplyAsync(Bob, url: "https://jobs.example.com/b");
        await ApplyAsync(_carol, url: "https://jobs.example.com/c");
        await ApplyAsync(_dave, url: "https://jobs.example.com/d");

        var community = await GetForApplicationAsync(Alice, alices);

        var reposted = Assert.Single(community.Ghost!.Reasons, reason => reason.Kind == "RoleReposted");
        Assert.Equal("This role has been added here as 4 separate postings.", reposted.Text);
    }

    [Fact]
    public async Task ForCompany_CompanyOnlyOthersTrack_IsNotFound()
    {
        await ApplyAsync(Alice, company: "Northwind");
        await ApplyAsync(Bob, company: "Contoso", url: "https://jobs.example.com/b");

        using var tracked = await SendAsync("/companies/contoso", Alice);
        using var unknown = await SendAsync("/companies/nobody-has-this", Alice);

        Assert.Equal(HttpStatusCode.NotFound, tracked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(await WithoutTraceIdAsync(tracked), await WithoutTraceIdAsync(unknown));
    }

    [Fact]
    public async Task Badges_OnlyMediumAndHigh_ForTheViewersApplications()
    {
        var ghosted = await ApplyAsync(Alice);
        var quiet = await ApplyAsync(Alice, company: "Fabrikam", url: "https://jobs.example.com/quiet");
        foreach (var member in new[] { Bob, _carol, _dave })
        {
            await ApplyAsync(member);
        }

        var badges = await GetBadgesAsync(Alice);
        var erinsBadges = await GetBadgesAsync(_erin);

        var badge = Assert.Single(badges.Items);
        Assert.Equal(ghosted, badge.ApplicationId);
        Assert.Contains(badge.Level, (string[])["Medium", "High"]);
        Assert.DoesNotContain(badges.Items, item => item.ApplicationId == quiet);
        Assert.Equal(0, erinsBadges.TotalCount);
    }

    [Fact]
    public async Task Routes_Anonymous_AreUnauthorized()
    {
        using var application = await SendAsync($"/applications/{Guid.NewGuid()}", accessToken: null);
        using var badges = await SendAsync("/badges", accessToken: null);
        using var company = await SendAsync("/companies/northwind", accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, application.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, badges.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, company.StatusCode);
    }

    [Fact]
    public async Task Badges_InvalidPage_IsValidationError()
    {
        using var response = await SendAsync("/badges?pageSize=0", Alice);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> ApplyAsync(
        string accessToken,
        string company = "Northwind Logistics",
        string? url = PostingUrl,
        string? postingText = null,
        int daysAgo = SettledDaysAgo) =>
        (await Client.CreateApplicationOkAsync(
            accessToken,
            NewApplication(company: company, url: url, postingText: postingText, appliedOn: Today.AddDays(-daysAgo)))).Id;

    private async Task AddUpdateAsync(string accessToken, Guid id, string? status, int daysAgo, string? note = null)
    {
        using var response = await Client.AddEventAsync(
            accessToken, id, new { status, occurredOn = Today.AddDays(-daysAgo), note });
        response.EnsureSuccessStatusCode();
    }

    private async Task<ApplicationCommunityDto> GetForApplicationAsync(string accessToken, Guid id)
    {
        using var response = await SendAsync($"/applications/{id}", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ApplicationCommunityDto>();
    }

    private async Task<CompanyDto> GetForCompanyAsync(string accessToken, string key)
    {
        using var response = await SendAsync($"/companies/{Uri.EscapeDataString(key)}", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<CompanyDto>();
    }

    private async Task<BadgePageDto> GetBadgesAsync(string accessToken)
    {
        using var response = await SendAsync("/badges", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<BadgePageDto>();
    }

    // The trace id differs per request; everything else must be identical, so the answer cannot be used to probe.
    private static async Task<string> WithoutTraceIdAsync(HttpResponseMessage response)
    {
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync(CancellationToken))!.AsObject();
        problem.Remove("traceId");
        return problem.ToJsonString();
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/community" + path, UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await Client.SendAsync(request, CancellationToken);
    }
}

internal sealed record NumbersDto(
    int Members,
    int Applications,
    int Responded,
    int ResponseRatePercent,
    double? AverageDays,
    double? MedianDays);

internal sealed record GhostReasonDto(string Kind, int Points, string Text);

internal sealed record GhostDto(int Score, int MaxScore, string Level, GhostReasonDto[] Reasons);

internal sealed record ApplicationCommunityDto(
    Guid ApplicationId,
    string? CompanyKey,
    NumbersDto? Posting,
    NumbersDto? Company,
    GhostDto? Ghost);

internal sealed record CompanyApplicationDto(
    Guid ApplicationId,
    string Role,
    string Status,
    DateOnly AppliedOn,
    int? NoResponseDays,
    string? Ghost);

internal sealed record CompanyDto(
    string CompanyKey,
    string Company,
    NumbersDto? Numbers,
    CompanyApplicationDto[] Applications);

internal sealed record BadgeDto(Guid ApplicationId, string Level, int Score);

internal sealed record BadgePageDto(BadgeDto[] Items, int Page, int PageSize, int TotalCount);
