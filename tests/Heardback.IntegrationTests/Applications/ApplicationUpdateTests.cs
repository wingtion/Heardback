using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationUpdateTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task Update_ValidEdit_SavesDetailsMovesTheSubmissionAndChangesTheVersion()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(appliedOn: Today.AddDays(-2)));

        using var response = await Client.UpdateApplicationAsync(Alice, created.Id, Edit(
            created, company: "Northwind Logistics Ltd", appliedOn: Today.AddDays(-5), notes: " Ask about on-call "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<ApplicationDto>();
        Assert.Equal("Northwind Logistics Ltd", updated.Company);
        Assert.Equal("Ask about on-call", updated.Notes);
        Assert.Equal(Today.AddDays(-5), Assert.Single(updated.Events).OccurredOn);
        Assert.NotEqual(created.Version, updated.Version);
    }

    [Fact]
    public async Task Update_StaleVersion_Returns409ChangedElsewhereAndKeepsTheOtherEdit()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());
        using var first = await Client.UpdateApplicationAsync(Alice, created.Id, Edit(created, notes: "from tab one"));

        using var second = await Client.UpdateApplicationAsync(Alice, created.Id, Edit(created, notes: "from tab two"));

        var problem = await second.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("applications.changed_elsewhere", problem.Code);
        using var current = await Client.GetApplicationAsync(Alice, created.Id);
        Assert.Equal("from tab one", (await current.ReadAsync<ApplicationDto>()).Notes);
    }

    [Fact]
    public async Task Update_TwoEditsAtTheSameTime_OneWinsAndTheOtherGets409()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(index =>
            CreateClient().UpdateApplicationAsync(Alice, created.Id, Edit(created, notes: $"edit {index}"))));
        var statuses = responses.Select(response => response.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(3, statuses.Count(status => status == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Update_NewLink_LinksToAnotherPostingAndLeavesTheOldOneUnchanged()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());
        await Client.CreateApplicationOkAsync(Bob, NewApplication());

        using var response = await Client.UpdateApplicationAsync(
            Alice, created.Id, Edit(created, url: "https://careers.example.org/northwind/backend"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, await CountRowsAsync("postings.job_postings"));
        using var bobsLookup = await Client.FindTrackedAsync(Bob, Url);
        Assert.Equal(HttpStatusCode.OK, bobsLookup.StatusCode);
        using var alicesOldLookup = await Client.FindTrackedAsync(Alice, Url);
        Assert.Equal(HttpStatusCode.NoContent, alicesOldLookup.StatusCode);
    }

    [Fact]
    public async Task Update_LinkOfAnotherOwnApplication_Returns409AlreadyTracked()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication());
        var other = await Client.CreateApplicationOkAsync(
            Alice, NewApplication(company: "Kestrel", url: "https://jobs.example.com/9000"));

        using var response = await Client.UpdateApplicationAsync(Alice, other.Id, Edit(other, url: Url));

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("applications.already_tracked", problem.Code);
    }

    [Fact]
    public async Task Update_OnlyNotesChange_KeepsThePostingAndItsSubmissionCount()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.UpdateApplicationAsync(Alice, created.Id, Edit(created, notes: "Just a note"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
        Assert.Equal(1, await CountRowsAsync("postings.job_postings WHERE times_added = 1"));
    }

    [Fact]
    public async Task Update_AnotherUsersApplication_Returns404AndChangesNothing()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.UpdateApplicationAsync(Bob, created.Id, Edit(created, company: "Hijacked"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var current = await Client.GetApplicationAsync(Alice, created.Id);
        Assert.Equal("Northwind Logistics", (await current.ReadAsync<ApplicationDto>()).Company);
    }

    [Fact]
    public async Task Update_WithoutVersion_Returns400()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.UpdateApplicationAsync(Alice, created.Id, new
        {
            company = "Northwind",
            role = "Dev",
            channel = "Referral",
            appliedOn = Today,
        });

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("version", problem.Errors!.Keys);
    }

    [Fact]
    public async Task Update_AppliedTwoDaysAhead_Returns400OnAppliedOn()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.UpdateApplicationAsync(
            Alice, created.Id, Edit(created, appliedOn: Today.AddDays(2)));

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("appliedOn", problem.Errors!.Keys);
    }

    private static object Edit(
        ApplicationDto current,
        string? company = null,
        string? url = null,
        DateOnly? appliedOn = null,
        string? notes = null) =>
        new
        {
            company = company ?? current.Company,
            role = current.Role,
            url = url ?? current.Url,
            postingText = current.PostingText,
            channel = current.Channel,
            appliedOn = appliedOn ?? current.AppliedOn,
            notes = notes ?? current.Notes,
            version = current.Version,
        };
}
