using System.Net;
using System.Net.Http.Headers;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Matching;

public sealed class MatchTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const string PostingText = "We build shipment APIs with C#, .NET 8 and PostgreSQL, deployed to Kubernetes on Azure.";

    [Fact]
    public async Task Match_CvAndPosting_ListsMatchedAndMissingSkillsWithAScore()
    {
        var application = await CreateAsync(Alice, PostingText);
        await UploadAsync(Alice, "Skills: C#, ASP.NET Core, PostgreSQL, Docker");

        var match = await GetMatchOkAsync(Alice, application);

        // ".NET" is in the role and the text; the CV shows it through C# and ASP.NET Core.
        Assert.Equal("Ready", match.State);
        Assert.Equal(["C#", ".NET", "PostgreSQL"], match.Matched.Select(skill => skill.Name));
        Assert.Equal(["Kubernetes", "Azure"], match.Missing.Select(skill => skill.Name));
        Assert.Equal(60, match.Score);
        Assert.Equal(["Language", "Framework", "Database"], match.Matched.Select(skill => skill.Category));
    }

    [Fact]
    public async Task Match_AfterANewCv_IsWorkedOutAgain()
    {
        var application = await CreateAsync(Alice, PostingText);
        await UploadAsync(Alice, "Skills: C#");
        var before = await GetMatchOkAsync(Alice, application);

        await UploadAsync(Alice, "Skills: C#, PostgreSQL, Kubernetes, Azure");
        var after = await GetMatchOkAsync(Alice, application);

        Assert.Equal(40, before.Score);
        Assert.Equal(100, after.Score);
        Assert.Empty(after.Missing);
    }

    [Fact]
    public async Task Match_NoCv_ListsThePostingSkillsAsMissing()
    {
        var application = await CreateAsync(Alice, PostingText);

        var match = await GetMatchOkAsync(Alice, application);

        Assert.Equal("NoCv", match.State);
        Assert.Null(match.Score);
        Assert.Empty(match.Matched);
        Assert.Equal(5, match.Missing.Length);
    }

    [Fact]
    public async Task Match_NoPostingText_SaysSo()
    {
        var application = await CreateAsync(Alice, postingText: null, role: "Office manager");
        await UploadAsync(Alice, "Skills: C#");

        var match = await GetMatchOkAsync(Alice, application);

        Assert.Equal("NoPostingText", match.State);
        Assert.Null(match.Score);
    }

    [Fact]
    public async Task Match_NoPostingTextButSkillsInTheRole_UsesTheRole()
    {
        var application = await CreateAsync(Alice, postingText: null, role: "React Developer");
        await UploadAsync(Alice, "Skills: React, Redux");

        var match = await GetMatchOkAsync(Alice, application);

        Assert.Equal("Ready", match.State);
        Assert.Equal(100, match.Score);
    }

    [Fact]
    public async Task Match_PostingWithoutKnownSkills_SaysSo()
    {
        var application = await CreateAsync(Alice, "Friendly team, flexible hours, great coffee.", role: "Generalist");

        var match = await GetMatchOkAsync(Alice, application);

        Assert.Equal("NoSkillsInPosting", match.State);
    }

    [Fact]
    public async Task Match_AnotherUsersApplication_Returns404()
    {
        var alices = await CreateAsync(Alice, PostingText);
        await UploadAsync(Bob, "Skills: C#");

        using var response = await GetMatchAsync(Bob, alices);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("matching.application_not_found", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Match_UnknownApplication_Returns404()
    {
        using var response = await GetMatchAsync(Alice, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Match_Anonymous_Returns401()
    {
        var application = await CreateAsync(Alice, PostingText);

        using var response = await GetMatchAsync(accessToken: null, application);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateAsync(string accessToken, string? postingText, string role = "Backend Engineer (.NET)")
    {
        var created = await Client.CreateApplicationOkAsync(
            accessToken, NewApplication(role: role, url: null, postingText: postingText));
        return created.Id;
    }

    private async Task UploadAsync(string accessToken, string skillsLine)
    {
        using var response = await Client.UploadCvAsync(accessToken, CvApi.Pdf("Ada Lovelace", skillsLine));
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> GetMatchAsync(string? accessToken, Guid applicationId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri($"{ApplicationsApi.BasePath}/{applicationId}/match", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await Client.SendAsync(request, CancellationToken);
    }

    private async Task<MatchDto> GetMatchOkAsync(string accessToken, Guid applicationId)
    {
        using var response = await GetMatchAsync(accessToken, applicationId);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<MatchDto>();
    }
}

internal sealed record MatchDto(string State, int? Score, MatchSkillDto[] Matched, MatchSkillDto[] Missing);

internal sealed record MatchSkillDto(string Name, string Category);
