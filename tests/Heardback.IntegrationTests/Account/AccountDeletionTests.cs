using System.Net;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Identity;
using Heardback.IntegrationTests.Matching;
using Heardback.IntegrationTests.Postgres;
using Npgsql;

namespace Heardback.IntegrationTests.Account;

/// <summary>Deleting an account removes everything personal and nothing of anyone else (ADR-0015).</summary>
public sealed class AccountDeletionTests(PostgresFixture postgres) : AccountTestBase(postgres)
{
    private const string BobEmail = "bob@example.com";
    private const string BobUrl = "https://jobs.example.com/bob-only";

    private static readonly string[] ModuleSchemas = ["identity", "applications", "postings", "matching", "llm"];

    [Fact]
    public async Task DeleteAccount_WithDataInEveryModule_LeavesNoRowWithTheUsersId()
    {
        var bobId = await UserIdOfAsync(BobEmail);
        await GiveBobDataInEveryModuleAsync();
        var ownerColumns = await UserIdColumnsAsync();
        Assert.Contains("applications.application_events.user_id", ownerColumns);
        Assert.Contains("matching.tailoring_results.user_id", ownerColumns);
        Assert.Contains("llm.analysis_jobs.requested_by_user_id", ownerColumns);
        Assert.True(await CountRowsWithAsync(ownerColumns, bobId) > 0);

        using var response = await Client.DeleteAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountRowsWithAsync(ownerColumns, bobId));
        Assert.Equal(0, await CountAsync("SELECT count(*) FROM identity.users WHERE id = @id", bobId));
    }

    [Fact]
    public async Task DeleteAccount_WithData_KeepsOtherMembersData()
    {
        var aliceId = await UserIdOfAsync("alice@example.com");
        await Client.CreateApplicationOkAsync(Alice, NewApplication(postingText: PostingText));
        await GiveBobDataInEveryModuleAsync();

        // The invite Alice gave Bob goes with Bob's account: it records his id (ADR-0015).
        var ownerColumns = (await UserIdColumnsAsync())
            .Where(column => !column.StartsWith("identity.invite_codes.", StringComparison.Ordinal))
            .ToList();
        var aliceRows = await CountRowsWithAsync(ownerColumns, aliceId);

        using var response = await Client.DeleteAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(aliceRows, await CountRowsWithAsync(ownerColumns, aliceId));
        Assert.Single((await Client.ListApplicationsOkAsync(Alice)).Items);
    }

    [Fact]
    public async Task DeleteAccount_PostingOnlyTheUserTracked_IsDeleted()
    {
        await Client.CreateApplicationOkAsync(Bob, NewApplication(url: BobUrl));

        using var response = await Client.DeleteAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("postings.job_postings"));
    }

    [Fact]
    public async Task DeleteAccount_PostingAnotherMemberTracks_IsKept()
    {
        await Client.CreateApplicationOkAsync(Bob, NewApplication(postingText: PostingText));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(postingText: PostingText));
        await RequestPostingAnalysisAsync(Alice);

        using var response = await Client.DeleteAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
        Assert.Equal(1, await CountRowsAsync("applications.posting_analyses"));
    }

    [Fact]
    public async Task DeleteAccount_AnalysisOnlyTheUserUsed_IsDeleted()
    {
        await RequestPostingAnalysisAsync(Bob);
        Assert.Equal(1, await CountRowsAsync("applications.posting_analyses"));

        using var response = await Client.DeleteAccountAsync(Bob);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.posting_analyses"));
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_Returns400AndKeepsTheAccount()
    {
        await Client.CreateApplicationOkAsync(Bob, NewApplication());

        using var response = await Client.DeleteAccountAsync(Bob, password: "not the right password");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("password", await response.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
        Assert.Single((await Client.ListApplicationsOkAsync(Bob)).Items);
    }

    [Fact]
    public async Task DeleteAccount_MissingPassword_Returns400()
    {
        using var response = await Client.DeleteAccountAsync(Bob, password: string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, await CountAsync("SELECT count(*) FROM identity.users WHERE id = @id", await UserIdOfAsync(BobEmail)));
    }

    [Fact]
    public async Task DeleteAccount_OnlyAdmin_Returns409()
    {
        using var response = await Client.DeleteAccountAsync(Alice);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var me = await Client.GetMeAsync(Alice);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_AdminWithAnotherAdmin_Succeeds()
    {
        var bobId = await UserIdOfAsync(BobEmail);
        await ExecuteSqlAsync($"UPDATE identity.users SET is_admin = true WHERE id = '{bobId}'");

        using var response = await Client.DeleteAccountAsync(Alice);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_AccessTokenAfterwards_IsRejected()
    {
        using var deleted = await Client.DeleteAccountAsync(Bob);

        using var list = await Client.ListApplicationsAsync(Bob);
        using var create = await Client.CreateApplicationAsync(Bob, NewApplication());

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_Afterwards_SignInAndRefreshFail()
    {
        var bobClient = CreateClient();
        var bob = await bobClient.SignInAsync(BobEmail);

        using var deleted = await bobClient.DeleteAccountAsync(bob.AccessToken);
        using var refresh = await bobClient.RefreshAsync();
        using var login = await CreateClient().LoginAsync(BobEmail);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_Anonymous_Returns401()
    {
        using var response = await Client.DeleteAccountAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task GiveBobDataInEveryModuleAsync()
    {
        using var cv = await Client.UploadCvAsync(Bob, CvApi.Pdf("Bob Builder", "Skills: C#, PostgreSQL"));
        cv.EnsureSuccessStatusCode();
        await Client.SetAiConsentAsync(Bob, allowed: true);

        var application = await Client.CreateApplicationOkAsync(
            Bob, NewApplication(url: BobUrl, postingText: PostingText));
        using var update = await Client.AddEventAsync(Bob, application.Id, new { status = "Interview", occurredOn = Today, note = "Call" });
        update.EnsureSuccessStatusCode();

        using var analysis = await Client.RequestPostingAnalysisAsync(Bob, application.Id);
        analysis.EnsureSuccessStatusCode();
        using var tailoring = await Client.RequestTailoringAsync(Bob, application.Id);
        tailoring.EnsureSuccessStatusCode();
        await RunJobsAsync();
        Assert.Equal(1, await CountRowsAsync("matching.tailoring_results"));
    }

    private async Task RequestPostingAnalysisAsync(string accessToken)
    {
        var application = (await Client.ListApplicationsOkAsync(accessToken)).Items.FirstOrDefault()?.Id
            ?? (await Client.CreateApplicationOkAsync(accessToken, NewApplication(postingText: PostingText))).Id;
        using var response = await Client.RequestPostingAnalysisAsync(accessToken, application);
        response.EnsureSuccessStatusCode();
        await RunJobsAsync();
    }

    // Every uuid column naming a user in any module schema, so a table added later is checked too.
    private async Task<List<string>> UserIdColumnsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT table_schema || '.' || table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = ANY(@schemas) AND data_type = 'uuid' AND column_name LIKE '%user_id'
            ORDER BY 1
            """,
            connection);
        command.Parameters.AddWithValue("schemas", ModuleSchemas);
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private async Task<long> CountRowsWithAsync(IEnumerable<string> columns, Guid userId)
    {
        long total = 0;
        foreach (var column in columns)
        {
            var lastDot = column.LastIndexOf('.');
            var sql = $"SELECT count(*) FROM {column[..lastDot]} WHERE {column[(lastDot + 1)..]} = @id";
            total += await CountAsync(sql, userId);
        }

        return total;
    }
}
