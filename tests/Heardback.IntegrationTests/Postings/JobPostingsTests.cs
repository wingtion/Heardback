using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Postgres;
using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace Heardback.IntegrationTests.Postings;

/// <summary>Each test gets its own freshly migrated database (xUnit creates a new instance per test).</summary>
[Collection(PostgresCollectionDefinition.Name)]
public sealed class JobPostingsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Company = "Northwind Logistics";
    private const string Title = "Backend Engineer (.NET)";
    private const string Text = "We are hiring a backend engineer to work on shipment tracking APIs.";
    private const string Url = "https://jobs.example.com/4471";

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private ApiFactory _factory = null!;
    private string _connectionString = string.Empty;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        _factory = new ApiFactory { ConnectionString = _connectionString, Clock = _clock };
        _ = _factory.Services;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await postgres.DropDatabaseAsync(_connectionString, CancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_NewPosting_CreatesIt()
    {
        var result = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await CountPostingsAsync());
    }

    [Fact]
    public async Task ResolveAsync_SameLinkCopiedDifferently_ReturnsTheSamePosting()
    {
        var first = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));
        _clock.Advance(TimeSpan.FromDays(2));

        var second = await ResolveAsync(
            new PostingSubmission("http://www.jobs.example.com/4471/?utm_source=linkedin#top", "Northwind", "Backend Dev", null));

        Assert.Equal(first.Value, second.Value);
        Assert.Equal(1, await CountPostingsAsync());
        var (timesAdded, lastSeenAt) = await ReadSubmissionStatsAsync(first.Value);
        Assert.Equal(2, timesAdded);
        Assert.Equal(_clock.GetUtcNow(), lastSeenAt);
    }

    [Fact]
    public async Task ResolveAsync_SameContentUnderAnotherLink_ReturnsTheSamePosting()
    {
        var first = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));

        var second = await ResolveAsync(
            new PostingSubmission("https://careers.example.org/northwind/backend", Company, Title, Text));

        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task ResolveAsync_SameContentWithoutLink_ReturnsTheSamePosting()
    {
        var first = await ResolveAsync(new PostingSubmission(null, Company, Title, Text));

        var second = await ResolveAsync(new PostingSubmission("", Company.ToUpperInvariant(), Title, Text + "  "));

        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task ResolveAsync_LinkMatchesOnePostingAndTextAnother_PrefersTheLink()
    {
        var byText = await ResolveAsync(new PostingSubmission(null, Company, Title, Text));
        var byLink = await ResolveAsync(new PostingSubmission(Url, "Kestrel Health", "Junior .NET Developer", null));

        var resolved = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));

        Assert.NotEqual(byText.Value, byLink.Value);
        Assert.Equal(byLink.Value, resolved.Value);
    }

    [Fact]
    public async Task ResolveAsync_NoLinkAndNoText_AlwaysCreatesANewPosting()
    {
        var first = await ResolveAsync(new PostingSubmission(null, Company, Title, null));
        var second = await ResolveAsync(new PostingSubmission(null, Company, Title, null));

        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, await CountPostingsAsync());
    }

    [Fact]
    public async Task ResolveAsync_InvalidLink_ReturnsValidationErrorOnUrlField()
    {
        var result = await ResolveAsync(new PostingSubmission("northwind-logistics/jobs", Company, Title, Text));

        Assert.False(result.IsSuccess);
        Assert.Equal("postings.invalid_url", result.Error.Code);
        Assert.Equal([PostingUrlAttribute.Message], result.Error.FieldErrors["url"]);
        Assert.Equal(0, await CountPostingsAsync());
    }

    [Fact]
    public async Task ResolveAsync_LinkWithCredentialsAndTracking_StoresTheCleanedLink()
    {
        var created = await ResolveAsync(new PostingSubmission(
            "https://user:secret@jobs.example.com/77?mc_eid=abc123&utm_source=x#apply", Company, Title, null));

        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("SELECT url FROM postings.job_postings WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", created.Value);
        Assert.Equal("https://jobs.example.com/77", await command.ExecuteScalarAsync(CancellationToken));
    }

    [Fact]
    public async Task ResolveAsync_LinkThatEncodesPastTheLimit_ReturnsValidationError()
    {
        var result = await ResolveAsync(
            new PostingSubmission("https://jobs.example.com/" + new string('€', 1500), Company, Title, Text));

        Assert.Equal("postings.invalid_url", result.Error!.Code);
        Assert.Equal(0, await CountPostingsAsync());
    }

    [Fact]
    public async Task ResolveAsync_ExistingPosting_IsNeverChanged()
    {
        var first = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));

        await ResolveAsync(new PostingSubmission(Url, "Someone Else", "Other Role", "Completely different text."));

        Assert.Equal((Company, Title, Text), await ReadContentAsync(first.Value));
    }

    [Fact]
    public async Task ResolveAsync_SamePostingSubmittedConcurrently_CreatesOnePosting()
    {
        const int submitters = 8;
        using var start = new SemaphoreSlim(0);

        var tasks = Enumerable.Range(0, submitters)
            .Select(_ => Task.Run(
                async () =>
                {
                    await start.WaitAsync(CancellationToken);
                    return await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));
                },
                CancellationToken))
            .ToList();
        start.Release(submitters);
        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Single(results.Select(result => result.Value).Distinct());
        Assert.Equal(1, await CountPostingsAsync());
        Assert.Equal(submitters, (await ReadSubmissionStatsAsync(results[0].Value)).TimesAdded);
    }

    [Fact]
    public async Task FindIdByUrlAsync_KnownLinkCopiedDifferently_ReturnsThePosting()
    {
        var created = await ResolveAsync(new PostingSubmission(Url, Company, Title, Text));

        var found = await FindIdByUrlAsync("https://www.jobs.example.com/4471?utm_campaign=x");

        Assert.Equal(created.Value, found);
    }

    [Theory]
    [InlineData("https://jobs.example.com/9999")]
    [InlineData("not a link")]
    public async Task FindIdByUrlAsync_UnknownOrInvalidLink_ReturnsNullAndCreatesNothing(string url)
    {
        var found = await FindIdByUrlAsync(url);

        Assert.Null(found);
        Assert.Equal(0, await CountPostingsAsync());
    }

    // A scope per call, like one HTTP request each, so concurrent calls never share a DbContext.
    private async Task<Result<Guid>> ResolveAsync(PostingSubmission submission)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IJobPostings>().ResolveAsync(submission, CancellationToken);
    }

    private async Task<Guid?> FindIdByUrlAsync(string url)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IJobPostings>().FindIdByUrlAsync(url, CancellationToken);
    }

    private async Task<long> CountPostingsAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM postings.job_postings", connection);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private async Task<(int TimesAdded, DateTimeOffset LastSeenAt)> ReadSubmissionStatsAsync(Guid postingId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT times_added, last_seen_at FROM postings.job_postings WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", postingId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);
        return (reader.GetInt32(0), reader.GetFieldValue<DateTimeOffset>(1));
    }

    private async Task<(string Company, string Title, string? Text)> ReadContentAsync(Guid postingId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT company, title, text FROM postings.job_postings WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", postingId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);
        return (reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(CancellationToken);
        return connection;
    }
}
