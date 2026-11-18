using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

/// <summary>
/// "Already tracked" depends only on the user's own applications, never on what other users submitted, and a
/// refused request leaves the shared posting catalogue untouched.
/// </summary>
public sealed class ApplicationDuplicateTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const string Text = "We are hiring a backend engineer to work on shipment tracking APIs.";
    private const string OtherUrl = "https://careers.example.org/northwind/backend";

    [Fact]
    public async Task Create_LinkPreviouslyMatchedByText_IsStillRecognisedAsTracked()
    {
        // Bob's posting holds Url and Text; Alice's application matches it by text, so her link is not in the catalogue.
        await Client.CreateApplicationOkAsync(Bob, NewApplication(postingText: Text));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(url: OtherUrl, postingText: Text));

        using var lookup = await Client.FindTrackedAsync(Alice, OtherUrl);
        using var again = await Client.CreateApplicationAsync(Alice, NewApplication(url: OtherUrl));

        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(1, await CountRowsAsync("applications.applications WHERE company = 'Northwind Logistics' AND url = 'https://careers.example.org/northwind/backend'"));
    }

    [Fact]
    public async Task Create_LinkAddedLaterByEdit_IsRecognisedAsTracked()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(url: null, postingText: Text));
        using var edited = await Client.UpdateApplicationAsync(Alice, created.Id, new
        {
            company = created.Company,
            role = created.Role,
            url = OtherUrl,
            postingText = created.PostingText,
            channel = created.Channel,
            appliedOn = created.AppliedOn,
            version = created.Version,
        });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

        using var lookup = await Client.FindTrackedAsync(Alice, OtherUrl);
        using var again = await Client.CreateApplicationAsync(Alice, NewApplication(url: OtherUrl));

        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidDate_LeavesNoSharedPosting()
    {
        using var response = await Client.CreateApplicationAsync(
            Alice, NewApplication(postingText: Text, appliedOn: Today.AddDays(5)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("postings.job_postings"));
    }

    [Fact]
    public async Task Create_OwnLinkAgain_IsRefusedWithoutCountingAnotherSubmission()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var again = await Client.CreateApplicationAsync(Alice, NewApplication(url: "https://www.jobs.example.com/4471/"));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings WHERE times_added = 1"));
    }

    [Fact]
    public async Task Update_StaleVersionWithANewLink_LeavesNoSharedPosting()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());
        using var first = await Client.AddEventAsync(Alice, created.Id, new { occurredOn = Today, note = "newer" });

        using var stale = await Client.UpdateApplicationAsync(Alice, created.Id, new
        {
            company = created.Company,
            role = created.Role,
            url = OtherUrl,
            channel = created.Channel,
            appliedOn = created.AppliedOn,
            version = created.Version,
        });

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
    }
}
