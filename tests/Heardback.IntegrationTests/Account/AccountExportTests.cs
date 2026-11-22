using System.Globalization;
using System.Net;
using System.Text.Json;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Matching;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Account;

/// <summary>The data export holds everything the user can see about themselves and nothing of anyone else (ADR-0015).</summary>
public sealed class AccountExportTests(PostgresFixture postgres) : AccountTestBase(postgres)
{
    private const int ExportsPerWindow = 3;

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        base.ConfigureSettings(settings);
        settings["Identity:RateLimits:DataExportsPerWindow"] = ExportsPerWindow.ToString(CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task Export_WithData_HasTheUsersDataInEverySection()
    {
        var application = await GiveBobDataAsync();

        using var response = await Client.ExportAccountAsync(Bob);
        using var export = await ReadJsonAsync(response);
        var root = export.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal("bob@example.com", root.GetProperty("account").GetProperty("email").GetString());

        var exported = Assert.Single(root.GetProperty("data").GetProperty("applications").EnumerateArray().ToList());
        Assert.Equal(application, exported.GetProperty("id").GetGuid());
        Assert.Equal("BOB-NOTES-MARKER", exported.GetProperty("notes").GetString());
        Assert.Contains("shipment APIs", exported.GetProperty("postingText").GetString(), StringComparison.Ordinal);
        Assert.Equal("Interview", exported.GetProperty("status").GetString());
        Assert.Contains(
            exported.GetProperty("timeline").EnumerateArray(),
            entry => entry.GetProperty("note").GetString() == "BOB-EVENT-MARKER");
        Assert.Equal("Northwind", exported.GetProperty("jobSummary").GetProperty("company").GetString());

        var cv = root.GetProperty("data").GetProperty("cv");
        Assert.Contains("BOB-CV-MARKER", cv.GetProperty("cv").GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Equal(JsonValueKind.String, cv.GetProperty("aiConsentGivenAt").ValueKind);
        var suggestions = Assert.Single(cv.GetProperty("cvSuggestions").EnumerateArray().ToList());
        Assert.Equal(application, suggestions.GetProperty("applicationId").GetGuid());
    }

    [Fact]
    public async Task Export_AnotherMembersData_IsNeverIncluded()
    {
        using var aliceCv = await Client.UploadCvAsync(Alice, CvApi.Pdf("Alice", "Skills: C#", "ALICE-CV-MARKER"));
        aliceCv.EnsureSuccessStatusCode();
        await Client.CreateApplicationOkAsync(Alice, NewApplication(
            company: "Contoso", url: "https://jobs.example.com/alice", postingText: "ALICE-POSTING-MARKER"));
        await GiveBobDataAsync();

        using var response = await Client.ExportAccountAsync(Bob);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("ALICE", body, StringComparison.Ordinal);
        Assert.DoesNotContain("alice@example.com", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_NewUser_HasEmptySections()
    {
        using var response = await Client.ExportAccountAsync(Bob);
        using var export = await ReadJsonAsync(response);
        var data = export.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(data.GetProperty("applications").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("cv").GetProperty("cv").ValueKind);
        Assert.Empty(data.GetProperty("cv").GetProperty("cvSuggestions").EnumerateArray());
    }

    [Fact]
    public async Task Export_Response_IsADownloadThatIsNotCached()
    {
        using var response = await Client.ExportAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("heardback-export-", response.Content.Headers.ContentDisposition?.FileName, StringComparison.Ordinal);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Export_OverThePerUserLimit_Returns429ButOthersCanStillExport()
    {
        for (var attempt = 0; attempt < ExportsPerWindow; attempt++)
        {
            using var allowed = await Client.ExportAccountAsync(Bob);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var refused = await Client.ExportAccountAsync(Bob);
        using var alice = await Client.ExportAccountAsync(Alice);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alice.StatusCode);
    }

    [Fact]
    public async Task Export_Anonymous_Returns401()
    {
        using var response = await Client.ExportAccountAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> GiveBobDataAsync()
    {
        using var cv = await Client.UploadCvAsync(Bob, CvApi.Pdf("Bob Builder", "Skills: C#, PostgreSQL", "BOB-CV-MARKER"));
        cv.EnsureSuccessStatusCode();
        await Client.SetAiConsentAsync(Bob, allowed: true);

        var created = await Client.CreateApplicationOkAsync(
            Bob, NewApplication(url: "https://jobs.example.com/bob", postingText: PostingText));
        using var notes = await Client.UpdateApplicationAsync(Bob, created.Id, new
        {
            company = created.Company,
            role = created.Role,
            url = created.Url,
            postingText = created.PostingText,
            channel = created.Channel,
            appliedOn = created.AppliedOn,
            notes = "BOB-NOTES-MARKER",
            version = created.Version,
        });
        notes.EnsureSuccessStatusCode();
        using var update = await Client.AddEventAsync(
            Bob, created.Id, new { status = "Interview", occurredOn = Today, note = "BOB-EVENT-MARKER" });
        update.EnsureSuccessStatusCode();

        using var analysis = await Client.RequestPostingAnalysisAsync(Bob, created.Id);
        analysis.EnsureSuccessStatusCode();
        using var tailoring = await Client.RequestTailoringAsync(Bob, created.Id);
        tailoring.EnsureSuccessStatusCode();
        await RunJobsAsync();
        return created.Id;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(CancellationToken), cancellationToken: CancellationToken);
}
