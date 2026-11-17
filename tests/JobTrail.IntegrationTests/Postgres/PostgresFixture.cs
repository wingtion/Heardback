using Npgsql;
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

    /// <summary>Creates an empty database so a test class starts from a clean slate without cleanup code.</summary>
    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        var databaseName = $"test_{Guid.NewGuid():N}";

        await using var connection = new NpgsqlConnection(Container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new NpgsqlConnectionStringBuilder(Container.GetConnectionString()) { Database = databaseName }
            .ConnectionString;
    }

    /// <summary>
    /// Closes the pooled connections to a database from <see cref="CreateDatabaseAsync"/> and drops it.
    /// Idle pooled connections otherwise stay open for minutes, and across a full run they approach the
    /// container's connection limit, which fails random tests.
    /// </summary>
    public async Task DropDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using (var pooled = new NpgsqlConnection(connectionString))
        {
            NpgsqlConnection.ClearPool(pooled);
        }

        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await using var connection = new NpgsqlConnection(Container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
