using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

/// <summary>No API keys configured: the app works and the analysis says it is unavailable.</summary>
public sealed class PostingAnalysisDisabledTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task Get_LlmDisabled_IsUnavailable()
    {
        var application = await CreateAsync();

        var analysis = await Client.GetPostingAnalysisOkAsync(Alice, application);

        Assert.Equal("Unavailable", analysis.State);
    }

    [Fact]
    public async Task Request_LlmDisabled_IsConflictAndQueuesNothing()
    {
        var application = await CreateAsync();

        using var response = await Client.RequestPostingAnalysisAsync(Alice, application);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("llm.analysis_jobs"));
    }

    private async Task<Guid> CreateAsync() =>
        (await Client.CreateApplicationOkAsync(Alice, NewApplication(postingText: "Backend engineer, .NET, remote."))).Id;
}
