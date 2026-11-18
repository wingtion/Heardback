using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationTimelineTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task AddEvent_NewStatusWithNote_ChangesStatusAndPutsTheEventOnTop()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(appliedOn: Today.AddDays(-3)));

        using var response = await Client.AddEventAsync(
            Alice, created.Id, new { status = "Interview", occurredOn = Today, note = "Recruiter call" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<ApplicationDto>();
        Assert.Equal("Interview", updated.Status);
        Assert.Equal(2, updated.Events.Count);
        Assert.Equal(new ApplicationEventDto(updated.Events[0].Id, "Interview", Today, "Recruiter call", false), updated.Events[0]);
        Assert.True(updated.Events[1].IsSubmission);
    }

    [Fact]
    public async Task AddEvent_NoteOnly_KeepsStatusAndClearsNoResponse()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(appliedOn: Today.AddDays(-20)));
        Assert.Equal(20, created.NoResponseDays);

        using var response = await Client.AddEventAsync(
            Alice, created.Id, new { occurredOn = Today, note = "Followed up by email" });

        var updated = await response.ReadAsync<ApplicationDto>();
        Assert.Equal("Applied", updated.Status);
        Assert.Null(updated.NoResponseDays);
        Assert.Null(updated.Events[0].Status);
    }

    [Fact]
    public async Task AddEvent_BoardMoveWithoutNote_ChangesStatus()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(Alice, created.Id, new { status = "Rejected", occurredOn = Today });

        Assert.Equal("Rejected", (await response.ReadAsync<ApplicationDto>()).Status);
    }

    [Fact]
    public async Task AddEvent_NothingChosen_Returns400EmptyUpdate()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(Alice, created.Id, new { occurredOn = Today, note = "  " });

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("applications.empty_update", problem.Code);
    }

    [Fact]
    public async Task AddEvent_CurrentStatus_Returns400SameStatus()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(Alice, created.Id, new { status = "Applied", occurredOn = Today });

        Assert.Equal("applications.same_status", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task AddEvent_DateTwoDaysAhead_Returns400OnOccurredOn()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(
            Alice, created.Id, new { status = "Offer", occurredOn = Today.AddDays(2) });

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("occurredOn", problem.Errors!.Keys);
    }

    [Fact]
    public async Task AddEvent_NoteLongerThan280_Returns400()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(
            Alice, created.Id, new { occurredOn = Today, note = new string('a', 281) });

        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("note", problem.Errors!.Keys);
    }

    [Fact]
    public async Task AddEvent_AnotherUsersApplication_Returns404AndAddsNothing()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(Bob, created.Id, new { status = "Offer", occurredOn = Today });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("applications.application_events"));
    }

    [Fact]
    public async Task AddEvent_ThenEditWithTheOldVersion_Returns409ChangedElsewhere()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());
        using var moved = await Client.AddEventAsync(Alice, created.Id, new { status = "Interview", occurredOn = Today });

        using var response = await Client.UpdateApplicationAsync(Alice, created.Id, new
        {
            company = created.Company,
            role = created.Role,
            url = created.Url,
            channel = created.Channel,
            appliedOn = created.AppliedOn,
            notes = "stale form",
            version = created.Version,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddEvent_SeveralAtTheSameTime_AllAreRecordedAndStatusMatchesTheTimeline()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(appliedOn: Today.AddDays(-10)));
        var updates = new[]
        {
            new { status = (string?)"Interview", occurredOn = Today.AddDays(-5), note = (string?)null },
            new { status = (string?)"Offer", occurredOn = Today, note = (string?)null },
            new { status = (string?)null, occurredOn = Today.AddDays(-1), note = (string?)"A note" },
        };

        var responses = await Task.WhenAll(updates.Select(update =>
            CreateClient().AddEventAsync(Alice, created.Id, update)));
        var statuses = responses.Select(response => response.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.OK, status));
        using var current = await Client.GetApplicationAsync(Alice, created.Id);
        var application = await current.ReadAsync<ApplicationDto>();
        Assert.Equal(4, application.Events.Count);
        Assert.Equal("Offer", application.Status);
    }
}
