using System.Net;
using System.Runtime.InteropServices;
using Heardback.IntegrationTests.Postgres;
using Npgsql;

namespace Heardback.IntegrationTests.Applications;

/// <summary>A shared posting is deleted once no application uses it, and never under a new link (ADR-0015).</summary>
public sealed class PostingCleanupTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    // Must match PostingLock in the Applications module.
    private const int PostingLockSpace = 0x4842_5031;

    [Fact]
    public async Task Update_NewLink_DeletesTheOldPostingNobodyUses()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.UpdateApplicationAsync(Alice, created.Id, new
        {
            company = created.Company,
            role = created.Role,
            url = "https://jobs.example.com/other",
            postingText = (string?)null,
            channel = created.Channel,
            appliedOn = created.AppliedOn,
            notes = (string?)null,
            version = created.Version,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("postings.job_postings"));
        Assert.Equal(1, await CountRowsAsync(
            "postings.job_postings WHERE url = 'https://jobs.example.com/other'"));
    }

    [Fact]
    public async Task Create_PostingDeletedWhileWaitingForItsLock_LinksToAPostingThatExists()
    {
        var bobs = await Client.CreateApplicationOkAsync(Bob, NewApplication());
        var postingId = await PostingIdOfAsync(bobs.Id);

        // Stand in for a cleanup that holds the posting's lock: Alice's create resolves the posting, then waits.
        await using var cleanup = new NpgsqlConnection(ConnectionString);
        await cleanup.OpenAsync(CancellationToken);
        await using var transaction = await cleanup.BeginTransactionAsync(CancellationToken);
        await ExecuteAsync(cleanup, $"SELECT pg_advisory_xact_lock({PostingLockSpace}, {KeyOf(postingId)})");

        var aliceCreate = CreateClient().CreateApplicationAsync(Alice, NewApplication());
        await WaitUntilSomeoneWaitsForAnAdvisoryLockAsync();

        await ExecuteAsync(cleanup, $"DELETE FROM applications.applications WHERE id = '{bobs.Id}'");
        await ExecuteAsync(cleanup, $"DELETE FROM postings.job_postings WHERE id = '{postingId}'");
        await transaction.CommitAsync(CancellationToken);

        using var response = await aliceCreate;
        var alices = await response.ReadAsync<ApplicationDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var linkedTo = await PostingIdOfAsync(alices.Id);
        Assert.NotEqual(postingId, linkedTo);
        Assert.Equal(1, await CountRowsAsync($"postings.job_postings WHERE id = '{linkedTo}'"));
    }

    [Fact]
    public async Task DeleteAndCreate_SamePostingAtOnce_NeverLinksToAMissingPosting()
    {
        for (var round = 0; round < 10; round++)
        {
            var url = $"https://jobs.example.com/race-{round}";
            var bobs = await Client.CreateApplicationOkAsync(Bob, NewApplication(url: url));

            var delete = CreateClient().DeleteApplicationAsync(Bob, bobs.Id);
            var create = CreateClient().CreateApplicationAsync(Alice, NewApplication(url: url));
            using var deleted = await delete;
            using var created = await create;

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        Assert.Equal(0, await CountRowsAsync(
            "applications.applications AS a WHERE NOT EXISTS (SELECT 1 FROM postings.job_postings AS p WHERE p.id = a.posting_id)"));
    }

    private static int KeyOf(Guid postingId)
    {
        Span<byte> bytes = stackalloc byte[16];
        postingId.TryWriteBytes(bytes);
        var parts = MemoryMarshal.Cast<byte, int>(bytes);
        return parts[0] ^ parts[1] ^ parts[2] ^ parts[3];
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private async Task<Guid> PostingIdOfAsync(Guid applicationId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT posting_id FROM applications.applications WHERE id = '{applicationId}'", connection);
        return (Guid)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private async Task WaitUntilSomeoneWaitsForAnAdvisoryLockAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (await CountRowsAsync("pg_locks WHERE locktype = 'advisory' AND NOT granted") == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
