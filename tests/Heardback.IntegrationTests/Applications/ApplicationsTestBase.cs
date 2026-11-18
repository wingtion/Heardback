using Heardback.IntegrationTests.Identity;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Two signed-in users, Alice and Bob, on a fresh database per test.</summary>
public abstract class ApplicationsTestBase(PostgresFixture postgres) : IdentityTestBase(postgres), IAsyncLifetime
{
    protected const string Url = "https://jobs.example.com/4471";

    protected string Alice { get; private set; } = string.Empty;

    protected string Bob { get; private set; } = string.Empty;

    /// <summary>Today's UTC date on the test clock.</summary>
    protected DateOnly Today => DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);

    public new async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Alice = (await Client.RegisterAdminAsync("alice@example.com")).AccessToken;
        Bob = (await CreateClient().RegisterUserAsync(Alice, "bob@example.com")).AccessToken;
    }

    /// <summary>Moves the clock and signs both users in again, since access tokens only last minutes.</summary>
    protected async Task AdvanceDaysAsync(int days)
    {
        Clock.Advance(TimeSpan.FromDays(days));
        Alice = (await CreateClient().SignInAsync("alice@example.com")).AccessToken;
        Bob = (await CreateClient().SignInAsync("bob@example.com")).AccessToken;
    }

    protected object NewApplication(
        string company = "Northwind Logistics",
        string role = "Backend Engineer (.NET)",
        string? url = Url,
        string? postingText = null,
        string channel = "LinkedInEasyApply",
        DateOnly? appliedOn = null) =>
        new { company, role, url, postingText, channel, appliedOn = appliedOn ?? Today };

    protected async Task<long> CountRowsAsync(string table)
    {
        await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new Npgsql.NpgsqlCommand($"SELECT count(*) FROM {table}", connection);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }
}
