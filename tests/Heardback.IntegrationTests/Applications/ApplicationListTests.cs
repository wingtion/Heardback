using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationListTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task List_TwoUsers_EachSeesOnlyTheirOwn()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind"));
        await Client.CreateApplicationOkAsync(Bob, NewApplication(company: "Kestrel Health", url: null));

        var alices = await Client.ListApplicationsOkAsync(Alice);
        var bobs = await Client.ListApplicationsOkAsync(Bob);

        Assert.Equal(["Northwind"], alices.Items.Select(item => item.Company));
        Assert.Equal(["Kestrel Health"], bobs.Items.Select(item => item.Company));
    }

    [Fact]
    public async Task List_SeveralApplications_NewestChangeFirstAndPaged()
    {
        foreach (var company in new[] { "First", "Second", "Third" })
        {
            await Client.CreateApplicationOkAsync(Alice, NewApplication(company: company, url: null));
            Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var page = await Client.ListApplicationsOkAsync(Alice, "?page=1&pageSize=2");

        Assert.Equal(["Third", "Second"], page.Items.Select(item => item.Company));
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task List_Search_MatchesCompanyOrRoleIgnoringCase()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind", role: "Backend", url: null));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Kestrel", role: "Frontend", url: null));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Halden", role: "Unity dev", url: null));

        var byCompany = await Client.ListApplicationsOkAsync(Alice, "?search=NORTH");
        var byRole = await Client.ListApplicationsOkAsync(Alice, "?search=end");

        Assert.Equal(["Northwind"], byCompany.Items.Select(item => item.Company));
        Assert.Equal(["Kestrel", "Northwind"], byRole.Items.Select(item => item.Company).Order());
    }

    [Fact]
    public async Task List_SearchWithLikeWildcards_MatchesThemLiterally()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "100% Remote", url: null));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Anything", url: null));

        var results = await Client.ListApplicationsOkAsync(Alice, $"?search={Uri.EscapeDataString("%")}");

        Assert.Equal(["100% Remote"], results.Items.Select(item => item.Company));
    }

    [Fact]
    public async Task List_ChannelFilter_ReturnsOnlyThatChannel()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind", url: null, channel: "Referral"));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Kestrel", url: null, channel: "KariyerNet"));

        var results = await Client.ListApplicationsOkAsync(Alice, "?channel=KariyerNet");

        Assert.Equal(["Kestrel"], results.Items.Select(item => item.Company));
    }

    [Fact]
    public async Task List_NoResponseAndAppliedFilters_SplitAppliedApplicationsByAge()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Old", url: null, appliedOn: Today.AddDays(-14)));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Recent", url: null, appliedOn: Today.AddDays(-13)));

        var noResponse = await Client.ListApplicationsOkAsync(Alice, "?status=NoResponse");
        var applied = await Client.ListApplicationsOkAsync(Alice, "?status=Applied");

        var old = Assert.Single(noResponse.Items);
        Assert.Equal("Old", old.Company);
        Assert.Equal(14, old.NoResponseDays);
        Assert.Equal(["Recent"], applied.Items.Select(item => item.Company));
        Assert.Null(applied.Items[0].NoResponseDays);
    }

    [Fact]
    public async Task List_PageSizeOverTheLimit_Returns400()
    {
        using var response = await Client.ListApplicationsAsync(Alice, "?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_CountsOnlyOwnApplicationsAndThoseWithNoResponse()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Old", url: null, appliedOn: Today.AddDays(-20)));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Recent", url: null));
        await Client.CreateApplicationOkAsync(Bob, NewApplication(company: "Bob's", url: null, appliedOn: Today.AddDays(-30)));

        var summary = await Client.GetSummaryAsync(Alice);

        Assert.Equal(new SummaryDto(2, 1), summary);
    }

    [Fact]
    public async Task Summary_TimePasses_ApplicationBecomesNoResponseWithoutAnyWrite()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(url: null));

        await AdvanceDaysAsync(14);

        Assert.Equal(new SummaryDto(1, 1), await Client.GetSummaryAsync(Alice));
    }
}
