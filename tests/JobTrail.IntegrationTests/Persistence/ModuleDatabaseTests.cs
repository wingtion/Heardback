using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;
using JobTrail.Shared.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace JobTrail.IntegrationTests.Persistence;

[Collection(PostgresCollectionDefinition.Name)]
public sealed class ModuleDatabaseTests(PostgresFixture postgres)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HasPendingModelChanges_EveryModule_IsFalse()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();

        var outOfSync = scope.ServiceProvider.GetServices<IModuleDatabase>()
            .Where(database => database.HasPendingModelChanges())
            .Select(database => database.Name)
            .ToList();

        Assert.Empty(outOfSync);
    }

    [Fact]
    public async Task Startup_WithMigrationsEnabled_CreatesIdentityTables()
    {
        var connectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        await using var factory = new ApiFactory { ConnectionString = connectionString };
        _ = factory.Services;

        var tables = await ListTablesAsync(connectionString, schema: "identity");

        Assert.Equal(["__ef_migrations_history", "invite_codes", "refresh_tokens", "users"], tables);
    }

    private static async Task<List<string>> ListTablesAsync(string connectionString, string schema)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema ORDER BY table_name",
            connection);
        command.Parameters.AddWithValue("schema", schema);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
