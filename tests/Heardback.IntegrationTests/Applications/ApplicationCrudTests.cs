using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationCrudTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task Create_ValidRequest_Returns201WithTheUsersCopyAndASubmissionEvent()
    {
        using var response = await Client.CreateApplicationAsync(
            Alice, NewApplication(postingText: "  Shipment tracking APIs.  ", appliedOn: Today.AddDays(-2)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var application = await response.ReadAsync<ApplicationDto>();
        Assert.Equal($"{ApplicationsApi.BasePath}/{application.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Northwind Logistics", application.Company);
        Assert.Equal(Url, application.Url);
        Assert.Equal("Shipment tracking APIs.", application.PostingText);
        Assert.Equal("LinkedInEasyApply", application.Channel);
        Assert.Equal("Applied", application.Status);
        var submission = Assert.Single(application.Events);
        Assert.True(submission.IsSubmission);
        Assert.Equal(Today.AddDays(-2), submission.OccurredOn);
    }

    [Fact]
    public async Task Create_MissingCompanyAndInvalidLink_Returns400WithDesignedFieldMessages()
    {
        using var response = await Client.CreateApplicationAsync(
            Alice, NewApplication(company: "  ", role: "", url: "northwind-logistics/jobs"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(["Enter the company name"], problem.Errors!["company"]);
        Assert.Equal(["Enter the role"], problem.Errors["role"]);
        Assert.Equal(["Enter a full link that starts with https://"], problem.Errors["url"]);
        Assert.Equal(0, await CountRowsAsync("postings.job_postings"));
    }

    [Fact]
    public async Task Create_MissingChannelAndDate_Returns400()
    {
        using var response = await Client.CreateApplicationAsync(Alice, new { company = "Northwind", role = "Dev" });

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("channel", problem.Errors!.Keys);
        Assert.Contains("appliedOn", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_AppliedTwoDaysAhead_Returns400OnAppliedOn()
    {
        using var response = await Client.CreateApplicationAsync(Alice, NewApplication(appliedOn: Today.AddDays(2)));

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("applications.date_in_future", problem.Code);
        Assert.Contains("appliedOn", problem.Errors!.Keys);
    }

    [Fact]
    public async Task Create_SamePostingTwiceUnderAnotherLinkForm_Returns409AlreadyTracked()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.CreateApplicationAsync(
            Alice, NewApplication(url: "http://www.jobs.example.com/4471/?utm_source=linkedin"));

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("applications.already_tracked", problem.Code);
        Assert.Equal(1, await CountRowsAsync("applications.applications"));
    }

    [Fact]
    public async Task Create_SameTextWithoutLink_Returns409AlreadyTracked()
    {
        const string text = "We are hiring a backend engineer to work on shipment tracking APIs.";
        await Client.CreateApplicationOkAsync(Alice, NewApplication(url: null, postingText: text));

        using var response = await Client.CreateApplicationAsync(Alice, NewApplication(url: null, postingText: text));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_SamePostingByTwoUsers_BothSucceedOnOneSharedPosting()
    {
        var alices = await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind"));
        var bobs = await Client.CreateApplicationOkAsync(Bob, NewApplication(company: "Northwind Logistics Ltd"));

        Assert.NotEqual(alices.Id, bobs.Id);
        Assert.Equal("Northwind", alices.Company);
        Assert.Equal("Northwind Logistics Ltd", bobs.Company);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
    }

    [Fact]
    public async Task Create_DoubleSubmitAtTheSameTime_CreatesOneApplication()
    {
        const int submits = 6;
        var responses = await Task.WhenAll(Enumerable.Range(0, submits)
            .Select(_ => CreateClient().CreateApplicationAsync(Alice, NewApplication())));

        var statuses = responses.Select(response => response.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Created));
        Assert.Equal(submits - 1, statuses.Count(status => status == HttpStatusCode.Conflict));
        Assert.Equal(1, await CountRowsAsync("applications.applications"));
    }

    [Fact]
    public async Task FindTracked_LinkTheUserTracks_ReturnsTheApplication()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.FindTrackedAsync(Alice, "https://www.jobs.example.com/4471#apply");

        var tracked = await response.ReadAsync<TrackedDto>();
        Assert.Equal(created.Id, tracked.Id);
        Assert.Equal("Applied", tracked.Status);
    }

    [Fact]
    public async Task FindTracked_LinkOnlyAnotherUserTracks_Returns204()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.FindTrackedAsync(Bob, Url);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task FindTracked_InvalidLink_Returns400()
    {
        using var response = await Client.FindTrackedAsync(Alice, "not a link");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_OwnApplication_ReturnsItWithTheTimeline()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.GetApplicationAsync(Alice, created.Id);

        var application = await response.ReadAsync<ApplicationDto>();
        Assert.Equal(created.Id, application.Id);
        Assert.Equal(created.Version, application.Version);
        Assert.Single(application.Events);
    }

    [Fact]
    public async Task Get_AnotherUsersApplication_Returns404()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.GetApplicationAsync(Bob, created.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnApplication_RemovesItAndItsTimelineButKeepsThePosting()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.DeleteApplicationAsync(Alice, created.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.applications"));
        Assert.Equal(0, await CountRowsAsync("applications.application_events"));
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
    }

    [Fact]
    public async Task Delete_AnotherUsersApplication_Returns404AndKeepsIt()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.DeleteApplicationAsync(Bob, created.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("applications.applications"));
    }

    [Fact]
    public async Task Endpoints_Anonymous_Return401()
    {
        using var response = await Client.GetAsync(new Uri(ApplicationsApi.BasePath, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
