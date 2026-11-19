using System.Net;
using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Llm;
using Heardback.IntegrationTests.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.IntegrationTests.Matching;

/// <summary>CV tailoring through the API, with a canned LLM and the job worker driven by the test.</summary>
public sealed class TailoringTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const int DailyLimit = 2;

    private const string PostingText = """
        We build shipment APIs with C#, .NET and PostgreSQL, deployed to Kubernetes on Azure.
        Questions: talent@northwind.example
        """;

    private const string Answer = """
        {"summary":"Strong .NET and PostgreSQL match; Kubernetes and Azure are missing.",
         "suggestions":[
           {"cvSection":"Experience","cvQuote":"ASP.NET Core, PostgreSQL","advice":"Move the PostgreSQL work to the top."},
           {"cvSection":"Skills","cvQuote":"Skills: C#","advice":"List C# before the other languages."},
           {"cvSection":"Projects","cvQuote":"Ada Lovelace","advice":"Say which .NET version the tracking service used."}]}
        """;

    private readonly CannedLlmClient _llm = new() { Respond = _ => CannedLlmClient.Answer(Answer) };

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Llm:Jobs:RunWorker"] = "false";
        settings["Matching:TailoringPerDay"] = DailyLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILlmClient>(_llm);
    }

    [Fact]
    public async Task Consent_ByDefault_IsNotGiven()
    {
        var consent = await Client.GetAiConsentAsync(Alice);

        Assert.False(consent.Allowed);
        Assert.Null(consent.AllowedAt);
    }

    [Fact]
    public async Task Consent_Allowed_IsRememberedWithTheTime()
    {
        var allowed = await Client.SetAiConsentAsync(Alice, allowed: true);
        await Client.SetAiConsentAsync(Alice, allowed: true);

        Assert.True(allowed.Allowed);
        Assert.NotNull(allowed.AllowedAt);
        Assert.True((await Client.GetAiConsentAsync(Alice)).Allowed);
        Assert.False((await Client.GetAiConsentAsync(Bob)).Allowed);
    }

    [Fact]
    public async Task Get_NoCv_SaysSoAndStillListsMissingKeywords()
    {
        var application = await CreateAsync(Alice);

        var tailoring = await Client.GetTailoringOkAsync(Alice, application);

        Assert.Equal("NoCv", tailoring.State);
        Assert.Contains("Kubernetes", tailoring.MissingKeywords);
    }

    [Fact]
    public async Task Get_NoPostingText_SaysSo()
    {
        await UploadAsync(Alice);
        var application = await CreateAsync(Alice, postingText: null);

        Assert.Equal("NoPostingText", (await Client.GetTailoringOkAsync(Alice, application)).State);
    }

    [Fact]
    public async Task GetAndRequest_WithoutConsent_AskForIt()
    {
        await UploadAsync(Alice);
        var application = await CreateAsync(Alice);

        var tailoring = await Client.GetTailoringOkAsync(Alice, application);
        using var request = await Client.RequestTailoringAsync(Alice, application);

        Assert.Equal("ConsentRequired", tailoring.State);
        Assert.Equal(HttpStatusCode.Conflict, request.StatusCode);
        Assert.Equal(0, await CountRowsAsync("llm.analysis_jobs"));
    }

    [Fact]
    public async Task Request_ThenJobRuns_SuggestionsAreReady()
    {
        var application = await ReadyToTailorAsync(Alice);

        var requested = await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();
        var tailoring = await Client.GetTailoringOkAsync(Alice, application);

        Assert.Equal("Queued", requested.State);
        Assert.Equal(1, requested.Usage.Used);
        Assert.Equal("Ready", tailoring.State);
        Assert.NotNull(tailoring.Tailoring);
        Assert.False(tailoring.Tailoring.IsOutdated);
        Assert.StartsWith("Strong .NET", tailoring.Tailoring.Summary, StringComparison.Ordinal);
        Assert.Equal(["Experience", "Skills", "Projects"], tailoring.Tailoring.Suggestions.Select(advice => advice.CvSection));
        Assert.Equal(["Kubernetes", "Azure"], tailoring.MissingKeywords);
    }

    [Fact]
    public async Task Request_SendsCvAsPersonalDataWithTheMatchAndWithoutPostingContacts()
    {
        var application = await ReadyToTailorAsync(Alice);

        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        var request = Assert.Single(_llm.Requests);
        Assert.Equal("cv-tailoring", request.PromptId);
        Assert.Equal(LlmDataClass.Personal, request.DataClass);
        Assert.Contains("<cv>\nAda Lovelace", request.Input, StringComparison.Ordinal);
        Assert.Contains("ALICE-CV-MARKER", request.Input, StringComparison.Ordinal);
        Assert.Contains("<posting>\nCompany: Northwind Logistics", request.Input, StringComparison.Ordinal);
        Assert.Contains("[email]", request.Input, StringComparison.Ordinal);
        Assert.DoesNotContain("talent@", request.Input, StringComparison.Ordinal);
        Assert.Contains("CV does not show (keyword match): Kubernetes, Azure", request.Input, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Job_RunsAsTheUserWhoAsked_ReadsOnlyTheirCv()
    {
        var alices = await ReadyToTailorAsync(Alice);
        await ReadyToTailorAsync(Bob, cvMarker: "BOB-CV-MARKER");

        await Client.RequestTailoringOkAsync(Alice, alices);
        await RunJobsAsync();

        var request = Assert.Single(_llm.Requests);
        Assert.Contains("ALICE-CV-MARKER", request.Input, StringComparison.Ordinal);
        Assert.DoesNotContain("BOB-CV-MARKER", request.Input, StringComparison.Ordinal);
        Assert.Equal("NotRequested", (await Client.GetTailoringOkAsync(Bob, await CreateAsync(Bob, url: null, postingText: PostingText + " Bob"))).State);
    }

    [Fact]
    public async Task Request_CurrentSuggestions_ReturnsThemWithoutNewJob()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        var again = await Client.RequestTailoringOkAsync(Alice, application);

        Assert.Equal("Ready", again.State);
        Assert.Equal(1, again.Usage.Used);
        Assert.Single(_llm.Requests);
    }

    [Fact]
    public async Task Get_AfterNewCv_IsOutdatedAndCanBeMadeAgain()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        await UploadAsync(Alice, cvMarker: "NEW-CV");
        var outdated = await Client.GetTailoringOkAsync(Alice, application);
        var remade = await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();
        var current = await Client.GetTailoringOkAsync(Alice, application);

        Assert.True(outdated.Tailoring!.IsOutdated);
        Assert.Equal("Queued", remade.State);
        Assert.NotNull(remade.Tailoring);
        Assert.True(remade.Tailoring.IsOutdated);
        Assert.False(current.Tailoring!.IsOutdated);
        Assert.Equal(2, _llm.Requests.Count);
        Assert.Equal(1, await CountRowsAsync("matching.tailoring_results"));
    }

    [Fact]
    public async Task Get_AfterPostingTextEdited_IsOutdated()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        await EditPostingTextAsync(Alice, application, PostingText + "\nAlso Terraform.");

        Assert.True((await Client.GetTailoringOkAsync(Alice, application)).Tailoring!.IsOutdated);
    }

    [Fact]
    public async Task WithdrawConsent_DeletesSuggestions()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        var withdrawn = await Client.SetAiConsentAsync(Alice, allowed: false);

        Assert.False(withdrawn.Allowed);
        Assert.Equal(0, await CountRowsAsync("matching.tailoring_results"));
        Assert.Equal("ConsentRequired", (await Client.GetTailoringOkAsync(Alice, application)).State);
    }

    [Fact]
    public async Task WithdrawConsent_WhileQueued_JobSendsNothingAndGivesTheUseBack()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);

        await Client.SetAiConsentAsync(Alice, allowed: false);
        await RunJobsAsync();

        Assert.Empty(_llm.Requests);
        Assert.Equal(0, (await Client.GetTailoringOkAsync(Alice, application)).Usage.Used);
    }

    [Fact]
    public async Task DeleteCv_DeletesSuggestions()
    {
        var application = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        using var deleted = await Client.DeleteCvAsync(Alice);

        deleted.EnsureSuccessStatusCode();
        Assert.Equal(0, await CountRowsAsync("matching.tailoring_results"));
        Assert.Equal("NoCv", (await Client.GetTailoringOkAsync(Alice, application)).State);
    }

    [Fact]
    public async Task DeleteApplication_DeletesItsSuggestionsOnly()
    {
        var first = await ReadyToTailorAsync(Alice);
        var second = await CreateAsync(Alice, url: null, postingText: PostingText + "\nSecond posting");
        await Client.RequestTailoringOkAsync(Alice, first);
        await Client.RequestTailoringOkAsync(Alice, second);
        await RunJobsAsync();

        using var deleted = await Client.DeleteApplicationAsync(Alice, first);

        deleted.EnsureSuccessStatusCode();
        Assert.Equal(1, await CountRowsAsync("matching.tailoring_results"));
        Assert.Equal("Ready", (await Client.GetTailoringOkAsync(Alice, second)).State);
    }

    [Fact]
    public async Task Request_DailyLimitUsedUp_Is429()
    {
        await UploadAsync(Alice);
        await Client.SetAiConsentAsync(Alice, allowed: true);
        for (var index = 0; index < DailyLimit; index++)
        {
            var queued = await CreateAsync(Alice, url: null, postingText: $"{PostingText}\nPosting {index}");
            await Client.RequestTailoringOkAsync(Alice, queued);
        }

        var over = await CreateAsync(Alice, url: null, postingText: $"{PostingText}\nOne more");
        using var response = await Client.RequestTailoringAsync(Alice, over);
        var problem = await response.ReadAsync<QuotaProblemDto>();

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("matching.tailoring_quota_exceeded", problem.Code);
        Assert.Equal(DailyLimit, await CountRowsAsync("llm.analysis_jobs"));
    }

    [Fact]
    public async Task Request_InvalidAnswerTwice_FailsAndGivesTheUseBack()
    {
        _llm.Respond = _ => CannedLlmClient.Answer("""{"summary":"","suggestions":[]}""");
        var application = await ReadyToTailorAsync(Alice);

        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();
        var failed = await Client.GetTailoringOkAsync(Alice, application);

        Assert.Equal("Failed", failed.State);
        Assert.Equal(0, failed.Usage.Used);
        Assert.Equal(2, _llm.Requests.Count);
    }

    [Fact]
    public async Task Job_SuggestionsNotGroundedInTheCv_AreDropped()
    {
        _llm.Respond = _ => CannedLlmClient.Answer("""
            {"summary":"Good fit.",
             "suggestions":[
               {"cvSection":"Experience","cvQuote":"Designed event-driven services on Azure","advice":"Move it up."},
               {"cvSection":"Skills","cvQuote":"Skills: C#","advice":"Add Kubernetes to your skills."},
               {"cvSection":"Skills","cvQuote":"Skills: C#","advice":"List C# first."}]}
            """);
        var application = await ReadyToTailorAsync(Alice);

        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();
        var tailoring = await Client.GetTailoringOkAsync(Alice, application);

        var kept = Assert.Single(tailoring.Tailoring!.Suggestions);
        Assert.Equal("List C# first.", kept.Advice);
        Assert.Equal("Skills: C#", kept.CvQuote);
    }

    [Fact]
    public async Task Job_NoSuggestionGrounded_FailsAndGivesTheUseBack()
    {
        _llm.Respond = _ => CannedLlmClient.Answer("""
            {"summary":"Good fit.",
             "suggestions":[{"cvSection":"Skills","cvQuote":"Skills: C#","advice":"Say you know Azure."}]}
            """);
        var application = await ReadyToTailorAsync(Alice);

        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();
        var failed = await Client.GetTailoringOkAsync(Alice, application);

        Assert.Equal("Failed", failed.State);
        Assert.Equal(0, failed.Usage.Used);
        Assert.Equal(0, await CountRowsAsync("matching.tailoring_results"));
    }

    [Fact]
    public async Task Request_UsesMediumReasoningForTailoring()
    {
        var application = await ReadyToTailorAsync(Alice);

        await Client.RequestTailoringOkAsync(Alice, application);
        await RunJobsAsync();

        Assert.Equal(LlmReasoning.Medium, Assert.Single(_llm.Requests).Reasoning);
    }

    [Fact]
    public async Task GetAndRequest_AnotherUsersApplication_AreNotFound()
    {
        var alices = await ReadyToTailorAsync(Alice);
        await Client.RequestTailoringOkAsync(Alice, alices);
        await RunJobsAsync();

        using var get = await Client.GetTailoringAsync(Bob, alices);
        using var request = await Client.RequestTailoringAsync(Bob, alices);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, request.StatusCode);
    }

    [Fact]
    public async Task GetAndRequest_Anonymous_AreUnauthorized()
    {
        var application = await CreateAsync(Alice);

        using var get = await Client.GetTailoringAsync(accessToken: null, application);
        using var request = await Client.RequestTailoringAsync(accessToken: null, application);

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, request.StatusCode);
    }

    private async Task<Guid> ReadyToTailorAsync(string accessToken, string cvMarker = "ALICE-CV-MARKER")
    {
        await UploadAsync(accessToken, cvMarker);
        await Client.SetAiConsentAsync(accessToken, allowed: true);
        return await CreateAsync(accessToken);
    }

    private async Task UploadAsync(string accessToken, string cvMarker = "ALICE-CV-MARKER")
    {
        using var response = await Client.UploadCvAsync(
            accessToken, CvApi.Pdf("Ada Lovelace", "Skills: C#, ASP.NET Core, PostgreSQL", cvMarker));
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateAsync(string accessToken, string? postingText = PostingText, string? url = null) =>
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
