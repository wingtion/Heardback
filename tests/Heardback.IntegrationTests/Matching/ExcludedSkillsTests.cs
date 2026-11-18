using System.Globalization;
using System.Net;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Matching;

public sealed class ExcludedSkillsTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const int WritesPerMinute = 3;

    // The project description names languages the author does not use (found while testing a real CV).
    private static readonly byte[] Cv = CvApi.Pdf(
        "Skills: C#, PostgreSQL, Docker",
        "Checked Java, C++, Go and Rust answers by LLM output prediction");

    protected override void ConfigureSettings(IDictionary<string, string?> settings) =>
        settings["Matching:CvWritesPerMinute"] = WritesPerMinute.ToString(CultureInfo.InvariantCulture);

    [Fact]
    public async Task Exclude_Skills_LeavesThemOutOfFoundAndListsThemApart()
    {
        await UploadAsync(Cv);

        using var response = await Client.ExcludeSkillsAsync(Alice, "Rust", "Go", "C++");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cv = await response.ReadAsync<CvDto>();
        Assert.Equal(["C#", "Java", "PostgreSQL", "Docker"], cv.Skills);
        Assert.Equal(["C++", "Go", "Rust"], cv.ExcludedSkills);
    }

    [Fact]
    public async Task Exclude_ThenANewUpload_KeepsTheExcludedSkills()
    {
        await UploadAsync(Cv);
        using var excluded = await Client.ExcludeSkillsAsync(Alice, "Rust");

        await UploadAsync(CvApi.Pdf("Skills: C#, Rust"));

        using var read = await Client.GetCvAsync(Alice);
        var cv = await read.ReadAsync<CvDto>();
        Assert.Equal(["C#"], cv.Skills);
        Assert.Equal(["Rust"], cv.ExcludedSkills);
    }

    [Fact]
    public async Task Exclude_EmptyList_RestoresEverySkill()
    {
        await UploadAsync(Cv);
        using var excluded = await Client.ExcludeSkillsAsync(Alice, "Rust");

        using var restored = await Client.ExcludeSkillsAsync(Alice);

        var cv = await restored.ReadAsync<CvDto>();
        Assert.Contains("Rust", cv.Skills);
        Assert.Empty(cv.ExcludedSkills);
    }

    [Fact]
    public async Task Match_ExcludedSkill_CountsAsMissingEvenWhenImplied()
    {
        await UploadAsync(CvApi.Pdf("Skills: C#, Rust"));
        var application = await Client.CreateApplicationOkAsync(
            Alice, NewApplication(role: "Engineer", url: null, postingText: "We use Rust and .NET."));
        using var excluded = await Client.ExcludeSkillsAsync(Alice, "Rust", ".NET");

        using var response = await Client.SendAsync(
            Authorized(HttpMethod.Get, $"{ApplicationsApi.BasePath}/{application.Id}/match"), CancellationToken);

        var match = await response.ReadAsync<MatchDto>();
        Assert.Equal(0, match.Score);
        Assert.Equal(["Rust", ".NET"], match.Missing.Select(skill => skill.Name));
    }

    [Fact]
    public async Task Exclude_UnknownSkill_Returns400OnTheSkillsField()
    {
        await UploadAsync(Cv);

        using var response = await Client.ExcludeSkillsAsync(Alice, "Rust", "Cobol");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("matching.unknown_skill", problem.Code);
        Assert.Contains("Cobol", problem.Errors!["skills"][0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Exclude_NoCv_Returns404AndLeavesOtherUsersAlone()
    {
        await UploadAsync(Cv);
        using var alices = await Client.ExcludeSkillsAsync(Alice, "Rust");

        using var bobs = await Client.ExcludeSkillsAsync(Bob, "C#");

        Assert.Equal(HttpStatusCode.NotFound, bobs.StatusCode);
        Assert.Equal("matching.cv_not_found", (await bobs.ReadAsync<ProblemDto>()).Code);
        using var read = await Client.GetCvAsync(Alice);
        Assert.Equal(["Rust"], (await read.ReadAsync<CvDto>()).ExcludedSkills);
    }

    [Fact]
    public async Task Exclude_OverTheWriteLimit_Returns429()
    {
        await UploadAsync(Cv);
        for (var write = 0; write < WritesPerMinute; write++)
        {
            using var accepted = await Client.ExcludeSkillsAsync(Alice, "Rust");
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using var rejected = await Client.ExcludeSkillsAsync(Alice, "Rust");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private async Task UploadAsync(byte[] pdf)
    {
        using var response = await Client.UploadCvAsync(Alice, pdf);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Alice);
        return request;
    }
}
