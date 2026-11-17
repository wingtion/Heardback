using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;
using Microsoft.Extensions.Time.Testing;

namespace JobTrail.IntegrationTests.Identity;

/// <summary>
/// Gives every test its own freshly migrated database (xUnit creates a new instance per test),
/// so tests never depend on each other's users or invite codes.
/// </summary>
[Collection(PostgresCollectionDefinition.Name)]
public abstract class IdentityTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    protected FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    protected ApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected string ConnectionString { get; private set; } = string.Empty;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        ConnectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        Factory = new ApiFactory { ConnectionString = ConnectionString, Clock = Clock };
        Client = Factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>A second browser: same API and database, separate cookie jar.</summary>
    protected HttpClient CreateClient() => Factory.CreateClient();
}
