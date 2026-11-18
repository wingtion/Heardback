using System.Net;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Matching;

public sealed class CvTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private static readonly byte[] AdasCv = CvApi.Pdf(
        "Ada Lovelace",
        "ada@example.com | +90 532 123 45 67 | linkedin.com/in/ada",
        "Skills: C#, .NET, PostgreSQL, Docker");

    private const string AdasStoredText =
        "Ada Lovelace\n[email] | [phone] | [link]\nSkills: C#, .NET, PostgreSQL, Docker";

    [Fact]
    public async Task Upload_Pdf_StoresOnlyTheTextWithoutPersonalDetails()
    {
        using var upload = await Client.UploadCvAsync(Alice, AdasCv);

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var uploaded = await upload.ReadAsync<CvDto>();
        Assert.Equal(AdasStoredText, uploaded.Text);
        Assert.Equal(["C#", ".NET", "PostgreSQL", "Docker"], uploaded.Skills);
        Assert.Equal(1, uploaded.PageCount);
        Assert.Equal(Clock.GetUtcNow(), uploaded.UploadedAt, TimeSpan.FromMilliseconds(1));

        using var read = await Client.GetCvAsync(Alice);
        var stored = await read.ReadAsync<CvDto>();
        Assert.Equal(uploaded.Text, stored.Text);
        Assert.Equal(uploaded.Skills, stored.Skills);
        Assert.Equal(uploaded.UploadedAt, stored.UploadedAt);
    }

    [Fact]
    public async Task Upload_Again_ReplacesTheEarlierCv()
    {
        using var first = await Client.UploadCvAsync(Alice, AdasCv);
        Clock.Advance(TimeSpan.FromMinutes(5));

        using var second = await Client.UploadCvAsync(Alice, CvApi.Pdf("Ada Lovelace", "Skills: Angular, TypeScript"));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(1, await CountRowsAsync("matching.cvs"));
        using var read = await Client.GetCvAsync(Alice);
        var stored = await read.ReadAsync<CvDto>();
        Assert.Equal("Ada Lovelace\nSkills: Angular, TypeScript", stored.Text);
        Assert.Equal(Clock.GetUtcNow(), stored.UploadedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Upload_TwoFirstUploadsAtOnce_LeaveOneCv()
    {
        var uploads = Enumerable.Range(0, 4).Select(_ => CreateClient().UploadCvAsync(Alice, AdasCv)).ToList();

        var responses = await Task.WhenAll(uploads);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(1, await CountRowsAsync("matching.cvs"));
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task Get_NoCvYet_Returns204()
    {
        using var read = await Client.GetCvAsync(Alice);

        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
    }

    [Fact]
    public async Task Delete_StoredCv_RemovesItAndASecondDeleteStillSucceeds()
    {
        using var upload = await Client.UploadCvAsync(Alice, AdasCv);

        using var deleted = await Client.DeleteCvAsync(Alice);
        using var deletedAgain = await Client.DeleteCvAsync(Alice);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deletedAgain.StatusCode);
        Assert.Equal(0, await CountRowsAsync("matching.cvs"));
    }

    [Fact]
    public async Task OtherUser_CannotReadReplaceOrDeleteTheCv()
    {
        using var alices = await Client.UploadCvAsync(Alice, AdasCv);

        using var bobReads = await Client.GetCvAsync(Bob);
        using var bobDeletes = await Client.DeleteCvAsync(Bob);
        using var bobUploads = await Client.UploadCvAsync(Bob, CvApi.Pdf("Bob", "Skills: Java"));

        Assert.Equal(HttpStatusCode.NoContent, bobReads.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, bobDeletes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bobUploads.StatusCode);
        using var aliceReads = await Client.GetCvAsync(Alice);
        Assert.Equal(AdasStoredText, (await aliceReads.ReadAsync<CvDto>()).Text);
        Assert.Equal(2, await CountRowsAsync("matching.cvs"));
    }

    [Fact]
    public async Task Upload_NotAPdf_Returns400OnTheFileField()
    {
        using var upload = await Client.UploadCvAsync(Alice, "PK\u0003\u0004 a Word file"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        var problem = await upload.ReadAsync<ProblemDto>();
        Assert.Equal("matching.cv_not_pdf", problem.Code);
        Assert.Equal(["Choose a PDF file."], problem.Errors!["file"]);
        Assert.Equal(0, await CountRowsAsync("matching.cvs"));
    }

    [Fact]
    public async Task Upload_NoFile_Returns400OnTheFileField()
    {
        using var upload = await Client.UploadWithoutFileAsync(Alice);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("matching.cv_missing", (await upload.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Upload_Anonymous_Returns401()
    {
        using var upload = await Client.UploadCvAsync(accessToken: null, AdasCv);

        Assert.Equal(HttpStatusCode.Unauthorized, upload.StatusCode);
    }

    [Fact]
    public async Task Upload_ScanWithoutText_Returns400()
    {
        using var upload = await Client.UploadCvAsync(Alice, CvApi.Pdf());

        Assert.Equal("matching.cv_no_text", (await upload.ReadAsync<ProblemDto>()).Code);
    }
}
