using Testcontainers.PostgreSql;

namespace JobTrail.IntegrationTests.Postgres;

/// <summary>One real Postgres container shared by every test in <see cref="PostgresCollectionDefinition"/>.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public string Image { get; } = ComposeFile.PostgresImage();

    public PostgreSqlContainer Container { get; }

    public PostgresFixture()
    {
        Container = new PostgreSqlBuilder(Image).Build();
    }

    public async ValueTask InitializeAsync() =>
        await Container.StartAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
