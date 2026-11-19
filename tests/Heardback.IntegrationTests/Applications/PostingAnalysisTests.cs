using System.Net;
using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.IntegrationTests.Llm;
using Heardback.IntegrationTests.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Posting analysis through the API, with a canned LLM and the job worker driven by the test.</summary>
public sealed class PostingAnalysisTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const string PostingText = """
        Northwind is hiring a Senior Backend Engineer (.NET, PostgreSQL, Kubernetes). Hybrid, Istanbul.
        Questions? Write to hiring.manager@northwind.example or call +90 532 123 45 67.
        """;

    private const string ValidAnswer = """
        {"company":"Northwind","title":"Senior Backend Engineer","seniority":"Senior","workMode":"Hybrid",
         "skills":[".NET","PostgreSQL","Kubernetes",".net"],"location":"Istanbul"}
        """;

    private readonly CannedLlmClient _llm = new() { Respond = _ => CannedLlmClient.Answer(ValidAnswer) };

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Llm:Jobs:RunWorker"] = "false";
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILlmClient>(_llm);
    }

    [Fact]
    public async Task Get_NoPostingText_SaysSo()
    {
        var application = await CreateAsync(Alice, postingText: null);

        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("NoPostingText", analysis.State);
    }

    [Fact]
    public async Task Request_NoPostingText_IsConflict()
    {
        var application = await CreateAsync(Alice, postingText: null);

        using var response = await Client.RequestPostingAnalysisAsync(Alice, application);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("llm.analysis_jobs"));
    }

    [Fact]
    public async Task Get_NeverRequested_IsNotRequestedAndCallsNoLlm()
    {
        var application = await CreateAsync(Alice);

        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("NotRequested", analysis.State);
        Assert.Equal(0, await CountRowsAsync("llm.analysis_jobs"));
        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public async Task Request_ThenJobRuns_AnalysisIsReady()
    {
        var application = await CreateAsync(Alice);

        var requested = await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();
        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Queued", requested.State);
        Assert.Equal("Ready", analysis.State);
        Assert.NotNull(analysis.Analysis);
        Assert.Equal("Northwind", analysis.Analysis.Company);
        Assert.Equal("Senior Backend Engineer", analysis.Analysis.Title);
        Assert.Equal("Senior", analysis.Analysis.Seniority);
        Assert.Equal("Hybrid", analysis.Analysis.WorkMode);
        Assert.Equal([".NET", "PostgreSQL", "Kubernetes"], analysis.Analysis.Skills);
        Assert.Equal("Istanbul", analysis.Analysis.Location);
        Assert.Equal(Clock.GetUtcNow(), analysis.Analysis.AnalyzedAt, TimeSpan.FromMilliseconds(1)); // µs in Postgres
    }

    [Fact]
    public async Task Request_SendsPublicPostingTextWithoutPersonalDetails()
    {
        var application = await CreateAsync(Alice);

        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();

        var request = Assert.Single(_llm.Requests);
        Assert.Equal("posting-analysis", request.PromptId);
        Assert.Equal(1, request.PromptVersion);
        Assert.Equal(LlmDataClass.Public, request.DataClass);
        Assert.StartsWith("<posting>\nCompany: Northwind Logistics\nRole: Backend Engineer (.NET)\n", request.Input, StringComparison.Ordinal);
        Assert.Contains("Senior Backend Engineer (.NET, PostgreSQL, Kubernetes)", request.Input, StringComparison.Ordinal);
        Assert.Contains("[email]", request.Input, StringComparison.Ordinal);
        Assert.Contains("[phone]", request.Input, StringComparison.Ordinal);
        Assert.DoesNotContain("hiring.manager@", request.Input, StringComparison.Ordinal);
        Assert.DoesNotContain("532 123", request.Input, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_Twice_QueuesOneJob()
    {
        var application = await CreateAsync(Alice);

        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        var second = await Client.RequestPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Queued", second.State);
        Assert.Equal(1, await CountRowsAsync("llm.analysis_jobs"));
    }

    [Fact]
    public async Task Request_AlreadyAnalysed_IsReadyWithoutNewJob()
    {
        var application = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();

        var again = await Client.RequestPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Ready", again.State);
        Assert.Equal(1, await CountRowsAsync("llm.analysis_jobs"));
        Assert.Single(_llm.Requests);
    }

    [Fact]
    public async Task Get_SameTextAnalysedForAnotherUser_IsReadyWithoutAsking()
    {
        var alices = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, alices);
        await RunJobsAsync();

        var bobs = await CreateAsync(Bob, url: null);
        var analysis = await Client.GetPostingAnalysisOkAsync(Bob, bobs);

        Assert.Equal("Ready", analysis.State);
        Assert.Single(_llm.Requests);
    }

    [Fact]
    public async Task Get_DifferentTextFromAnotherUser_IsNotShared()
    {
        var alices = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, alices);
        await RunJobsAsync();

        var bobs = await CreateAsync(Bob, url: null, postingText: PostingText + "\nMy own note: ask about the bonus.");
        var analysis = await Client.GetPostingAnalysisOkAsync(Bob, bobs);

        Assert.Equal("NotRequested", analysis.State);
        Assert.Null(analysis.Analysis);
    }

    [Fact]
    public async Task GetAndRequest_AnotherUsersApplication_AreNotFound()
    {
        var alices = await CreateAsync(Alice);

        using var get = await Client.GetPostingAnalysisAsync(Bob, alices);
        using var request = await Client.RequestPostingAnalysisAsync(Bob, alices);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, request.StatusCode);
        Assert.Equal(0, await CountRowsAsync("llm.analysis_jobs"));
    }

    [Fact]
    public async Task GetAndRequest_Anonymous_AreUnauthorized()
    {
        var application = await CreateAsync(Alice);

        using var get = await Client.GetPostingAnalysisAsync(accessToken: null, application);
        using var request = await Client.RequestPostingAnalysisAsync(accessToken: null, application);

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, request.StatusCode);
    }

    [Fact]
    public async Task Get_AfterPostingTextEdited_IsNotRequestedForTheNewText()
    {
        var application = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();

        await EditPostingTextAsync(Alice, application, "Junior frontend developer, Angular, remote.");
        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("NotRequested", analysis.State);
    }

    [Fact]
    public async Task Request_InvalidAnswerTwice_IsFailedAndCanBeRequestedAgain()
    {
        _llm.Respond = _ => CannedLlmClient.Answer("""{"company":"Northwind"}""");
        var application = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();

        var failed = await Client.GetPostingAnalysisOkAsync(Alice, application);
        _llm.Respond = _ => CannedLlmClient.Answer(ValidAnswer);
        var again = await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();
        var ready = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Failed", failed.State);
        Assert.Equal(3, _llm.Requests.Count); // the invalid answer and its one retry, then the new request
        Assert.Equal("Queued", again.State);
        Assert.Equal("Ready", ready.State);
    }

    [Fact]
    public async Task Request_ProvidersUnavailable_StaysQueuedForRetry()
    {
        _llm.Respond = _ => LlmResult.Unavailable(TimeSpan.FromSeconds(20));
        var application = await CreateAsync(Alice);

        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        await RunJobsAsync();
        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Queued", analysis.State);
        Assert.Single(_llm.Requests);
    }

    [Fact]
    public async Task Job_ApplicationDeletedBeforeItRuns_EndsWithoutCallingTheLlm()
    {
        var application = await CreateAsync(Alice);
        await Client.RequestPostingAnalysisOkAsync(Alice, application);
        using (var deleted = await Client.DeleteApplicationAsync(Alice, application))
        {
            deleted.EnsureSuccessStatusCode();
        }

        await RunJobsAsync();

        Assert.Empty(_llm.Requests);
        Assert.Equal(0, await CountRowsAsync("applications.posting_analyses"));
        Assert.Equal(1, await CountRowsAsync("llm.analysis_jobs WHERE status = 'Succeeded'"));
    }

    private async Task<Guid> CreateAsync(string accessToken, string? postingText = PostingText, string? url = Url) =>
        (await Client.CreateApplicationOkAsync(accessToken, NewApplication(url: url, postingText: postingText))).Id;

    private async Task EditPostingTextAsync(string accessToken, Guid id, string postingText)
    {
        using var read = await Client.GetApplicationAsync(accessToken, id);
        var current = await read.ReadAsync<ApplicationDto>();
        using var response = await Client.UpdateApplicationAsync(accessToken, id, new
        {
            company = current.Company,
            role = current.Role,
            url = current.Url,
            postingText,
            channel = current.Channel,
            appliedOn = current.AppliedOn,
            notes = (string?)null,
            version = current.Version,
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task RunJobsAsync()
    {
        bool ran;
        do
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            ran = await scope.ServiceProvider.GetRequiredService<AnalysisJobRunner>().RunNextAsync(CancellationToken);
        }
        while (ran);
    }
}
