using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;
using Microsoft.AspNetCore.Mvc.Testing;
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
        ConfigureSettings(Factory.Settings);
        Client = Factory.CreateClient();
    }

    /// <summary>Override to change configuration (e.g. lower limits) before the API starts.</summary>
    protected virtual void ConfigureSettings(IDictionary<string, string?> settings)
    {
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>A second browser: same API and database, separate cookie jar.</summary>
    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>A client that never stores cookies, for sending hand-picked refresh tokens.</summary>
    protected HttpClient CreateCookielessClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = Factory.ClientOptions.BaseAddress,
        });
}
